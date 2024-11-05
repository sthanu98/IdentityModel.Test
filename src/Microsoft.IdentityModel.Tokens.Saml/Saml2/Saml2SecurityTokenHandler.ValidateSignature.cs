﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Xml;
using TokenLogMessages = Microsoft.IdentityModel.Tokens.LogMessages;

#nullable enable
namespace Microsoft.IdentityModel.Tokens.Saml2
{
    public partial class Saml2SecurityTokenHandler : SecurityTokenHandler
    {
        internal static ValidationResult<SecurityKey> ValidateSignature(
            Saml2SecurityToken samlToken,
            ValidationParameters validationParameters,
#pragma warning disable CA1801 // Review unused parameters
            CallContext callContext)
#pragma warning restore CA1801 // Review unused parameters
        {
            if (samlToken is null)
            {
                return ValidationError.NullParameter(
                    nameof(samlToken),
                    new StackFrame(true));
            }

            if (validationParameters is null)
            {
                return ValidationError.NullParameter(
                    nameof(validationParameters),
                    new StackFrame(true));
            }

            // Delegate is set by the user, we call it and return the result.
            if (validationParameters.SignatureValidator is not null)
                return validationParameters.SignatureValidator(samlToken, validationParameters, null, callContext);

            // If the user wants to accept unsigned tokens, they must set validationParameters.SignatureValidator
            if (samlToken.Assertion.Signature is null)
                return new XmlValidationError(
                    new MessageDetail(
                        TokenLogMessages.IDX10504,
                        samlToken.Assertion.CanonicalString),
                    ValidationFailureType.SignatureValidationFailed,
                    typeof(SecurityTokenValidationException),
                    new StackFrame(true));

            IList<SecurityKey>? keys = null;
            SecurityKey? resolvedKey = null;
            bool keyMatched = false;

            if (validationParameters.IssuerSigningKeyResolver is not null)
            {
                resolvedKey = validationParameters.IssuerSigningKeyResolver(
                    samlToken.Assertion.CanonicalString,
                    samlToken,
                    samlToken.Assertion.Signature.KeyInfo?.Id,
                    validationParameters,
                    null,
                    callContext);
            }
            else
            {
                resolvedKey = SamlTokenUtilities.ResolveTokenSigningKey(samlToken.Assertion.Signature.KeyInfo, validationParameters);
            }

            if (resolvedKey is null)
            {
                if (validationParameters.TryAllIssuerSigningKeys)
                    keys = validationParameters.IssuerSigningKeys;
            }
            else
            {
                keys = [resolvedKey];
                keyMatched = true;
            }

            bool canMatchKey = samlToken.Assertion.Signature.KeyInfo != null;
            List<ValidationError>? errors = null;
            StringBuilder? keysAttempted = null;

            if (keys is not null)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    SecurityKey key = keys[i];
                    ValidationResult<string> algorithmValidationResult = validationParameters.AlgorithmValidator(
                        samlToken.Assertion.Signature.SignedInfo.SignatureMethod,
                        key,
                        samlToken,
                        validationParameters,
                        callContext);

                    if (!algorithmValidationResult.IsValid)
                    {
                        (errors ??= new()).Add(algorithmValidationResult.UnwrapError());
                    }
                    else
                    {
                        var validationError = samlToken.Assertion.Signature.Verify(
                        key,
                        validationParameters.CryptoProviderFactory ?? key.CryptoProviderFactory,
                        callContext);

                        if (validationError is null)
                        {
                            samlToken.SigningKey = key;

                            return key;
                        }
                        else
                        {
                            (errors ??= new()).Add(validationError.AddStackFrame(new StackFrame()));
                        }
                    }

                    (keysAttempted ??= new()).Append(key.ToString());
                    if (canMatchKey && !keyMatched && key.KeyId is not null && samlToken.Assertion.Signature.KeyInfo is not null)
                        keyMatched = samlToken.Assertion.Signature.KeyInfo.MatchesKey(key);
                }
            }

            if (canMatchKey && keyMatched)
                return new XmlValidationError(
                    new MessageDetail(
                        TokenLogMessages.IDX10514,
                        keysAttempted?.ToString(),
                        samlToken.Assertion.Signature.KeyInfo,
                        GetErrorStrings(errors),
                        samlToken),
                    ValidationFailureType.SignatureValidationFailed,
                    typeof(SecurityTokenInvalidSignatureException),
                    new StackFrame(true));

            if ((keysAttempted?.Length ?? 0) > 0)
                return new XmlValidationError(
                    new MessageDetail(
                        TokenLogMessages.IDX10512,
                        keysAttempted!.ToString(),
                        GetErrorStrings(errors),
                        samlToken),
                    ValidationFailureType.SignatureValidationFailed,
                    typeof(SecurityTokenSignatureKeyNotFoundException),
                    new StackFrame(true));

            return new XmlValidationError(
                new MessageDetail(TokenLogMessages.IDX10500),
                ValidationFailureType.SignatureValidationFailed,
                typeof(SecurityTokenSignatureKeyNotFoundException),
                new StackFrame(true));
        }

        private static string GetErrorStrings(List<ValidationError>? errors)
        {
            // This method is called if there are errors in the signature validation process.
            // This check is there to account for the optional parameter.
            if (errors is null)
                return string.Empty;

            if (errors.Count == 1)
                return errors[0].MessageDetail.Message;

            StringBuilder sb = new();
            for (int i = 0; i < errors.Count; i++)
            {
                sb.AppendLine(errors[i].MessageDetail.Message);
            }

            return sb.ToString();
        }
    }
}
#nullable restore
