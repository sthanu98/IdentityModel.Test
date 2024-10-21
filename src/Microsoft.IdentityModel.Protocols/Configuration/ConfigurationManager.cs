// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.IdentityModel.Protocols
{
    /// <summary>
    /// Manages the retrieval of Configuration data.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDocumentRetriever"/>.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class ConfigurationManager<T> : BaseConfigurationManager, IConfigurationManager<T> where T : class
    {
        private bool _isFirstRefreshRequest = true;
        private readonly SemaphoreSlim _configurationNullLock = new SemaphoreSlim(1);

        private readonly IDocumentRetriever _docRetriever;
        private readonly IConfigurationRetriever<T> _configRetriever;
        private readonly IConfigurationValidator<T> _configValidator;
        private T _currentConfiguration;

        // task states are used to ensure the call to 'update config' (UpdateCurrentConfiguration) is a singleton. Uses Interlocked.CompareExchange.
        // metadata is not being obtained
        private const int ConfigurationRetrieverIdle = 0;
        // metadata is being retrieved
        private const int ConfigurationRetrieverRunning = 1;
        private int _configurationRetrieverState = ConfigurationRetrieverIdle;

        /// <summary>
        /// Instantiates a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever)
            : this(metadataAddress, configRetriever, new HttpDocumentRetriever(), new LastKnownGoodConfigurationCacheOptions())
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="httpClient">The client to use when obtaining configuration.</param>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, HttpClient httpClient)
            : this(metadataAddress, configRetriever, new HttpDocumentRetriever(httpClient), new LastKnownGoodConfigurationCacheOptions())
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="docRetriever">The <see cref="IDocumentRetriever"/> that reaches out to obtain the configuration.</param>
        /// <exception cref="ArgumentNullException">If 'metadataAddress' is null or empty.</exception>
        /// <exception cref="ArgumentNullException">If 'configRetriever' is null.</exception>
        /// <exception cref="ArgumentNullException">If 'docRetriever' is null.</exception>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, IDocumentRetriever docRetriever)
            : this(metadataAddress, configRetriever, docRetriever, new LastKnownGoodConfigurationCacheOptions())
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="docRetriever">The <see cref="IDocumentRetriever"/> that reaches out to obtain the configuration.</param>
        /// <param name="lkgCacheOptions">The <see cref="LastKnownGoodConfigurationCacheOptions"/></param>
        /// <exception cref="ArgumentNullException">If 'metadataAddress' is null or empty.</exception>
        /// <exception cref="ArgumentNullException">If 'configRetriever' is null.</exception>
        /// <exception cref="ArgumentNullException">If 'docRetriever' is null.</exception>
        /// <exception cref="ArgumentNullException">If 'lkgCacheOptions' is null.</exception>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, IDocumentRetriever docRetriever, LastKnownGoodConfigurationCacheOptions lkgCacheOptions)
            : base(lkgCacheOptions)
        {
            if (string.IsNullOrWhiteSpace(metadataAddress))
                throw LogHelper.LogArgumentNullException(nameof(metadataAddress));

            if (configRetriever == null)
                throw LogHelper.LogArgumentNullException(nameof(configRetriever));

            if (docRetriever == null)
                throw LogHelper.LogArgumentNullException(nameof(docRetriever));

            MetadataAddress = metadataAddress;
            _docRetriever = docRetriever;
            _configRetriever = configRetriever;
        }

        /// <summary>
        /// Instantiates a new <see cref="ConfigurationManager{T}"/> with configuration validator that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="docRetriever">The <see cref="IDocumentRetriever"/> that reaches out to obtain the configuration.</param>
        /// <param name="configValidator">The <see cref="IConfigurationValidator{T}"/></param>
        /// <exception cref="ArgumentNullException">If 'configValidator' is null.</exception>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, IDocumentRetriever docRetriever, IConfigurationValidator<T> configValidator)
            : this(metadataAddress, configRetriever, docRetriever, configValidator, new LastKnownGoodConfigurationCacheOptions())
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="ConfigurationManager{T}"/> with configuration validator that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="docRetriever">The <see cref="IDocumentRetriever"/> that reaches out to obtain the configuration.</param>
        /// <param name="configValidator">The <see cref="IConfigurationValidator{T}"/></param>
        /// <param name="lkgCacheOptions">The <see cref="LastKnownGoodConfigurationCacheOptions"/></param>
        /// <exception cref="ArgumentNullException">If 'configValidator' is null.</exception>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, IDocumentRetriever docRetriever, IConfigurationValidator<T> configValidator, LastKnownGoodConfigurationCacheOptions lkgCacheOptions)
            : this(metadataAddress, configRetriever, docRetriever, lkgCacheOptions)
        {
            if (configValidator == null)
                throw LogHelper.LogArgumentNullException(nameof(configValidator));

            _configValidator = configValidator;
        }

        /// <summary>
        /// Obtains an updated version of Configuration.
        /// </summary>
        /// <returns>Configuration of type T.</returns>
        /// <remarks>If the time since the last call is less than <see cref="BaseConfigurationManager.AutomaticRefreshInterval"/> then <see cref="IConfigurationRetriever{T}.GetConfigurationAsync"/> is not called and the current Configuration is returned.</remarks>
        public async Task<T> GetConfigurationAsync()
        {
            return await GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Obtains an updated version of Configuration.
        /// </summary>
        /// <param name="cancel">CancellationToken</param>
        /// <returns>Configuration of type T.</returns>
        /// <remarks>If the time since the last call is less than <see cref="BaseConfigurationManager.AutomaticRefreshInterval"/> then <see cref="IConfigurationRetriever{T}.GetConfigurationAsync"/> is not called and the current Configuration is returned.</remarks>
        public virtual async Task<T> GetConfigurationAsync(CancellationToken cancel)
        {
            if (_currentConfiguration != null)
            {
                // StartupTime is the time when ConfigurationManager was instantiated.
                double nextRefresh = _automaticRefreshIntervalInSeconds + _secondsWhenLastAutomaticRefreshOccurred;
                if (nextRefresh > (DateTimeOffset.UtcNow - StartupTime).TotalSeconds)
                    return _currentConfiguration;
            }

            Exception fetchMetadataFailure = null;

            // LOGIC
            // if configuration == null => configuration has never been retrieved.
            //   reach out to the metadata endpoint. Since multiple threads could be calling this method
            //   we need to ensure that only one thread is actually fetching the metadata.
            // else
            //   if update task is running, return the current configuration
            //   else kick off task to update current configuration
            if (_currentConfiguration == null)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    await _configurationNullLock.WaitAsync(cancel).ConfigureAwait(false);
                    if (_currentConfiguration != null)
                        return _currentConfiguration;

                    Interlocked.Exchange(ref _configurationRetrieverState, ConfigurationRetrieverRunning);
                    NumberOfTimesMetadataWasRequested++;

                    // Don't use the individual CT here, this is a shared operation that shouldn't be affected by an individual's cancellation.
                    // The transport should have it's own timeouts, etc.
                    T configuration = await _configRetriever.GetConfigurationAsync(
                        MetadataAddress,
                        _docRetriever,
                        CancellationToken.None).ConfigureAwait(false);

                    if (_configValidator != null)
                    {
                        ConfigurationValidationResult result = _configValidator.Validate(configuration);
                        // TODO - result could be null, if the configurationValidator is not implemented correctly.
                        // need test
                        // in this case we have never had a valid configuration, so we will throw an exception if the validation fails
                        if (!result.Succeeded)
                            throw LogHelper.LogExceptionMessage(
                                new InvalidConfigurationException(
                                    LogHelper.FormatInvariant(
                                        LogMessages.IDX20810,
                                        result.ErrorMessage)));
                    }

                    SetCurrentConfiguration(configuration);
                }
                catch (Exception ex)
                {
                    fetchMetadataFailure = ex;

                    HandleException(_currentConfiguration, ex);
                }
                finally
                {
                    _configurationNullLock.Release();
                    Interlocked.Exchange(ref _configurationRetrieverState, ConfigurationRetrieverIdle);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
            else
            {
                if (Interlocked.CompareExchange(ref _configurationRetrieverState, ConfigurationRetrieverRunning, ConfigurationRetrieverIdle) == ConfigurationRetrieverIdle)
                {
                    _ = Task.Run(UpdateCurrentConfiguration, CancellationToken.None);
                }
            }

            // If metadata exists return it.
            if (_currentConfiguration != null)
                return _currentConfiguration;

            throw LogHelper.LogExceptionMessage(
                new InvalidOperationException(
                    LogHelper.FormatInvariant(
                        LogMessages.IDX20803,
                        LogHelper.MarkAsNonPII(MetadataAddress ?? "null"),
                        LogHelper.MarkAsNonPII(_secondsWhenLastAutomaticRefreshOccurred),
                        LogHelper.MarkAsNonPII(fetchMetadataFailure)),
                    fetchMetadataFailure));
        }

        /// <summary>
        /// This should be called when the configuration needs to be updated either from RequestRefresh or AutomaticRefresh
        /// The Caller should first check the state checking state using:
        ///   if (Interlocked.CompareExchange(ref _configurationRetrieverState, ConfigurationRetrieverRunning, ConfigurationRetrieverIdle) == ConfigurationRetrieverIdle).
        /// </summary>
        private void UpdateCurrentConfiguration()
        {
            NumberOfTimesMetadataWasRequested++;
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                T configuration = _configRetriever.GetConfigurationAsync(
                    MetadataAddress,
                    _docRetriever,
                    CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

                if (_configValidator != null)
                {
                    ConfigurationValidationResult result = _configValidator.Validate(configuration);

                    if (!result.Succeeded)
                        throw LogHelper.LogExceptionMessage(
                            new InvalidConfigurationException(
                                LogHelper.FormatInvariant(
                                    LogMessages.IDX20810,
                                    result.ErrorMessage)));
                }

                SetCurrentConfiguration(configuration);
            }
            catch (Exception ex)
            {
                HandleException(_currentConfiguration, ex);
            }
            finally
            {
                Interlocked.Exchange(ref _configurationRetrieverState, ConfigurationRetrieverIdle);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        // Obtaining configuration resulted in an exception.
        // Set the next automatic refresh time.
        private void HandleException(T configuration, Exception ex)
        {
            double secondsUntilNow = (DateTimeOffset.UtcNow - StartupTime).TotalSeconds;

            // if configuration == null => configuration has never been retrieved.
            if (configuration == null)
            {
                // BootstrapIntervalInSeconds must be less that RequestRefreshInterval.
                if (BootstrapIntervalInSeconds < _requestRefreshIntervalInSeconds)
                {
                    // Adopt exponential backoff for bootstrap refresh interval with a decorrelated jitter if it is not longer than the refresh interval.
                    int bootstrapRefreshIntervalInSecondsWithJitter = new Random().Next(BootstrapIntervalInSeconds);
                    BootstrapIntervalInSeconds += BootstrapIntervalInSeconds;
                    Interlocked.Exchange(ref _secondsWhenLastAutomaticRefreshOccurred, secondsUntilNow + bootstrapRefreshIntervalInSecondsWithJitter);
                }
                else
                {
                    Interlocked.Exchange(ref _secondsWhenLastAutomaticRefreshOccurred, secondsUntilNow + (_automaticRefreshIntervalInSeconds < _requestRefreshIntervalInSeconds ?
                        _automaticRefreshIntervalInSeconds : _requestRefreshIntervalInSeconds));
                }

                throw LogHelper.LogExceptionMessage(
                    new InvalidOperationException(
                        LogHelper.FormatInvariant(
                            LogMessages.IDX20803,
                            LogHelper.MarkAsNonPII(MetadataAddress ?? "null"),
                            LogHelper.MarkAsNonPII(_secondsWhenLastAutomaticRefreshOccurred),
                            LogHelper.MarkAsNonPII(ex)),
                        ex));
            }
            else
            {
                Interlocked.Exchange(ref _secondsWhenLastAutomaticRefreshOccurred, secondsUntilNow + (_automaticRefreshIntervalInSeconds < _requestRefreshIntervalInSeconds ? _automaticRefreshIntervalInSeconds : _requestRefreshIntervalInSeconds));
                LogHelper.LogExceptionMessage(
                    new InvalidOperationException(
                        LogHelper.FormatInvariant(
                            LogMessages.IDX20806,
                            LogHelper.MarkAsNonPII(MetadataAddress ?? "null"),
                            LogHelper.MarkAsNonPII(ex)),
                        ex));
            }
        }

        /// <summary>
        /// Called only when configuration is successfully obtained.
        /// </summary>
        /// <param name="configuration"></param>
        private void SetCurrentConfiguration(T configuration)
        {
            _currentConfiguration = configuration;
            // StartupTime is the time when ConfigurationManager was instantiated.
            // (DateTimeOffset.UtcNow - StartupTime).TotalSeconds is the number of seconds since ConfigurationManager was instantiated.
            // Record in seconds when the last time configuration was obtained.

            double nextRefresh = (DateTimeOffset.UtcNow - StartupTime).TotalSeconds + _automaticRefreshIntervalInSeconds
                + ((_automaticRefreshIntervalInSeconds >= int.MaxValue) ? 0 : (new Random().Next((int)_automaticRefreshIntervalInSeconds / 20)));

            Interlocked.Exchange(ref _secondsWhenLastAutomaticRefreshOccurred, (DateTimeOffset.UtcNow - StartupTime).TotalSeconds);
        }

        /// <summary>
        /// Obtains an updated version of Configuration.
        /// </summary>
        /// <param name="cancel">CancellationToken</param>
        /// <returns>Configuration of type BaseConfiguration    .</returns>
        /// <remarks>If the time since the last call is less than <see cref="BaseConfigurationManager._automaticRefreshIntervalInSeconds"/> then <see cref="IConfigurationRetriever{T}.GetConfigurationAsync"/> is not called and the current Configuration is returned.</remarks>
        public override async Task<BaseConfiguration> GetBaseConfigurationAsync(CancellationToken cancel)
        {
            T obj = await GetConfigurationAsync(cancel).ConfigureAwait(false);
            return obj as BaseConfiguration;
        }

        /// <summary>
        /// Triggers updating metadata when:
        /// <para>1. Called the first time.</para>
        /// <para>2. The time between when this method was called and DateTimeOffset.Now is greater than <see cref="BaseConfigurationManager.RefreshInterval"/>.</para>
        /// <para>If <see cref="BaseConfigurationManager.RefreshInterval"/> == <see cref="TimeSpan.MaxValue"/> then this method does nothing.</para>
        /// </summary>
        public override void RequestRefresh()
        {
            double nextRefresh = _requestRefreshIntervalInSeconds + _secondsWhenLastRequestRefreshIOccurred;
            if (nextRefresh < (DateTimeOffset.UtcNow - StartupTime).TotalSeconds || _isFirstRefreshRequest)
            {
                Interlocked.Exchange(ref _secondsWhenLastRequestRefreshIOccurred, (DateTimeOffset.UtcNow - StartupTime).TotalSeconds);
                _isFirstRefreshRequest = false;
                if (Interlocked.CompareExchange(ref _configurationRetrieverState, ConfigurationRetrieverRunning, ConfigurationRetrieverIdle) == ConfigurationRetrieverIdle)
                {
                    _ = Task.Run(UpdateCurrentConfiguration, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// 12 hours is the default time interval that afterwards, <see cref="GetBaseConfigurationAsync(CancellationToken)"/> will obtain new configuration.
        /// </summary>
        public new static readonly TimeSpan DefaultAutomaticRefreshInterval = BaseConfigurationManager.DefaultAutomaticRefreshInterval;

        /// <summary>
        /// 5 minutes is the default time interval that must pass for <see cref="RequestRefresh"/> to obtain a new configuration.
        /// </summary>
        public new static readonly TimeSpan DefaultRefreshInterval = BaseConfigurationManager.DefaultRefreshInterval;

        /// <summary>
        /// 5 minutes is the minimum value for automatic refresh. <see cref="MinimumAutomaticRefreshInterval"/> can not be set less than this value.
        /// </summary>
        public new static readonly TimeSpan MinimumAutomaticRefreshInterval = BaseConfigurationManager.MinimumAutomaticRefreshInterval;

        /// <summary>
        /// 1 second is the minimum time interval that must pass for <see cref="MinimumRefreshInterval"/> to  obtain new configuration.
        /// </summary>
        public new static readonly TimeSpan MinimumRefreshInterval = BaseConfigurationManager.MinimumRefreshInterval;
    }
}
