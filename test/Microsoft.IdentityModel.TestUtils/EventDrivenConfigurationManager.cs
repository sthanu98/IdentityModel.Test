// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.IdentityModel.TestUtils
{
    public class EventDrivenConfigurationRetriever<T> : IConfigurationRetriever<T>
    {
        private ManualResetEvent _signalEvent;
        private ManualResetEvent _waitEvent;
        private T _configuration;

        /// <summary>
        /// Initializes an new instance of <see cref="EventDrivenConfigurationRetriever{T}"/> with a configuration instance.
        /// </summary>
        /// <param name="configuration">The Configuration that will be returned</param>
        /// <param name="signalEvent">A <see cref="ManualResetEvent"/>that is be signaled when inside GetConfigurationAsync.</param>
        /// <param name="waitEvent">A <see cref="ManualResetEvent"/>that waits inside GetConfigurationAsync.</param>
        public EventDrivenConfigurationRetriever(
            T configuration,
            ManualResetEvent signalEvent,
            ManualResetEvent waitEvent)
        {
            _configuration = configuration;
            _signalEvent = signalEvent;
            _waitEvent = waitEvent;
        }

        public Task<T> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            _waitEvent.WaitOne();
            _signalEvent.Set();
            return Task.FromResult(_configuration);
        }
    }

    /// <summary>
    /// This type is used for testing the functionality of using a last known good configuration, as well
    /// as a refreshed configuration.
    /// </summary>
    /// <typeparam name="T">must be a class inherit from <see cref="BaseConfiguration"/>.</typeparam>
    public class EventControlledConfigurationManger<T> : ConfigurationManager<T>, IConfigurationManager<T> where T : class
    {
        private ManualResetEvent _configSignalEvent;
        private ManualResetEvent _configWaitEvent;
        private ManualResetEvent _refreshSignalEvent;
        private ManualResetEvent _refreshWaitEvent;

        /// <summary>
        /// Initializes an new instance of <see cref="EventControlledConfigurationManger{T}"/> with a Configuration instance.
        /// </summary>
        /// <param name="metadataAddress">The metadata address to obtain configuration.</param>
        /// <param name="configurationRetriever">The <see cref="IConfigurationRetriever{T}"/>that reads the metadata.</param>
        /// <param name="documentRetriever">The <see cref="IDocumentRetriever"/>that obtains the metadata.</param>
        /// <param name="configSignalEvent">A <see cref="ManualResetEvent"/>that is signaled when GetConfigurationAsync is exiting.</param>
        /// <param name="configWaitEvent">A <see cref="ManualResetEvent"/>that waits in GetConfigurationAsync after calling base.GetConfigurationAsync. </param>
        /// <param name="refreshSignalEvent">A <see cref="ManualResetEvent"/> that is signaled when RequestRefresh is exiting.</param>
        /// <param name="refreshWaitEvent">A <see cref="ManualResetEvent"/>that waits after base.RequestRefresh is called.</param>
        public EventControlledConfigurationManger(
            string metadataAddress,
            IConfigurationRetriever<T> configurationRetriever,
            IDocumentRetriever documentRetriever,
            ManualResetEvent configSignalEvent,
            ManualResetEvent configWaitEvent,
            ManualResetEvent refreshSignalEvent = null,
            ManualResetEvent refreshWaitEvent = null) : base(metadataAddress, configurationRetriever, documentRetriever)
        {
            _configSignalEvent = configSignalEvent;
            _configWaitEvent = configWaitEvent;
            _refreshWaitEvent = refreshWaitEvent;
            _refreshSignalEvent = refreshSignalEvent;
        }

        /// <summary>
        /// Obtains an updated version of Configuration.
        /// </summary>
        /// <param name="cancel"><see cref="CancellationToken"/>.</param>
        /// <returns>Configuration of type T.</returns>
        public override Task<T> GetConfigurationAsync(CancellationToken cancel)
        {
            try
            {
                Task<T> t = base.GetConfigurationAsync(cancel);
                _configWaitEvent.WaitOne();
                return t;
            }
            finally
            {
                _configSignalEvent.Set();
            }
        }

        /// <summary>
        /// Unless _refreshedConfiguration is set, this is a no-op.
        /// </summary>
        public override void RequestRefresh()
        {
            try
            {
                base.RequestRefresh();
                if (_refreshWaitEvent != null)
                    _refreshWaitEvent.WaitOne();
            }
            finally
            {
                if (_refreshSignalEvent != null)
                    _refreshSignalEvent.Set();
            }
        }
    }
}

