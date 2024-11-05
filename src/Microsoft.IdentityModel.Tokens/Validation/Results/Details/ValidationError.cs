﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.Tokens
{
    /// <summary>
    /// Contains information so that Exceptions can be logged or thrown written as required.
    /// </summary>
    internal class ValidationError
    {
        private Type _exceptionType;

        /// <summary>
        /// Creates an instance of <see cref="ValidationError"/>
        /// </summary>
        /// <param name="MessageDetail"/> contains information about the exception that is used to generate the exception message.
        /// <param name="exceptionType"/> is the type of exception that occurred.
        /// <param name="failureType"/> is the type of validation failure that occurred.
        /// <param name="stackFrame"/> is the stack frame where the exception occurred.
        internal ValidationError(
            MessageDetail MessageDetail,
            ValidationFailureType failureType,
            Type exceptionType,
            StackFrame stackFrame)
            : this(MessageDetail, failureType, exceptionType, stackFrame, null)
        {
            // TODO: need to include CallContext.
        }

        /// <summary>
        /// Creates an instance of <see cref="ValidationError"/>
        /// </summary>
        /// <param name="messageDetail"/> contains information about the exception that is used to generate the exception message.
        /// <param name="exceptionType"/> is the type of exception that occurred.
        /// <param name="failureType"/> is the type of validation failure that occurred.
        /// <param name="stackFrame"/> is the stack frame where the exception occurred.
        /// <param name="innerException"/> is the inner exception that occurred.
        internal ValidationError(
            MessageDetail messageDetail,
            ValidationFailureType failureType,
            Type exceptionType,
            StackFrame stackFrame,
            Exception innerException)
        {
            InnerException = innerException;
            MessageDetail = messageDetail;
            _exceptionType = exceptionType;
            FailureType = failureType;
            StackFrames = new List<StackFrame>(4)
            {
                stackFrame
            };
        }

        /// <summary>
        /// Creates an instance of an <see cref="Exception"/> using <see cref="ValidationError"/>
        /// </summary>
        /// <returns>An instance of an Exception.</returns>
        internal virtual Exception GetException()
        {
            return GetException(ExceptionType, InnerException);
        }

        internal Exception GetException(Type exceptionType, Exception innerException)
        {
            Exception exception = null;

            if (innerException == null && InnerValidationError == null)
            {
                if (exceptionType == typeof(SecurityTokenArgumentNullException))
                    exception = new SecurityTokenArgumentNullException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidAudienceException))
                    exception = new SecurityTokenInvalidAudienceException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidIssuerException))
                    exception = new SecurityTokenInvalidIssuerException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidLifetimeException))
                    exception = new SecurityTokenInvalidLifetimeException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenReplayDetectedException))
                    exception = new SecurityTokenReplayDetectedException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenReplayAddFailedException))
                    exception = new SecurityTokenReplayAddFailedException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidSigningKeyException))
                    exception = new SecurityTokenInvalidSigningKeyException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidTypeException))
                    exception = new SecurityTokenInvalidTypeException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenReplayDetectedException))
                    exception = new SecurityTokenReplayDetectedException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenExpiredException))
                    exception = new SecurityTokenExpiredException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenNotYetValidException))
                    exception = new SecurityTokenNotYetValidException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidLifetimeException))
                    exception = new SecurityTokenInvalidLifetimeException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenNoExpirationException))
                    exception = new SecurityTokenNoExpirationException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidIssuerException))
                    exception = new SecurityTokenInvalidIssuerException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenSignatureKeyNotFoundException))
                    exception = new SecurityTokenSignatureKeyNotFoundException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenDecryptionFailedException))
                    exception = new SecurityTokenDecryptionFailedException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenMalformedException))
                    exception = new SecurityTokenMalformedException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidSignatureException))
                    exception = new SecurityTokenInvalidSignatureException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenArgumentNullException))
                    exception = new SecurityTokenArgumentNullException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidAlgorithmException))
                    exception = new SecurityTokenInvalidAlgorithmException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenInvalidAlgorithmException))
                    exception = new SecurityTokenInvalidAlgorithmException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenException))
                    exception = new SecurityTokenException(MessageDetail.Message);
                else if (exceptionType == typeof(SecurityTokenKeyWrapException))
                    exception = new SecurityTokenKeyWrapException(MessageDetail.Message);
                else if (ExceptionType == typeof(SecurityTokenValidationException))
                    exception = new SecurityTokenValidationException(MessageDetail.Message);
                else
                {
                    // Exception type is unknown
                    var message = LogHelper.FormatInvariant(LogMessages.IDX10002, exceptionType, MessageDetail.Message);
                    exception = new SecurityTokenException(message);
                }
            }
            else
            {
                Exception actualException = innerException ?? InnerValidationError.GetException();

                if (exceptionType == typeof(SecurityTokenArgumentNullException))
                    return new SecurityTokenArgumentNullException(MessageDetail.Message, innerException);
                else if (exceptionType == typeof(SecurityTokenInvalidAudienceException))
                    exception = new SecurityTokenInvalidAudienceException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidIssuerException))
                    exception = new SecurityTokenInvalidIssuerException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidLifetimeException))
                    exception = new SecurityTokenInvalidLifetimeException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenReplayDetectedException))
                    exception = new SecurityTokenReplayDetectedException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenReplayAddFailedException))
                    exception = new SecurityTokenReplayAddFailedException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidSigningKeyException))
                    exception = new SecurityTokenInvalidSigningKeyException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidTypeException))
                    exception = new SecurityTokenInvalidTypeException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenReplayDetectedException))
                    exception = new SecurityTokenReplayDetectedException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenExpiredException))
                    exception = new SecurityTokenExpiredException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenNotYetValidException))
                    exception = new SecurityTokenNotYetValidException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidLifetimeException))
                    exception = new SecurityTokenInvalidLifetimeException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenNoExpirationException))
                    exception = new SecurityTokenNoExpirationException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidIssuerException))
                    exception = new SecurityTokenInvalidIssuerException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenSignatureKeyNotFoundException))
                    exception = new SecurityTokenSignatureKeyNotFoundException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenDecryptionFailedException))
                    exception = new SecurityTokenDecryptionFailedException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenMalformedException))
                    exception = new SecurityTokenMalformedException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidSignatureException))
                    exception = new SecurityTokenInvalidSignatureException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenArgumentNullException))
                    exception = new SecurityTokenArgumentNullException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidAlgorithmException))
                    exception = new SecurityTokenInvalidAlgorithmException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenInvalidAlgorithmException))
                    exception = new SecurityTokenInvalidAlgorithmException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenException))
                    exception = new SecurityTokenException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenKeyWrapException))
                    exception = new SecurityTokenKeyWrapException(MessageDetail.Message, actualException);
                else if (exceptionType == typeof(SecurityTokenValidationException))
                    exception = new SecurityTokenValidationException(MessageDetail.Message, actualException);
                else
                {
                    // Exception type is unknown
                    var message = LogHelper.FormatInvariant(LogMessages.IDX10002, exceptionType, MessageDetail.Message);
                    exception = new SecurityTokenException(message, actualException);
                }
            }

            if (exception is SecurityTokenException securityTokenException)
                securityTokenException.SetValidationError(this);
            else if (exception is SecurityTokenArgumentNullException securityTokenArgumentNullException)
                securityTokenArgumentNullException.SetValidationError(this);

            return exception;
        }

        internal static ValidationError NullParameter(string parameterName, StackFrame stackFrame) => new(
            MessageDetail.NullParameter(parameterName),
            ValidationFailureType.NullArgument,
            typeof(SecurityTokenArgumentNullException),
            stackFrame);

        /// <summary>
        /// Gets the type of validation failure that occurred.
        /// </summary>
        internal ValidationFailureType FailureType { get; }

        /// <summary>
        /// Gets the type of exception that occurred.
        /// </summary>
        public Type ExceptionType => _exceptionType;

        /// <summary>
        /// Gets the inner exception that occurred.
        /// </summary>
        public Exception InnerException { get; }

        /// <summary>
        /// Gets the details for the inner exception that occurred.
        /// </summary>
        public ValidationError InnerValidationError { get; }

        /// <summary>
        /// Gets the message details that are used to generate the exception message.
        /// </summary>
        internal MessageDetail MessageDetail { get; }

        /// <summary>
        /// Gets the stack frames where the exception occurred.
        /// </summary>
        public IList<StackFrame> StackFrames { get; }

        /// <summary>
        /// Adds a stack frame to the list of stack frames and returns the updated object.
        /// </summary>
        /// <param name="stackFrame">The <see cref="StackFrame"/> to be added.</param>
        /// <returns></returns>
        public ValidationError AddStackFrame(StackFrame stackFrame)
        {
            StackFrames.Add(stackFrame);
            return this;
        }

        /// <summary>
        /// Adds the current stack frame to the list of stack frames and returns the updated object.
        /// If there is no cache entry for the given file path and line number, a new stack frame is created and added to the cache.
        /// </summary>
        /// <param name="filePath">The path to the file from which this method is called. Captured automatically by default.</param>
        /// <param name="lineNumber">The line number from which this method is called. CAptured automatically by default.</param>
        /// <param name="skipFrames">The number of stack frames to skip when capturing. Used to avoid capturing this method and get the caller instead.</param>
        /// <returns>The updated object.</returns>
        internal ValidationError AddCurrentStackFrame([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, int skipFrames = 1)
        {
            // We add 1 to the skipped frames to skip the current method
            StackFrames.Add(GetCurrentStackFrame(filePath, lineNumber, skipFrames + 1));
            return this;
        }

        /// <summary>
        /// Returns the stack frame corresponding to the file path and line number from which this method is called.
        /// If there is no cache entry for the given file path and line number, a new stack frame is created and added to the cache.
        /// </summary>
        /// <param name="filePath">The path to the file from which this method is called. Captured automatically by default.</param>
        /// <param name="lineNumber">The line number from which this method is called. CAptured automatically by default.</param>
        /// <param name="skipFrames">The number of stack frames to skip when capturing. Used to avoid capturing this method and get the caller instead.</param>
        /// <returns>The captured stack frame.</returns>
        /// <remarks>If this is called from a helper method, consider adding an extra skip frame to avoid capturing the helper instead.</remarks>
        internal static StackFrame GetCurrentStackFrame(
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, int skipFrames = 1)
        {
            // String is allocated, but it goes out of scope immediately after the call
            string key = filePath + lineNumber;
            StackFrame frame = CachedStackFrames.GetOrAdd(key, new StackFrame(skipFrames, true));
            return frame;
        }

        // ConcurrentDictionary is thread-safe and only locks when adding a new item.
        private static ConcurrentDictionary<string, StackFrame> CachedStackFrames { get; } = new();
    }
}
