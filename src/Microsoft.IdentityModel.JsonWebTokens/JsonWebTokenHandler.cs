﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TokenLogMessages = Microsoft.IdentityModel.Tokens.LogMessages;

namespace Microsoft.IdentityModel.JsonWebTokens
{
    /// <summary>
    /// A <see cref="SecurityTokenHandler"/> designed for creating and validating Json Web Tokens. 
    /// See: http://tools.ietf.org/html/rfc7519 and http://www.rfc-editor.org/info/rfc7515.
    /// </summary>
    public class JsonWebTokenHandler : TokenHandler
    {
        /// <summary>
        /// Gets the type of the <see cref="JsonWebToken"/>.
        /// </summary>
        /// <return>The type of <see cref="JsonWebToken"/></return>
        public Type TokenType
        {
            get { return typeof(JsonWebToken); }
        }

        /// <summary>
        /// Determines if the string is a well formed Json Web Token (JWT).
        /// <para>see: http://tools.ietf.org/html/rfc7519 </para>
        /// </summary>
        /// <param name="token">String that should represent a valid JWT.</param>
        /// <remarks>Uses <see cref="Regex.IsMatch(string, string)"/> matching:
        /// <para>JWS: @"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]*$"</para>
        /// <para>JWE: (dir): @"^[A-Za-z0-9-_]+\.\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]*$"</para>
        /// <para>JWE: (wrappedkey): @"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]$"</para>
        /// </remarks>
        /// <returns>
        /// <para>'false' if the token is null or whitespace.</para>
        /// <para>'false' if token.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</para>
        /// <para>'true' if the token is in JSON compact serialization format.</para>
        /// </returns>
        public virtual bool CanReadToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (token.Length> MaximumTokenSizeInBytes)
            {
                LogHelper.LogInformation(TokenLogMessages.IDX10209, token.Length, MaximumTokenSizeInBytes);
                return false;
            }

            // Set the maximum number of segments to MaxJwtSegmentCount + 1. This controls the number of splits and allows detecting the number of segments is too large.
            // For example: "a.b.c.d.e.f.g.h" => [a], [b], [c], [d], [e], [f.g.h]. 6 segments.
            // If just MaxJwtSegmentCount was used, then [a], [b], [c], [d], [e.f.g.h] would be returned. 5 segments.
            string[] tokenParts = token.Split(new char[] { '.' }, JwtConstants.MaxJwtSegmentCount + 1);
            if (tokenParts.Length == JwtConstants.JwsSegmentCount)
                return JwtTokenUtilities.RegexJws.IsMatch(token);
            else if (tokenParts.Length == JwtConstants.JweSegmentCount)
                return JwtTokenUtilities.RegexJwe.IsMatch(token);

            LogHelper.LogInformation(LogMessages.IDX14107);
            return false;
        }

        /// <summary>
        /// Returns a value that indicates if this handler can validate a <see cref="SecurityToken"/>.
        /// </summary>
        /// <returns>'true', indicating this instance can validate a <see cref="JsonWebToken"/>.</returns>
        public virtual bool CanValidateToken
        {
            get { return true; }
        }

        /// <summary>
        /// Creates a JWS (Json Web Signature).
        /// </summary>
        /// <param name="payload">A string containing JSON which represents the JWT token payload.</param>
        /// <param name="signingCredentials">Defines the security key and algorithm that will be used to sign the JWS.</param>
        /// <returns>A JWS in Compact Serialization Format.</returns>
        public virtual string CreateToken(string payload, SigningCredentials signingCredentials)
        {
            if (string.IsNullOrEmpty(payload))
                throw LogHelper.LogArgumentNullException(nameof(payload));

            if (signingCredentials == null)
                throw LogHelper.LogArgumentNullException(nameof(signingCredentials));

            return CreateTokenPrivate(JObject.Parse(payload), signingCredentials, null, null);
        }

