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
                IssuerExtensibilityTheoryData testCase = new IssuerExtensibilityTheoryData("CustomIssuerValidatorDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.CustomIssuerValidatorDelegate;
                testCase.IssuerValidationError = new CustomIssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorDelegate), null),
                    typeof(SecurityTokenInvalidIssuerException),
                    CustomIssuerValidatorDelegates.CustomIssuerValidationStackFrame!,
                    issuerGuid);
                theoryData.Add(testCase);

                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: CustomSecurityTokenInvalidIssuerException : SecurityTokenInvalidIssuerException
                testCase = new IssuerExtensibilityTheoryData("CustomIssuerValidatorCustomExceptionDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionDelegate;
                testCase.IssuerValidationError = new CustomIssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionDelegate), null),
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    CustomIssuerValidatorDelegates.CustomIssuerValidationStackFrame!,
                    issuerGuid);
                theoryData.Add(testCase);

                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: NotSupportedException : SystemException
                testCase = new IssuerExtensibilityTheoryData("CustomIssuerValidatorUnknownExceptionDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(SecurityTokenException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorUnknownExceptionDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.CustomIssuerValidatorUnknownExceptionDelegate;
                testCase.IssuerValidationError = new CustomIssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorUnknownExceptionDelegate), null),
                    typeof(NotSupportedException),
                    CustomIssuerValidatorDelegates.CustomIssuerValidationStackFrame!,
                    issuerGuid);
                theoryData.Add(testCase);

                // CustomIssuerValidationError : IssuerValidationError, ExceptionType: NotSupportedException : SystemException, ValidationFailureType: CustomIssuerValidationFailureType
                testCase = new IssuerExtensibilityTheoryData("CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(CustomSecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate;
                testCase.IssuerValidationError = new CustomIssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.CustomIssuerValidatorCustomExceptionCustomFailureTypeDelegate), null),
                    CustomIssuerValidationError.CustomIssuerValidationFailureType,
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    CustomIssuerValidatorDelegates.CustomIssuerValidationCustomExceptionCustomFailureTypeDelegateStackFrame!,
                    issuerGuid,
                    null);
                theoryData.Add(testCase);
                #endregion

                #region return IssuerValidationError
                // Test cases where delegate is overridden and return an IssuerValidationError
                // IssuerValidationError : ValidationError, ExceptionType:  SecurityTokenInvalidIssuerException
                testCase = new IssuerExtensibilityTheoryData("IssuerValidatorDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidIssuerException),
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.IssuerValidatorDelegate;
                testCase.IssuerValidationError = new IssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorDelegate), null),
                    typeof(SecurityTokenInvalidIssuerException),
                    CustomIssuerValidatorDelegates.IssuerValidationStackFrame!,
                    issuerGuid);
                theoryData.Add(testCase);

                // IssuerValidationError : ValidationError, ExceptionType:  CustomSecurityTokenInvalidIssuerException : SecurityTokenInvalidIssuerException
                testCase = new IssuerExtensibilityTheoryData("IssuerValidatorCustomIssuerExceptionTypeDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(SecurityTokenException),
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomIssuerExceptionTypeDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.IssuerValidatorCustomIssuerExceptionTypeDelegate;
                testCase.IssuerValidationError = new IssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomIssuerExceptionTypeDelegate), null),
                    typeof(CustomSecurityTokenInvalidIssuerException),
                    CustomIssuerValidatorDelegates.IssuerValidationCustomIssuerExceptionTypeStackFrame!,
                    issuerGuid);
                theoryData.Add(testCase);

                // IssuerValidationError : ValidationError, ExceptionType:  CustomSecurityTokenException : SystemException
                testCase = new IssuerExtensibilityTheoryData("IssuerValidatorCustomExceptionTypeDelegate", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(SecurityTokenException),
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomExceptionTypeDelegate));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.IssuerValidatorCustomExceptionTypeDelegate;
                testCase.IssuerValidationError = new IssuerValidationError(
                    new MessageDetail(
                        nameof(CustomIssuerValidatorDelegates.IssuerValidatorCustomExceptionTypeDelegate), null),
                    typeof(CustomSecurityTokenException),
                    CustomIssuerValidatorDelegates.IssuerValidationCustomExceptionTypeStackFrame!,
                    issuerGuid);
                theoryData.Add(testCase);

                // IssuerValidationError : ValidationError, ExceptionType: SecurityTokenInvalidIssuerException, inner: CustomSecurityTokenInvalidIssuerException
                testCase = new IssuerExtensibilityTheoryData("IssuerValidatorThrows", issuerGuid);
                testCase.ExpectedException = new ExpectedException(
                        typeof(SecurityTokenInvalidIssuerException),
                        string.Format(Tokens.LogMessages.IDX10269),
                        typeof(CustomSecurityTokenInvalidIssuerException));
                testCase.ValidationParameters!.IssuerValidatorAsync = CustomIssuerValidatorDelegates.IssuerValidatorThrows;
                testCase.IssuerValidationError = new IssuerValidationError(
                    new MessageDetail(
                        string.Format(Tokens.LogMessages.IDX10269), null),
                    ValidationFailureType.IssuerValidatorThrew,
                    typeof(SecurityTokenInvalidIssuerException),
                    CustomIssuerValidatorDelegates.IssuerValidationStackFrame!,
                    issuerGuid,
                    new SecurityTokenInvalidIssuerException(nameof(CustomIssuerValidatorDelegates.IssuerValidatorThrows)));
                theoryData.Add(testCase);
                #endregion

                return theoryData;
            }
        }

        public class IssuerExtensibilityTheoryData : ValidateTokenAsyncBaseTheoryData
        {
            public IssuerExtensibilityTheoryData(string testId, string issuer) : base(testId)
            {
                JsonWebToken = JsonUtilities.CreateUnsignedJsonWebToken("iss", issuer);
            }

            public JsonWebToken JsonWebToken { get; }

            public JsonWebTokenHandler JsonWebTokenHandler { get; } = new JsonWebTokenHandler();

            public bool IsValid { get; set; }

            internal override ValidationParameters? ValidationParameters { get; set; } = new ValidationParameters
            {
                AlgorithmValidator = SkipValidationDelegates.SkipAlgorithmValidation,
                AudienceValidator = SkipValidationDelegates.SkipAudienceValidation,
                IssuerValidatorAsync = SkipValidationDelegates.SkipIssuerValidation,
                IssuerSigningKeyValidator = SkipValidationDelegates.SkipIssuerSigningKeyValidation,
                LifetimeValidator = SkipValidationDelegates.SkipLifetimeValidation,
                SignatureValidator = SkipValidationDelegates.SkipSignatureValidation,
                TokenReplayValidator = SkipValidationDelegates.SkipTokenReplayValidation,
                TokenTypeValidator = SkipValidationDelegates.SkipTokenTypeValidation
            };

            internal ValidatedIssuer ValidatedIssuer { get; set; }

            internal IssuerValidationError? IssuerValidationError { get; set; }
        }
    }
}
#nullable restore
