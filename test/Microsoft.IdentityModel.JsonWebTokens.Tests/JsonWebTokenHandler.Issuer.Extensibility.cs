// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens.Tests;
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Json.Tests;
using Xunit;

#nullable enable
namespace Microsoft.IdentityModel.JsonWebTokens.Extensibility.Tests
{
    public partial class JsonWebTokenHandlerValidateTokenAsyncTests
    {
        [Theory, MemberData(nameof(Issuer_ExtensibilityTestCases), DisableDiscoveryEnumeration = true)]
        public async Task ValidateTokenAsync_IssuerValidator_Extensibility(IssuerExtensibilityTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.{nameof(ValidateTokenAsync_IssuerValidator_Extensibility)}", theoryData);
            context.IgnoreType = false;

            try
            {
                ValidationResult<ValidatedToken> validationResult = await theoryData.JsonWebTokenHandler.ValidateTokenAsync(
                    theoryData.JsonWebToken!,
                    theoryData.ValidationParameters!,
                    theoryData.CallContext,
                    CancellationToken.None);

                if (validationResult.IsValid)
                {
                    ValidatedToken validatedToken = validationResult.UnwrapResult();
                    if (validatedToken.ValidatedIssuer.HasValue)
                        IdentityComparer.AreValidatedIssuersEqual(validatedToken.ValidatedIssuer.Value, theoryData.ValidatedIssuer, context);
                }
                else
                {
                    ValidationError validationError = validationResult.UnwrapError();
                    IdentityComparer.AreValidationErrorsEqual(validationError, theoryData.IssuerValidationError, context);
                    theoryData.ExpectedException.ProcessException(validationError.GetException(), context);
                }
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<IssuerExtensibilityTheoryData> Issuer_ExtensibilityTestCases
        {
            get
            {
                var theoryData = new TheoryData<IssuerExtensibilityTheoryData>();
                CallContext callContext = new CallContext();
                string issuerGuid = Guid.NewGuid().ToString();

                #region return CustomIssuerValidationError
                // Test cases where delegate is overridden and return an CustomIssuerValidationError
                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: SecurityTokenInvalidIssuerException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "CustomIssuerValidatorDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.CustomIssuerValidatorDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorDelegate)),
                    IssuerValidationError = new CustomIssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorDelegate), null),
                        typeof(SecurityTokenInvalidIssuerException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid)
                });

                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: CustomSecurityTokenInvalidIssuerException : SecurityTokenInvalidIssuerException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "CustomIssuerValidatorCustomExceptionDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionDelegate)),
                    IssuerValidationError = new CustomIssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionDelegate), null),
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid),
                });

                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: NotSupportedException : SystemException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "CustomIssuerValidatorUnknownExceptionDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.CustomIssuerValidatorUnknownExceptionDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorUnknownExceptionDelegate)),
                    IssuerValidationError = new CustomIssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorUnknownExceptionDelegate), null),
                        typeof(NotSupportedException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid),
                });

                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: NotSupportedException : SystemException, ValidationFailureType: CustomIssuerValidationFailureType
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate)),
                    IssuerValidationError = new CustomIssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate), null),
                        CustomIssuerValidationError.CustomIssuerValidationFailureType,
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid,
                        null),
                });
                #endregion

                #region return IssuerValidationError
                // Test cases where delegate is overridden and return an IssuerValidationError
                // IssuerValidationError : ValidationError, ExceptionType:  SecurityTokenInvalidIssuerException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "IssuerValidatorDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.IssuerValidatorDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorDelegate)),
                    IssuerValidationError = new IssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.IssuerValidatorDelegate), null),
                        typeof(SecurityTokenInvalidIssuerException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid)
                });

                // IssuerValidationError : ValidationError, ExceptionType:  CustomSecurityTokenInvalidIssuerException : SecurityTokenInvalidIssuerException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "IssuerValidatorCustomIssuerExceptionTypeDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.IssuerValidatorCustomIssuerExceptionTypeDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenException),
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomIssuerExceptionTypeDelegate)),
                    IssuerValidationError = new IssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomIssuerExceptionTypeDelegate), null),
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid)
                });

                // IssuerValidationError : ValidationError, ExceptionType:  CustomSecurityTokenException : SystemException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "IssuerValidatorCustomExceptionTypeDelegate",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.IssuerValidatorCustomExceptionTypeDelegate)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenException),
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomExceptionTypeDelegate)),
                    IssuerValidationError = new IssuerValidationError(
                        new MessageDetail(
                            nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomExceptionTypeDelegate), null),
                        typeof(CustomSecurityTokenException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid)
                });

                // IssuerValidationError : ValidationError, ExceptionType: SecurityTokenInvalidIssuerException, inner: CustomSecurityTokenInvalidIssuerException
                theoryData.Add(new IssuerExtensibilityTheoryData(
                    "IssuerValidatorThrows",
                    issuerGuid,
                    CustomIssuerValidatorDelegates.IssuerValidatorThrows)
                {
                    ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidIssuerException),
                        string.Format(Tokens.LogMessages.IDX10269),
                        typeof(CustomSecurityTokenInvalidIssuerException)),
                    IssuerValidationError = new IssuerValidationError(
                        new MessageDetail(
                            string.Format(Tokens.LogMessages.IDX10269), null),
                        ValidationFailureType.IssuerValidatorThrew,
                        typeof(SecurityTokenInvalidIssuerException),
                        ValidationError.GetCurrentStackFrame(),
                        issuerGuid,
                        new SecurityTokenInvalidIssuerException(nameof(CustomIssuerValidatorDelegates.IssuerValidatorThrows))
                    )
                });
                #endregion

                return theoryData;
            }
        }

        public class IssuerExtensibilityTheoryData : ValidateTokenAsyncBaseTheoryData
        {
            internal IssuerExtensibilityTheoryData(string testId, string issuer, IssuerValidationDelegateAsync issuerValidator) : base(testId)
            {
                JsonWebToken = JsonUtilities.CreateUnsignedJsonWebToken("iss", issuer);
                ValidationParameters = new ValidationParameters
                {
                    AlgorithmValidator = SkipValidationDelegates.SkipAlgorithmValidation,
                    AudienceValidator = SkipValidationDelegates.SkipAudienceValidation,
                    IssuerValidatorAsync = issuerValidator,
                    IssuerSigningKeyValidator = SkipValidationDelegates.SkipIssuerSigningKeyValidation,
                    LifetimeValidator = SkipValidationDelegates.SkipLifetimeValidation,
                    SignatureValidator = SkipValidationDelegates.SkipSignatureValidation,
                    TokenReplayValidator = SkipValidationDelegates.SkipTokenReplayValidation,
                    TokenTypeValidator = SkipValidationDelegates.SkipTokenTypeValidation
                };
            }

            public JsonWebToken JsonWebToken { get; }

            public JsonWebTokenHandler JsonWebTokenHandler { get; } = new JsonWebTokenHandler();

            public bool IsValid { get; set; }

            internal ValidatedIssuer ValidatedIssuer { get; set; }

            internal IssuerValidationError? IssuerValidationError { get; set; }
        }
    }
}
#nullable restore
