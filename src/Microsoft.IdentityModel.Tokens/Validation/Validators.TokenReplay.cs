// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// Definition for delegate that will validate that a <see cref="SecurityToken"/> has not been replayed.
    /// </summary>
    /// <param name="expirationTime">When does the <see cref="SecurityToken"/> expire..</param>
    /// <param name="securityToken">The security token that is being validated.</param>
    /// <param name="validationParameters">The <see cref="ValidationParameters"/> to be used for validating the token.</param>
    /// <param name="callContext"></param>
    /// <returns>A <see cref="ReplayValidationResult"/>that contains the results of validating the token.</returns>
    /// <remarks>This delegate is not expected to throw.</remarks>
    internal delegate ReplayValidationResult TokenReplayValidatorDelegate(
        DateTime? expirationTime,
        string securityToken,
        ValidationParameters validationParameters,
        CallContext callContext);

    /// <summary>
    /// Partial class for Token Replay validation.
    /// </summary>
    public static partial class Validators
    {
        /// <summary>
        /// Validates if a token has been replayed.
        /// </summary>
        /// <param name="expirationTime">When does the security token expire.</param>
        /// <param name="securityToken">The <see cref="SecurityToken"/> being validated.</param>
        /// <param name="validationParameters">The <see cref="ValidationParameters"/> to be used for validating the token.</param>
        /// <param name="callContext"></param>
        /// <exception cref="ArgumentNullException">If 'securityToken' is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">If 'validationParameters' is null or whitespace.</exception>
        /// <exception cref="SecurityTokenNoExpirationException">If <see cref="ValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
        /// <exception cref="SecurityTokenReplayDetectedException">If the 'securityToken' is found in the cache.</exception>
        /// <exception cref="SecurityTokenReplayAddFailedException">If the 'securityToken' could not be added to the <see cref="ValidationParameters.TokenReplayCache"/>.</exception>
#pragma warning disable CA1801 // Review unused parameters
        internal static ReplayValidationResult ValidateTokenReplay(DateTime? expirationTime, string securityToken, ValidationParameters validationParameters, CallContext callContext)
#pragma warning restore CA1801 // Review unused parameters
        {
            if (string.IsNullOrWhiteSpace(securityToken))
                return new ReplayValidationResult(
                    expirationTime,
                    ValidationFailureType.NullArgument,
                    new ExceptionDetail(
                        new MessageDetail(
                            LogMessages.IDX10000,
                            LogHelper.MarkAsNonPII(nameof(securityToken))),
                        ExceptionDetail.ExceptionType.ArgumentNull,
                        new StackFrame(),
                        null));

            if (validationParameters == null)
                return new ReplayValidationResult(
                    expirationTime,
                    ValidationFailureType.NullArgument,
                    new ExceptionDetail(
                        new MessageDetail(
                            LogMessages.IDX10000,
                            LogHelper.MarkAsNonPII(nameof(validationParameters))),
                        ExceptionDetail.ExceptionType.ArgumentNull,
                        new StackFrame(),
                        null));

            // check if token if replay cache is set, then there must be an expiration time.
            if (validationParameters.TokenReplayCache != null)
            {
                if (expirationTime == null)
                    return new ReplayValidationResult(
                        expirationTime,
                        ValidationFailureType.TokenReplayValidationFailed,
                        new ExceptionDetail(
                            new MessageDetail(
                                LogMessages.IDX10227,
                                LogHelper.MarkAsUnsafeSecurityArtifact(securityToken, t => t.ToString())),
                            ExceptionDetail.ExceptionType.SecurityTokenReplayDetected,
                            new StackFrame(),
                            null));

                if (validationParameters.TokenReplayCache.TryFind(securityToken))
                    return new ReplayValidationResult(
                        expirationTime,
                        ValidationFailureType.TokenReplayValidationFailed,
                        new ExceptionDetail(
                            new MessageDetail(
                                LogMessages.IDX10228,
                                LogHelper.MarkAsUnsafeSecurityArtifact(securityToken, t => t.ToString())),
                            ExceptionDetail.ExceptionType.SecurityTokenReplayDetected,
                            new StackFrame(),
                            null));

                if (!validationParameters.TokenReplayCache.TryAdd(securityToken, expirationTime.Value))
                    return new ReplayValidationResult(
                        expirationTime,
                        ValidationFailureType.TokenReplayValidationFailed,
                        new ExceptionDetail(
                            new MessageDetail(
                                LogMessages.IDX10229,
                                LogHelper.MarkAsUnsafeSecurityArtifact(securityToken, t => t.ToString())),
                            ExceptionDetail.ExceptionType.SecurityTokenReplayAddFailed,
                            new StackFrame(),
                            null));
            }

            // if it reaches here, that means no token replay is detected.
            LogHelper.LogInformation(LogMessages.IDX10240);
            return new ReplayValidationResult(expirationTime);
        }
    }
}