        /// <summary>
        /// Creates a JWS(Json Web Signature).
        /// </summary>
        /// <param name="tokenDescriptor">A <see cref="SecurityTokenDescriptor"/> that contains details of contents of the token.</param>
        /// <returns>A JWS in Compact Serialization Format.</returns>
        public virtual string CreateToken(SecurityTokenDescriptor tokenDescriptor)
        {
            if (tokenDescriptor == null)
                throw LogHelper.LogArgumentNullException(nameof(tokenDescriptor));

            if (tokenDescriptor.Claims == null || !tokenDescriptor.Claims.Any())
                LogHelper.LogWarning(LogMessages.IDX14114);

            if (tokenDescriptor.SigningCredentials == null)
                throw LogHelper.LogArgumentNullException(nameof(tokenDescriptor.SigningCredentials));

            var payload = tokenDescriptor.Claims == null ? new Dictionary<string, object>() : new Dictionary<string, object>(tokenDescriptor.Claims);

            if (tokenDescriptor.Audience != null)
            {
                if (payload.ContainsKey(JwtRegisteredClaimNames.Aud))
                    LogHelper.LogInformation(LogHelper.FormatInvariant(LogMessages.IDX14113, nameof(tokenDescriptor.Audience)));

                payload[JwtRegisteredClaimNames.Aud] = tokenDescriptor.Audience;
            }

            if (tokenDescriptor.Expires.HasValue)
            {
                if (payload.ContainsKey(JwtRegisteredClaimNames.Exp))
                    LogHelper.LogInformation(LogHelper.FormatInvariant(LogMessages.IDX14113, nameof(tokenDescriptor.Expires)));

                payload[JwtRegisteredClaimNames.Exp] = EpochTime.GetIntDate(tokenDescriptor.Expires.Value).ToString();
            }

            if (tokenDescriptor.Issuer != null)
            {
                if (payload.ContainsKey(JwtRegisteredClaimNames.Iss))
                    LogHelper.LogInformation(LogHelper.FormatInvariant(LogMessages.IDX14113, nameof(tokenDescriptor.Issuer)));

                payload[JwtRegisteredClaimNames.Iss] = tokenDescriptor.Issuer;
            }

            if (tokenDescriptor.IssuedAt.HasValue)
            {
                if (payload.ContainsKey(JwtRegisteredClaimNames.Iat))
                    LogHelper.LogInformation(LogHelper.FormatInvariant(LogMessages.IDX14113, nameof(tokenDescriptor.IssuedAt)));

                payload[JwtRegisteredClaimNames.Iat] = EpochTime.GetIntDate(tokenDescriptor.IssuedAt.Value).ToString();
            }

            if (tokenDescriptor.NotBefore.HasValue)
            {
                if (payload.ContainsKey(JwtRegisteredClaimNames.Nbf))
                    LogHelper.LogInformation(LogHelper.FormatInvariant(LogMessages.IDX14113, nameof(tokenDescriptor.NotBefore)));

                payload[JwtRegisteredClaimNames.Nbf] = EpochTime.GetIntDate(tokenDescriptor.NotBefore.Value).ToString();
            }

            if (!payload.Any())
                throw LogHelper.LogExceptionMessage(new SecurityTokenException(LogMessages.IDX14115));

            return CreateTokenPrivate(JObject.FromObject(payload), tokenDescriptor.SigningCredentials, tokenDescriptor.EncryptingCredentials, tokenDescriptor.CompressionAlgorithm);
        }

        /// <summary>
        /// Creates a JWE (Json Web Encryption).
        /// </summary>
        /// <param name="payload">A string containing JSON which represents the JWT token payload.</param>
        /// <param name="signingCredentials">Defines the security key and algorithm that will be used to sign the JWT.</param>
        /// <param name="encryptingCredentials">Defines the security key and algorithm that will be used to encrypt the JWT.</param>
        /// <returns>A JWE in compact serialization format.</returns>
        public virtual string CreateToken(string payload, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials)
        {
            if (string.IsNullOrEmpty(payload))
                throw LogHelper.LogArgumentNullException(nameof(payload));

            if (signingCredentials == null)
                throw LogHelper.LogArgumentNullException(nameof(signingCredentials));

            if (encryptingCredentials == null)
                throw LogHelper.LogArgumentNullException(nameof(encryptingCredentials));

            return CreateTokenPrivate(JObject.Parse(payload), signingCredentials, encryptingCredentials, null);
        }

        /// <summary>
        /// Creates a JWE (Json Web Encryption).
        /// </summary>
        /// <param name="payload">A string containing JSON which represents the JWT token payload.</param>
        /// <param name="signingCredentials">Defines the security key and algorithm that will be used to sign the JWT.</param>
        /// <param name="encryptingCredentials">Defines the security key and algorithm that will be used to encrypt the JWT.</param>
        /// <param name="compressionAlgorithm">Defines the compression algorithm that will be used to compress the JWT token payload.</param>
        /// <returns>A JWE in compact serialization format.</returns>
        public virtual string CreateToken(string payload, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials, string compressionAlgorithm)
        {
            if (string.IsNullOrEmpty(payload))
                throw LogHelper.LogArgumentNullException(nameof(payload));

            if (signingCredentials == null)
                throw LogHelper.LogArgumentNullException(nameof(signingCredentials));

            if (encryptingCredentials == null)
                throw LogHelper.LogArgumentNullException(nameof(encryptingCredentials));

            if (string.IsNullOrEmpty(compressionAlgorithm))
                throw LogHelper.LogArgumentNullException(nameof(compressionAlgorithm));

            return CreateTokenPrivate(JObject.Parse(payload), signingCredentials, encryptingCredentials, compressionAlgorithm);
        }

