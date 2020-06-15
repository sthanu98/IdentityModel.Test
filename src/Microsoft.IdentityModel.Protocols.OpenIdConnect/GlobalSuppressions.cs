﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~M:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.CreateLogoutRequestUrl~System.String")]
[assembly: SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~M:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.CreateAuthenticationRequestUrl~System.String")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration.JwksUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration.OpPolicyUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration.OpTosUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.ErrorUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.PostLogoutRedirectUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.TargetLinkUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.RedirectUri")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Previously released as returing a string", Scope = "member", Target = "~P:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage.RequestUri")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Globalization is not used in the project")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Previously released as non-static", Scope = "member", Target = "~M:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration.ShouldSerializeSigningKeys~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Previously released as explicit implementation", Scope = "member", Target = "~M:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever.Microsoft#IdentityModel#Protocols#IConfigurationRetriever<Microsoft#IdentityModel#Protocols#OpenIdConnect#OpenIdConnectConfiguration>#GetConfigurationAsync(System.String,Microsoft.IdentityModel.Protocols.IDocumentRetriever,System.Threading.CancellationToken)~System.Threading.Tasks.Task{Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration}")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Normalized only for display", Scope = "member", Target = "~M:Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectProtocolValidator.ValidateIdToken(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectProtocolValidationContext)")]
