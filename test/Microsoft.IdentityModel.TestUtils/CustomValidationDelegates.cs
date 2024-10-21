// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

#nullable enable
namespace Microsoft.IdentityModel.TestUtils
{
    internal class CustomSecurityTokenInvalidIssuerException : SecurityTokenInvalidIssuerException
    {
        public CustomSecurityTokenInvalidIssuerException(string message)
            : base(message)
        {
        }
    }

    internal class CustomSecurityTokenException : SystemException
    {
        public CustomSecurityTokenException(string message)
            : base(message)
        {
        }
    }


    internal class CustomIssuerValidationError : IssuerValidationError
    {
        /// <summary>
        /// A custom validation failure type.
        /// </summary>
        public static readonly ValidationFailureType CustomIssuerValidationFailureType = new IssuerValidatorFailure("CustomIssuerValidationFailureType");
        private class IssuerValidatorFailure : ValidationFailureType { internal IssuerValidatorFailure(string name) : base(name) { } }

        public CustomIssuerValidationError(
            MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            string? invalidIssuer)
            : base(messageDetail, exceptionType, stackFrame, invalidIssuer)
        {
        }

        public CustomIssuerValidationError(
            MessageDetail messageDetail,
            ValidationFailureType validationFailureType,
            Type exceptionType,
            StackFrame stackFrame,
            string? invalidIssuer,
            Exception? innerException)
            : base(messageDetail, validationFailureType, exceptionType, stackFrame, invalidIssuer, innerException)
        {
        }

        internal override Exception GetException()
        {
            if (ExceptionType == typeof(CustomSecurityTokenInvalidIssuerException))
                return new CustomSecurityTokenInvalidIssuerException(MessageDetail.Message) { InvalidIssuer = InvalidIssuer };

            return base.GetException();
        }
    }

    internal class CustomIssuerWithoutGetExceptionValidationOverrideError : IssuerValidationError
    {
        public CustomIssuerWithoutGetExceptionValidationOverrideError(MessageDetail messageDetail,
            Type exceptionType,
            StackFrame stackFrame,
            string? invalidIssuer) :
            base(messageDetail, exceptionType, stackFrame, invalidIssuer)
        {
        }
    }

    internal class CustomIssuerValidatorDelegates
    {
        internal async static Task<ValidationResult<ValidatedIssuer>> CustomIssuerValidatorDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            // Returns a CustomIssuerValidationError : IssuerValidationError
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new CustomIssuerValidationError(
                    new MessageDetail(nameof(CustomIssuerValidatorDelegate), null),
                    typeof(SecurityTokenInvalidIssuerException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }

        internal async static Task<ValidationResult<ValidatedIssuer>> CustomIssuerValidatorCustomExceptionDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new CustomIssuerValidationError(
                    new MessageDetail(nameof(CustomIssuerValidatorCustomExceptionDelegate), null),
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }

        internal async static Task<ValidationResult<ValidatedIssuer>> CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new CustomIssuerValidationError(
                    new MessageDetail(nameof(CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate), null),
                    CustomIssuerValidationError.CustomIssuerValidationFailureType,
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer,
                    null)));
        }

        internal async static Task<ValidationResult<ValidatedIssuer>> CustomIssuerValidatorUnknownExceptionDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new CustomIssuerValidationError(
                    new MessageDetail(nameof(CustomIssuerValidatorUnknownExceptionDelegate), null),
                    typeof(NotSupportedException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }

        internal async static Task<ValidationResult<ValidatedIssuer>> CustomIssuerValidatorWithoutGetExceptionOverrideDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new CustomIssuerWithoutGetExceptionValidationOverrideError(
                    new MessageDetail(nameof(CustomIssuerValidatorWithoutGetExceptionOverrideDelegate), null),
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }

        internal async static Task<ValidationResult<ValidatedIssuer>> IssuerValidatorDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new IssuerValidationError(
                    new MessageDetail(nameof(IssuerValidatorDelegate), null),
                    typeof(SecurityTokenInvalidIssuerException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async static Task<ValidationResult<ValidatedIssuer>> IssuerValidatorThrows(
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            throw new CustomSecurityTokenInvalidIssuerException(nameof(IssuerValidatorThrows));
        }

        internal async static Task<ValidationResult<ValidatedIssuer>> IssuerValidatorCustomIssuerExceptionTypeDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new IssuerValidationError(
                    new MessageDetail(nameof(IssuerValidatorCustomIssuerExceptionTypeDelegate), null),
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }
        internal async static Task<ValidationResult<ValidatedIssuer>> IssuerValidatorCustomExceptionTypeDelegate(
            string issuer,
            SecurityToken securityToken,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(new ValidationResult<ValidatedIssuer>(
                new IssuerValidationError(
                    new MessageDetail(nameof(IssuerValidatorCustomExceptionTypeDelegate), null),
                    typeof(CustomSecurityTokenException),
                    ValidationError.GetCurrentStackFrame(),
                    issuer)));
        }
    }
}
#nullable restore