        private string CreateTokenPrivate(JObject payload, SigningCredentials signingCredentials, EncryptingCredentials encryptingCredentials, string algorithm)
        {
            if (!JsonWebTokenManager.KeyToHeaderCache.TryGetValue(JsonWebTokenManager.GetHeaderCacheKey(signingCredentials), out string rawHeader))
            {
                var header = new JObject
                {
                    { JwtHeaderParameterNames.Alg, signingCredentials.Algorithm },
                    { JwtHeaderParameterNames.Kid, signingCredentials.Key.KeyId },
                    { JwtHeaderParameterNames.Typ, JwtConstants.HeaderType }
                };

                rawHeader = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header.ToString(Formatting.None)));
                JsonWebTokenManager.KeyToHeaderCache.TryAdd(JsonWebTokenManager.GetHeaderCacheKey(signingCredentials), rawHeader);
            }

            if (SetDefaultTimesOnTokenCreation)
            {
                var now = DateTime.UtcNow;
                if (!payload.TryGetValue(JwtRegisteredClaimNames.Exp, out _))
                    payload.Add(JwtRegisteredClaimNames.Exp, EpochTime.GetIntDate(now + TimeSpan.FromMinutes(TokenLifetimeInMinutes)).ToString());

                if (!payload.TryGetValue(JwtRegisteredClaimNames.Iat, out _))
                    payload.Add(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString());

                if (!payload.TryGetValue(JwtRegisteredClaimNames.Nbf, out _))
                    payload.Add(JwtRegisteredClaimNames.Nbf, EpochTime.GetIntDate(now).ToString());
            }
       
            var rawPayload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payload.ToString(Formatting.None)));
            var message = rawHeader + "." + rawPayload;
            var rawSignature = JwtTokenUtilities.CreateEncodedSignature(message, signingCredentials);

            if (encryptingCredentials != null)
                return EncryptToken(message + "." + rawSignature, encryptingCredentials, algorithm);
            else
                return message + "." + rawSignature;
        }

        /// <summary>
        /// Compress a JWT token string.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="algorithm"></param>
        /// <exception cref="ArgumentNullException">if 'token' is null.</exception>
        /// <exception cref="ArgumentNullException">if 'algorithm' is null.</exception>
        /// <exception cref="NotSupportedException">if the compression algorithm is not supported.</exception>
        /// <returns>Compressed JWT token bytes.</returns>
        private byte[] CompressToken(string token, string algorithm)
        {
            if (token == null)
                throw LogHelper.LogArgumentNullException(nameof(token));

            if (string.IsNullOrEmpty(algorithm))
                throw LogHelper.LogArgumentNullException(nameof(algorithm));

            if (!CompressionProviderFactory.Default.IsSupportedAlgorithm(algorithm))
                throw LogHelper.LogExceptionMessage(new NotSupportedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10682, algorithm)));

            var compressionProvider = CompressionProviderFactory.Default.CreateCompressionProvider(algorithm);
        
            return compressionProvider.Compress(Encoding.UTF8.GetBytes(token)) ?? throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(TokenLogMessages.IDX10680, algorithm)));
        }

        /// <summary>
        /// Creates a <see cref="ClaimsIdentity"/> from a <see cref="JsonWebToken"/>.
        /// </summary>
        /// <param name="jwtToken">The <see cref="JsonWebToken"/> to use as a <see cref="Claim"/> source.</param>
        /// <param name="validationParameters"> Contains parameters for validating the token.</param>
        /// <returns>A <see cref="ClaimsIdentity"/> containing the <see cref="JsonWebToken.Claims"/>.</returns>
        protected virtual ClaimsIdentity CreateClaimsIdentity(JsonWebToken jwtToken, TokenValidationParameters validationParameters)
        {
            if (jwtToken == null)
                throw LogHelper.LogArgumentNullException(nameof(jwtToken));

            if (validationParameters == null)
                throw LogHelper.LogArgumentNullException(nameof(validationParameters));

            var actualIssuer = jwtToken.Issuer;
            if (string.IsNullOrWhiteSpace(actualIssuer))
            {
                LogHelper.LogVerbose(TokenLogMessages.IDX10244, ClaimsIdentity.DefaultIssuer);
                actualIssuer = ClaimsIdentity.DefaultIssuer;
            }

            return CreateClaimsIdentity(jwtToken, validationParameters, actualIssuer);
        }

        private ClaimsIdentity CreateClaimsIdentity(JsonWebToken jwtToken, TokenValidationParameters validationParameters, string actualIssuer)
        {
            ClaimsIdentity identity = validationParameters.CreateClaimsIdentity(jwtToken, actualIssuer);
            foreach (Claim jwtClaim in jwtToken.Claims)
            {
                string claimType = jwtClaim.Type;
                if (claimType == ClaimTypes.Actor)
                {
                    if (identity.Actor != null)
                        throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(LogMessages.IDX14112, JwtRegisteredClaimNames.Actort, jwtClaim.Value)));

                    if (CanReadToken(jwtClaim.Value))
                    {
                        JsonWebToken actor = ReadToken(jwtClaim.Value) as JsonWebToken;
                        identity.Actor = CreateClaimsIdentity(actor, validationParameters, actualIssuer);
                    }
                }

                if (jwtClaim.Properties.Count == 0)
                {
                    identity.AddClaim(new Claim(claimType, jwtClaim.Value, jwtClaim.ValueType, actualIssuer, actualIssuer, identity));
                }
                else
                {
                    Claim claim = new Claim(claimType, jwtClaim.Value, jwtClaim.ValueType, actualIssuer, actualIssuer, identity);

                    foreach (var kv in jwtClaim.Properties)
                        claim.Properties[kv.Key] = kv.Value;

                    identity.AddClaim(claim);
                }
                
            }

            return identity;
        }

        /// <summary>
        /// Decompress JWT token bytes.
        /// </summary>
        /// <param name="tokenBytes"></param>
        /// <param name="algorithm"></param>
        /// <exception cref="ArgumentNullException">if 'tokenBytes' is null.</exception>
        /// <exception cref="ArgumentNullException">if 'algorithm' is null.</exception>
        /// <exception cref="NotSupportedException">if the decompression algorithm is not supported.</exception>
        /// <returns>Decompressed JWT token</returns>
        private string DecompressToken(byte[] tokenBytes, string algorithm)
        {
            if (tokenBytes == null)
                throw LogHelper.LogArgumentNullException(nameof(tokenBytes));

            if (string.IsNullOrEmpty(algorithm))
                throw LogHelper.LogArgumentNullException(nameof(algorithm));

            if (!CompressionProviderFactory.Default.IsSupportedAlgorithm(algorithm))
                throw LogHelper.LogExceptionMessage(new NotSupportedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10682, algorithm)));

            var compressionProvider = CompressionProviderFactory.Default.CreateCompressionProvider(algorithm);

            var decompressedBytes = compressionProvider.Decompress(tokenBytes);

            return decompressedBytes != null ? Encoding.UTF8.GetString(decompressedBytes) : throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(TokenLogMessages.IDX10679, algorithm)));
        }

        /// <summary>
        /// Decrypts a JWE and returns the clear text 
        /// </summary>
        /// <param name="jwtToken">the JWE that contains the cypher text.</param>
        /// <param name="validationParameters">contains crypto material.</param>
        /// <returns>the decoded / cleartext contents of the JWE.</returns>
        /// <exception cref="ArgumentNullException">if 'jwtToken' is null.</exception>
        /// <exception cref="ArgumentNullException">if 'validationParameters' is null.</exception>
        /// <exception cref="SecurityTokenException">if 'jwtToken.Enc' is null or empty.</exception>
        /// <exception cref="SecurityTokenDecompressionFailedException">if decompression failed.</exception>
        /// <exception cref="SecurityTokenEncryptionKeyNotFoundException">if 'jwtToken.Kid' is not null AND decryption fails.</exception>
        /// <exception cref="SecurityTokenDecryptionFailedException">if the JWE was not able to be decrypted.</exception>
        protected string DecryptToken(JsonWebToken jwtToken, TokenValidationParameters validationParameters)
        {
            if (jwtToken == null)
                throw LogHelper.LogArgumentNullException(nameof(jwtToken));

            if (validationParameters == null)
                throw LogHelper.LogArgumentNullException(nameof(validationParameters));

            if (string.IsNullOrEmpty(jwtToken.Enc))
                throw LogHelper.LogExceptionMessage(new SecurityTokenException(LogHelper.FormatInvariant(TokenLogMessages.IDX10612)));

            var keys = GetContentEncryptionKeys(jwtToken, validationParameters);
            var decryptionSucceeded = false;
            byte[] decryptedTokenBytes = null;

            // keep track of exceptions thrown, keys that were tried
            var exceptionStrings = new StringBuilder();
            var keysAttempted = new StringBuilder();
            foreach (SecurityKey key in keys)
            {
                var cryptoProviderFactory = validationParameters.CryptoProviderFactory ?? key.CryptoProviderFactory;
                if (cryptoProviderFactory == null)
                {
                    LogHelper.LogWarning(TokenLogMessages.IDX10607, key);
                    continue;
                }

                if (!cryptoProviderFactory.IsSupportedAlgorithm(jwtToken.Enc, key))
                {
                    LogHelper.LogWarning(TokenLogMessages.IDX10611, jwtToken.Enc, key);
                    continue;
                }

                try
                {
                    decryptedTokenBytes = DecryptToken(jwtToken, cryptoProviderFactory, key);
                    decryptionSucceeded = true;
                    break;
                }
                catch (Exception ex)
                {
                    exceptionStrings.AppendLine(ex.ToString());
                }

                if (key != null)
                    keysAttempted.AppendLine(key.ToString());
            }

            if (!decryptionSucceeded && keysAttempted.Length > 0)
                throw LogHelper.LogExceptionMessage(new SecurityTokenDecryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10603, keysAttempted, exceptionStrings, jwtToken.EncodedToken)));

            if (!decryptionSucceeded)
                throw LogHelper.LogExceptionMessage(new SecurityTokenDecryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10609, jwtToken.EncodedToken)));

            if (string.IsNullOrEmpty(jwtToken.Zip))
                return Encoding.UTF8.GetString(decryptedTokenBytes);
           
            try
            {
                return DecompressToken(decryptedTokenBytes, jwtToken.Zip);
            }
            catch (Exception ex)
            {
                throw LogHelper.LogExceptionMessage(new SecurityTokenDecompressionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10679, jwtToken.Zip), ex));
            }
        }

        private byte[] DecryptToken(JsonWebToken jwtToken, CryptoProviderFactory cryptoProviderFactory, SecurityKey key)
        {
            var decryptionProvider = cryptoProviderFactory.CreateAuthenticatedEncryptionProvider(key, jwtToken.Enc);
            if (decryptionProvider == null)
                throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(TokenLogMessages.IDX10610, key, jwtToken.Enc)));

            return decryptionProvider.Decrypt(
                    Base64UrlEncoder.DecodeBytes(jwtToken.Ciphertext),
                    Encoding.ASCII.GetBytes(jwtToken.EncodedHeader),
                    Base64UrlEncoder.DecodeBytes(jwtToken.InitializationVector),
                    Base64UrlEncoder.DecodeBytes(jwtToken.AuthenticationTag));
        }

        private string EncryptToken(string innerJwt, EncryptingCredentials encryptingCredentials, string algorithm)
        {
            var cryptoProviderFactory = encryptingCredentials.CryptoProviderFactory ?? encryptingCredentials.Key.CryptoProviderFactory;

            if (cryptoProviderFactory == null)
                throw LogHelper.LogExceptionMessage(new ArgumentException(LogMessages.IDX14104));

            // if direct algorithm, look for support
            if (JwtConstants.DirectKeyUseAlg.Equals(encryptingCredentials.Alg, StringComparison.Ordinal))
            {
                if (!cryptoProviderFactory.IsSupportedAlgorithm(encryptingCredentials.Enc, encryptingCredentials.Key))
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10615, encryptingCredentials.Enc, encryptingCredentials.Key)));

                var header = new JObject();

                if (!string.IsNullOrEmpty(encryptingCredentials.Alg))
                    header.Add(JwtHeaderParameterNames.Alg, encryptingCredentials.Alg);

                if (!string.IsNullOrEmpty(encryptingCredentials.Enc))
                    header.Add(JwtHeaderParameterNames.Enc, encryptingCredentials.Enc);

                if (!string.IsNullOrEmpty(encryptingCredentials.Key.KeyId))
                    header.Add(JwtHeaderParameterNames.Kid, encryptingCredentials.Key.KeyId);

                if (!string.IsNullOrEmpty(algorithm))
                    header.Add(JwtHeaderParameterNames.Zip, algorithm);

                header.Add(JwtHeaderParameterNames.Typ, JwtConstants.HeaderType);

                var encryptionProvider = cryptoProviderFactory.CreateAuthenticatedEncryptionProvider(encryptingCredentials.Key, encryptingCredentials.Enc);
                if (encryptionProvider == null)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogMessages.IDX14103));

                byte[] plainText;
                if (!string.IsNullOrEmpty(algorithm))
                {
                    try
                    {
                        plainText = CompressToken(innerJwt, algorithm);
                    } 
                    catch (Exception ex)
                    {
                        throw LogHelper.LogExceptionMessage(new SecurityTokenCompressionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10680, algorithm), ex));
                    }
                }
                else
                {
                    plainText = Encoding.UTF8.GetBytes(innerJwt);
                }

                try
                {
                    var rawHeader = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header.ToString(Newtonsoft.Json.Formatting.None)));
                    var encryptionResult = encryptionProvider.Encrypt(plainText, Encoding.ASCII.GetBytes(rawHeader));
                    return string.Join(".", rawHeader, string.Empty, Base64UrlEncoder.Encode(encryptionResult.IV), Base64UrlEncoder.Encode(encryptionResult.Ciphertext), Base64UrlEncoder.Encode(encryptionResult.AuthenticationTag));

                }
                catch (Exception ex)
                {
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10616, encryptingCredentials.Enc, encryptingCredentials.Key), ex));
                }
            }
            else
            {
                if (!cryptoProviderFactory.IsSupportedAlgorithm(encryptingCredentials.Alg, encryptingCredentials.Key))
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10615, encryptingCredentials.Alg, encryptingCredentials.Key)));

                SymmetricSecurityKey symmetricKey = null;

                // only 128, 384 and 512 AesCbcHmac for CEK algorithm
                if (SecurityAlgorithms.Aes128CbcHmacSha256.Equals(encryptingCredentials.Enc, StringComparison.Ordinal))
                    symmetricKey = new SymmetricSecurityKey(JwtTokenUtilities.GenerateKeyBytes(256));
                else if (SecurityAlgorithms.Aes192CbcHmacSha384.Equals(encryptingCredentials.Enc, StringComparison.Ordinal))
                    symmetricKey = new SymmetricSecurityKey(JwtTokenUtilities.GenerateKeyBytes(384));
                else if (SecurityAlgorithms.Aes256CbcHmacSha512.Equals(encryptingCredentials.Enc, StringComparison.Ordinal))
                    symmetricKey = new SymmetricSecurityKey(JwtTokenUtilities.GenerateKeyBytes(512));
                else
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10617, SecurityAlgorithms.Aes128CbcHmacSha256, SecurityAlgorithms.Aes192CbcHmacSha384, SecurityAlgorithms.Aes256CbcHmacSha512, encryptingCredentials.Enc)));

                var kwProvider = cryptoProviderFactory.CreateKeyWrapProvider(encryptingCredentials.Key, encryptingCredentials.Alg);
                var wrappedKey = kwProvider.WrapKey(symmetricKey.Key);
                var encryptionProvider = cryptoProviderFactory.CreateAuthenticatedEncryptionProvider(symmetricKey, encryptingCredentials.Enc);
                if (encryptionProvider == null)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogMessages.IDX14103));

                var header = new JObject();

                if (!string.IsNullOrEmpty(encryptingCredentials.Alg))
                    header.Add(JwtHeaderParameterNames.Alg, encryptingCredentials.Alg);

                if (!string.IsNullOrEmpty(encryptingCredentials.Enc))
                    header.Add(JwtHeaderParameterNames.Enc, encryptingCredentials.Enc);

                if (!string.IsNullOrEmpty(encryptingCredentials.Key.KeyId))
                    header.Add(JwtHeaderParameterNames.Kid, encryptingCredentials.Key.KeyId);

                if (!string.IsNullOrEmpty(algorithm))
                    header.Add(JwtHeaderParameterNames.Zip, algorithm);

                header.Add(JwtHeaderParameterNames.Typ, JwtConstants.HeaderType);

                byte[] plainText; 
                if (!string.IsNullOrEmpty(algorithm))
                {
                    try
                    {
                        plainText = CompressToken(innerJwt, algorithm);
                    }
                    catch (Exception ex)
                    {
                        throw LogHelper.LogExceptionMessage(new SecurityTokenCompressionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10680, algorithm), ex));
                    }
                }
                else
                {
                    plainText = Encoding.UTF8.GetBytes(innerJwt);
                }

                try
                {
                    var rawHeader = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header.ToString(Newtonsoft.Json.Formatting.None)));
                    var encryptionResult = encryptionProvider.Encrypt(plainText, Encoding.ASCII.GetBytes(rawHeader));
                    return string.Join(".", rawHeader, Base64UrlEncoder.Encode(wrappedKey), Base64UrlEncoder.Encode(encryptionResult.IV), Base64UrlEncoder.Encode(encryptionResult.Ciphertext), Base64UrlEncoder.Encode(encryptionResult.AuthenticationTag));
                }
                catch (Exception ex)
                {
                    throw LogHelper.LogExceptionMessage(new SecurityTokenEncryptionFailedException(LogHelper.FormatInvariant(TokenLogMessages.IDX10616, encryptingCredentials.Enc, encryptingCredentials.Key), ex));
                }
            }
        }

        private IEnumerable<SecurityKey> GetAllSigningKeys(string token, TokenValidationParameters validationParameters)
        {
            LogHelper.LogInformation(TokenLogMessages.IDX10243);
            if (validationParameters.IssuerSigningKey != null)
                yield return validationParameters.IssuerSigningKey;

            if (validationParameters.IssuerSigningKeys != null)
                foreach (SecurityKey key in validationParameters.IssuerSigningKeys)
                    yield return key;
        }

        private IEnumerable<SecurityKey> GetContentEncryptionKeys(JsonWebToken jwtToken, TokenValidationParameters validationParameters)
        {
            IEnumerable<SecurityKey> keys = null;

            if (validationParameters.TokenDecryptionKeyResolver != null)
                keys = validationParameters.TokenDecryptionKeyResolver(jwtToken.EncodedToken, jwtToken, jwtToken.Kid, validationParameters);
            else
            {
                var key = ResolveTokenDecryptionKey(jwtToken.EncodedToken, jwtToken, validationParameters);
                if (key != null)
                    keys = new List<SecurityKey> { key };
            }

            // control gets here if:
            // 1. User specified delegate: TokenDecryptionKeyResolver returned null
            // 2. ResolveTokenDecryptionKey returned null
            // Try all the keys. This is the degenerate case, not concerned about perf.
            if (keys == null)
                keys = JwtTokenUtilities.GetAllDecryptionKeys(validationParameters);

            if (jwtToken.Alg.Equals(JwtConstants.DirectKeyUseAlg))
                return keys;

            var unwrappedKeys = new List<SecurityKey>();
            foreach (var key in keys)
            {
                if (key.CryptoProviderFactory.IsSupportedAlgorithm(jwtToken.Alg, key))
                {
                    var kwp = key.CryptoProviderFactory.CreateKeyWrapProviderForUnwrap(key, jwtToken.Alg);
                    var unwrappedKey = kwp.UnwrapKey(Base64UrlEncoder.DecodeBytes(jwtToken.EncryptedKey));
                    unwrappedKeys.Add(new SymmetricSecurityKey(unwrappedKey));
                }
            }

            return unwrappedKeys;
        }

        /// <summary>
        /// Returns a <see cref="SecurityKey"/> to use when validating the signature of a token.
        /// </summary>
        /// <param name="jwtToken">The <see cref="JsonWebToken"/> that is being validated.</param>
        /// <param name="validationParameters">A <see cref="TokenValidationParameters"/>  required for validation.</param>
        /// <returns>Returns a <see cref="SecurityKey"/> to use for signature validation.</returns>
        /// <remarks>If key fails to resolve, then null is returned</remarks>
        internal virtual SecurityKey ResolveIssuerSigningKey(JsonWebToken jwtToken, TokenValidationParameters validationParameters)
        {
            if (validationParameters == null)
                throw LogHelper.LogArgumentNullException(nameof(validationParameters));

            if (jwtToken == null)
                throw LogHelper.LogArgumentNullException(nameof(jwtToken));

            return JwtTokenUtilities.FindKeyMatch(jwtToken.Kid, jwtToken.X5t, validationParameters.IssuerSigningKey, validationParameters.IssuerSigningKeys);
        }

        /// <summary>
        /// Returns a <see cref="SecurityKey"/> to use when decrypting a JWE.
        /// </summary>
        /// <param name="token">The <see cref="string"/> the token that is being decrypted.</param>
        /// <param name="jwtToken">The <see cref="JsonWebToken"/> that is being decrypted.</param>
        /// <param name="validationParameters">A <see cref="TokenValidationParameters"/>  required for validation.</param>
        /// <returns>Returns a <see cref="SecurityKey"/> to use for signature validation.</returns>
        /// <remarks>If key fails to resolve, then null is returned</remarks>
        protected virtual SecurityKey ResolveTokenDecryptionKey(string token, JsonWebToken jwtToken, TokenValidationParameters validationParameters)
        {
            if (jwtToken == null)
                throw LogHelper.LogArgumentNullException(nameof(jwtToken));

            if (validationParameters == null)
                throw LogHelper.LogArgumentNullException(nameof(validationParameters));

            return JwtTokenUtilities.FindKeyMatch(jwtToken.Kid, jwtToken.X5t, validationParameters.TokenDecryptionKey, validationParameters.TokenDecryptionKeys);
        }

        /// <summary>
        /// Converts a string into an instance of <see cref="JsonWebToken"/>.
        /// </summary>
        /// <param name="token">A 'JSON Web Token' (JWT) in JWS or JWE Compact Serialization Format.</param>
        /// <returns>A <see cref="JsonWebToken"/></returns>
        /// <exception cref="ArgumentNullException">'token' is null or empty.</exception>
        /// <exception cref="ArgumentException">'token.Length' is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
        /// <remarks><para>If the 'token' is in JWE Compact Serialization format, only the protected header will be deserialized.</para>
        /// This method is unable to decrypt the payload. Use <see cref="ValidateToken(string, TokenValidationParameters)"/>to obtain the payload.</remarks>
        public virtual JsonWebToken ReadJsonWebToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw LogHelper.LogArgumentNullException(nameof(token));

            if (token.Length> MaximumTokenSizeInBytes)
                throw LogHelper.LogExceptionMessage(new ArgumentException(LogHelper.FormatInvariant(TokenLogMessages.IDX10209, token.Length, MaximumTokenSizeInBytes)));

            return new JsonWebToken(token);
        }

        /// <summary>
        /// Converts a string into an instance of <see cref="JsonWebToken"/>.
        /// </summary>
        /// <param name="token">A 'JSON Web Token' (JWT) in JWS or JWE Compact Serialization Format.</param>
        /// <returns>A <see cref="JsonWebToken"/></returns>
        /// <exception cref="ArgumentNullException">'token' is null or empty.</exception>
        /// <exception cref="ArgumentException">'token.Length' is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
        public virtual SecurityToken ReadToken(string token)
        {
            return ReadJsonWebToken(token);
        }

        /// <summary>
        /// Validates a JWS or a JWE.
        /// </summary>
        /// <param name="token">A 'JSON Web Token' (JWT) in JWS or JWE Compact Serialization Format.</param>
        /// <param name="validationParameters">A <see cref="TokenValidationParameters"/>  required for validation.</param>
        /// <returns>A <see cref="TokenValidationResult"/></returns>
        public virtual TokenValidationResult ValidateToken(string token, TokenValidationParameters validationParameters)
        {
            if (string.IsNullOrEmpty(token))
                return new TokenValidationResult { Exception = LogHelper.LogArgumentNullException(nameof(token)) };

            if (validationParameters == null)
                return new TokenValidationResult { Exception = LogHelper.LogArgumentNullException(nameof(validationParameters)) };

            if (token.Length > MaximumTokenSizeInBytes)
                return new TokenValidationResult { Exception = LogHelper.LogExceptionMessage(new ArgumentException(LogHelper.FormatInvariant(TokenLogMessages.IDX10209, token.Length, MaximumTokenSizeInBytes))) };

            var tokenParts = token.Split(new char[] { '.' }, JwtConstants.MaxJwtSegmentCount + 1);
            if (tokenParts.Length != JwtConstants.JwsSegmentCount && tokenParts.Length != JwtConstants.JweSegmentCount)
                return new TokenValidationResult { Exception = LogHelper.LogExceptionMessage(new ArgumentException(LogHelper.FormatInvariant(LogMessages.IDX14111, token))) };

            try
            {
                if (tokenParts.Length == JwtConstants.JweSegmentCount)
                {
                    var jwtToken = new JsonWebToken(token);
                    var decryptedJwt = DecryptToken(jwtToken, validationParameters);
                    var innerToken = ValidateSignature(decryptedJwt, validationParameters);
                    jwtToken.InnerToken = innerToken;
                    var innerTokenValidationResult = ValidateTokenPayload(innerToken, validationParameters);
                    return new TokenValidationResult
                    {
                        SecurityToken = jwtToken,
                        ClaimsIdentity = innerTokenValidationResult.ClaimsIdentity,
                        IsValid = true
                    };
                }
                else
                {
                    var jsonWebToken = ValidateSignature(token, validationParameters);
                    return ValidateTokenPayload(jsonWebToken, validationParameters);
                }
            } 
            catch (Exception ex)
            {
                return new TokenValidationResult
                {
                    Exception = ex
                };
            }     
        }

        private TokenValidationResult ValidateTokenPayload(JsonWebToken jsonWebToken, TokenValidationParameters validationParameters)
        {
            var expires = (jsonWebToken.ValidTo == null) ? null : new DateTime?(jsonWebToken.ValidTo);
            var notBefore = (jsonWebToken.ValidFrom == null) ? null : new DateTime?(jsonWebToken.ValidFrom);

            Validators.ValidateLifetime(notBefore, expires, jsonWebToken, validationParameters);
            Validators.ValidateAudience(jsonWebToken.Audiences, jsonWebToken, validationParameters);
            var issuer = Validators.ValidateIssuer(jsonWebToken.Issuer, jsonWebToken, validationParameters);
            Validators.ValidateTokenReplay(expires, jsonWebToken.EncodedToken, validationParameters);
            if (validationParameters.ValidateActor && !string.IsNullOrWhiteSpace(jsonWebToken.Actor))
            {
                var actorValidationResult =  ValidateToken(jsonWebToken.Actor, validationParameters.ActorValidationParameters ?? validationParameters);
            }

            Validators.ValidateIssuerSecurityKey(jsonWebToken.SigningKey, jsonWebToken, validationParameters);

            return new TokenValidationResult
            {
                SecurityToken = jsonWebToken,
                ClaimsIdentity = CreateClaimsIdentity(jsonWebToken, validationParameters),
                IsValid = true
            };
        }

        /// <summary>
        /// Validates the JWT signature.
        /// </summary>
        private JsonWebToken ValidateSignature(string token, TokenValidationParameters validationParameters)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw LogHelper.LogArgumentNullException(nameof(token));

            if (validationParameters == null)
                throw LogHelper.LogArgumentNullException(nameof(validationParameters));

            if (validationParameters.SignatureValidator != null)
            {
                var validatedToken = validationParameters.SignatureValidator(token, validationParameters);
                if (validatedToken == null)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10505, token)));

                var validatedJsonWebToken = validatedToken as JsonWebToken;
                if (validatedJsonWebToken == null)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10506, typeof(JsonWebToken), validatedJsonWebToken.GetType(), token)));

                return validatedJsonWebToken;
            }

            JsonWebToken jwtToken = null;

            if (validationParameters.TokenReader != null)
            {
                var securityToken = validationParameters.TokenReader(token, validationParameters);
                if (securityToken == null)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10510, token)));

                jwtToken = securityToken as JsonWebToken;
                if (jwtToken == null)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10509, typeof(JsonWebToken), securityToken.GetType(), token)));
            }
            else
            {
                jwtToken = new JsonWebToken(token);
            }

            var encodedBytes = Encoding.UTF8.GetBytes(jwtToken.EncodedHeader + "." + jwtToken.EncodedPayload);
            if (string.IsNullOrEmpty(jwtToken.EncodedSignature))
            {
                if (validationParameters.RequireSignedTokens)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10504, token)));
                else
                    return jwtToken;
            }

            var kidMatched = false;
            IEnumerable<SecurityKey> keys = null;
            if (validationParameters.IssuerSigningKeyResolver != null)
            {
                keys = validationParameters.IssuerSigningKeyResolver(token, jwtToken, jwtToken.Kid, validationParameters);
            }
            else
            {
                var key = ResolveIssuerSigningKey(jwtToken, validationParameters);
                if (key != null)
                {
                    kidMatched = true;
                    keys = new List<SecurityKey> { key };
                }
            }

            if (keys == null)
            {
                // control gets here if:
                // 1. User specified delegate: IssuerSigningKeyResolver returned null
                // 2. ResolveIssuerSigningKey returned null
                // Try all the keys. This is the degenerate case, not concerned about perf.
                keys = GetAllSigningKeys(token, validationParameters);
            }

            // keep track of exceptions thrown, keys that were tried
            var exceptionStrings = new StringBuilder();
            var keysAttempted = new StringBuilder();
            var kidExists = !string.IsNullOrEmpty(jwtToken.Kid);
            byte[] signatureBytes;

            try
            {
                signatureBytes = Base64UrlEncoder.DecodeBytes(jwtToken.EncodedSignature);
            }
            catch (FormatException e)
            {
                throw new SecurityTokenInvalidSignatureException(TokenLogMessages.IDX10508, e);
            }

            foreach (var key in keys)
            {
                try
                {
                    if (ValidateSignature(encodedBytes, signatureBytes, key, jwtToken.Alg, validationParameters))
                    {
                        LogHelper.LogInformation(TokenLogMessages.IDX10242, token);
                        jwtToken.SigningKey = key;
                        return jwtToken;
                    };
                }
                catch (Exception ex)
                {
                    exceptionStrings.AppendLine(ex.ToString());
                }

                if (key != null)
                {
                    keysAttempted.AppendLine(key.ToString() + " , KeyId: " + key.KeyId);
                    if (kidExists && !kidMatched && key.KeyId != null)
                        kidMatched = jwtToken.Kid.Equals(key.KeyId, key is X509SecurityKey ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                }
            }

            if (kidExists)
            {
                if (kidMatched)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10511, keysAttempted, jwtToken.Kid, exceptionStrings, jwtToken)));
                else
                    throw LogHelper.LogExceptionMessage(new SecurityTokenSignatureKeyNotFoundException(LogHelper.FormatInvariant(TokenLogMessages.IDX10501, jwtToken.Kid, jwtToken)));
            }
            else
            {
                if (keysAttempted.Length > 0)
                    throw LogHelper.LogExceptionMessage(new SecurityTokenInvalidSignatureException(LogHelper.FormatInvariant(TokenLogMessages.IDX10503, keysAttempted, exceptionStrings, jwtToken)));
                else
                    throw LogHelper.LogExceptionMessage(new SecurityTokenSignatureKeyNotFoundException(TokenLogMessages.IDX10500));
            }
        }

        /// <summary>
        /// Obtains a <see cref="SignatureProvider "/> and validates the signature.
        /// </summary>
        /// <param name="encodedBytes">Bytes to validate.</param>
        /// <param name="signature">Signature to compare against.</param>
        /// <param name="key"><See cref="SecurityKey"/> to use.</param>
        /// <param name="algorithm">Crypto algorithm to use.</param>
        /// <param name="validationParameters">Priority will be given to <see cref="TokenValidationParameters.CryptoProviderFactory"/> over <see cref="SecurityKey.CryptoProviderFactory"/>.</param>
        /// <returns>'true' if signature is valid.</returns>
        private bool ValidateSignature(byte[] encodedBytes, byte[] signature, SecurityKey key, string algorithm, TokenValidationParameters validationParameters)
        {
            var cryptoProviderFactory = validationParameters.CryptoProviderFactory ?? key.CryptoProviderFactory;
            if (!cryptoProviderFactory.IsSupportedAlgorithm(algorithm, key))
            {
                LogHelper.LogInformation(LogMessages.IDX14000, algorithm, key);
                return false;
            }

            var signatureProvider = cryptoProviderFactory.CreateForVerifying(key, algorithm);
            if (signatureProvider == null)
                throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(TokenLogMessages.IDX10647, (key == null ? "Null" : key.ToString()), (algorithm == null ? "Null" : algorithm))));

            try
            {
                return signatureProvider.Verify(encodedBytes, signature);
            }
            finally
            {
                cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);
            }
        }
    }
}
