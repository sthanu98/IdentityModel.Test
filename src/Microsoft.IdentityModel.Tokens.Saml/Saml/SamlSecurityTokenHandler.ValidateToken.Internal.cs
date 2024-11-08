// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Microsoft.IdentityModel.Tokens.Saml
{
    /// <summary>
    /// A <see cref="SecurityTokenHandler"/> designed for creating and validating Saml Tokens. See: http://docs.oasis-open.org/security/saml/v2.0/saml-core-2.0-os.pdf
    /// </summary>
    public partial class SamlSecurityTokenHandler : SecurityTokenHandler
    {
        internal async Task<ValidationResult<ValidatedToken>> ValidateTokenAsync(
            string token,
            ValidationParameters validationParameters,
            CallContext callContext,
            CancellationToken cancellationToken)
        {
            if (token is null)
                return ValidationError.NullParameter(nameof(token), ValidationError.GetCurrentStackFrame());

            if (validationParameters is null)
                return ValidationError.NullParameter(nameof(validationParameters), ValidationError.GetCurrentStackFrame());

            var tokenReadingResult = ReadSamlToken(token, callContext);
            if (!tokenReadingResult.IsValid)
                return tokenReadingResult.UnwrapError().AddCurrentStackFrame();

            return await ValidateTokenAsync(tokenReadingResult.UnwrapResult(), validationParameters, callContext, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<ValidationResult<ValidatedToken>> ValidateTokenAsync(
            SamlSecurityToken samlToken,
            ValidationParameters validationParameters,
            CallContext callContext,
#pragma warning disable CA1801 // Review unused parameters
            CancellationToken cancellationToken)
#pragma warning restore CA1801 // Review unused parameters
        {
            if (samlToken is null)
            {
                StackFrames.TokenNull ??= new StackFrame(true);
                return ValidationError.NullParameter(
                    nameof(samlToken),
                    StackFrames.TokenNull);
            }

            if (validationParameters is null)
            {
                StackFrames.TokenValidationParametersNull ??= new StackFrame(true);
                return ValidationError.NullParameter(
                    nameof(validationParameters),
                    StackFrames.TokenValidationParametersNull);
            }

            ValidationResult<ValidatedConditions> conditionsResult = ValidateConditions(samlToken, validationParameters, callContext);

            if (!conditionsResult.IsValid)
            {
                StackFrames.AssertionConditionsValidationFailed ??= new StackFrame(true);
                return conditionsResult.UnwrapError().AddStackFrame(StackFrames.AssertionConditionsValidationFailed);
            }

            ValidationResult<ValidatedIssuer> issuerValidationResult = await validationParameters.IssuerValidatorAsync(
                samlToken.Issuer,
                samlToken,
                validationParameters,
                callContext,
                cancellationToken).ConfigureAwait(false);

            if (!issuerValidationResult.IsValid)
            {
                StackFrames.IssuerValidationFailed ??= new StackFrame(true);
                return issuerValidationResult.UnwrapError().AddStackFrame(StackFrames.IssuerValidationFailed);
            }

            ValidationResult<SecurityKey> signatureValidationResult = ValidateSignature(samlToken, validationParameters, callContext);

            if (!signatureValidationResult.IsValid)
            {
                StackFrames.SignatureValidationFailed ??= new StackFrame(true);
                return signatureValidationResult.UnwrapError().AddStackFrame(StackFrames.SignatureValidationFailed);
            }

            ValidationResult<ValidatedSigningKeyLifetime> issuerSigningKeyValidationResult = validationParameters.IssuerSigningKeyValidator(
                samlToken.SigningKey,
                samlToken,
                validationParameters,
                null,
                callContext);

            if (!issuerSigningKeyValidationResult.IsValid)
            {
                StackFrames.IssuerSigningKeyValidationFailed ??= new StackFrame(true);
                return issuerSigningKeyValidationResult.UnwrapError().AddStackFrame(StackFrames.IssuerSigningKeyValidationFailed);
            }

            return new ValidatedToken(samlToken, this, validationParameters);
        }

        // ValidatedConditions is basically a named tuple but using a record struct better expresses the intent.
        internal record struct ValidatedConditions(string? ValidatedAudience, ValidatedLifetime? ValidatedLifetime);

        internal virtual ValidationResult<ValidatedConditions> ValidateConditions(
            SamlSecurityToken samlToken,
            ValidationParameters validationParameters,
            CallContext callContext)
        {
            if (samlToken.Assertion is null)
            {
                StackFrames.AssertionNull ??= new StackFrame(true);
                return ValidationError.NullParameter(
                    nameof(samlToken.Assertion),
                    StackFrames.AssertionNull);
            }

            if (samlToken.Assertion.Conditions is null)
            {
                StackFrames.AssertionConditionsNull ??= new StackFrame(true);
                return ValidationError.NullParameter(
                    nameof(samlToken.Assertion.Conditions),
                    StackFrames.AssertionConditionsNull);
            }

            var lifetimeValidationResult = validationParameters.LifetimeValidator(
                samlToken.Assertion.Conditions.NotBefore,
                samlToken.Assertion.Conditions.NotOnOrAfter,
                samlToken,
                validationParameters,
                callContext);

            if (!lifetimeValidationResult.IsValid)
            {
                StackFrames.LifetimeValidationFailed ??= new StackFrame(true);
                return lifetimeValidationResult.UnwrapError().AddStackFrame(StackFrames.LifetimeValidationFailed);
            }

            string? validatedAudience = null;
            foreach (var condition in samlToken.Assertion.Conditions.Conditions)
            {

                if (condition is SamlAudienceRestrictionCondition audienceRestriction)
                {

                    // AudienceRestriction.Audiences is an ICollection<Uri> so we need make a conversion to List<string> before calling our audience validator 
                    var audiencesAsList = audienceRestriction.Audiences.Select(static x => x.OriginalString).ToList();

                    var audienceValidationResult = validationParameters.AudienceValidator(
                        audiencesAsList,
                        samlToken,
                        validationParameters,
                        callContext);

                    if (!audienceValidationResult.IsValid)
                        return audienceValidationResult.UnwrapError();

                    validatedAudience = audienceValidationResult.UnwrapResult();
                }

                if (validatedAudience != null)
                    break;
            }

            return new ValidatedConditions(validatedAudience, lifetimeValidationResult.UnwrapResult());
        }
    }
}
#nullable restore
