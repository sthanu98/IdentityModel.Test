using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsTrust.WsUtility;
using System.Security.Cryptography.X509Certificates;
using static Microsoft.IdentityModel.Protocols.WsTrust.WsSecurity.SignedXmlInternal;

namespace Microsoft.IdentityModel.Protocols.WsTrust.WsSecurity
{
    /// <summary>
    /// Computes Signature
    /// </summary>
    public static class WsSecuritySendSecurityHeader
    {
        // For Transport Security we have to sign the 'To' header with the 
        // supporting tokens.
        //private Stream _toHeaderStream = null;
        //private string _toHeaderId = null;

        /// <summary>
        /// Generates Signature for the Xml document using the provided certificate
        /// </summary>
        /// <param name="document"></param>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static XmlElement CreateSignature(XmlDocument document, X509Certificate2 certificate)
        {
            var signedXml = new SignedXMLInternal(document)
            {
                SigningKey = certificate.GetRSAPrivateKey()
            };

            // TODO: Consider adding a SecurityTokenReference here
            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data keyInfoData = new KeyInfoX509Data(certificate);
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // TODO: Fix these hardcoded values
            var referenceSecurityTimestamp = new Reference
            {
                Uri = "#_0"
            };
            referenceSecurityTimestamp.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(referenceSecurityTimestamp);

            var referenceToHeader = new Reference
            {
                Uri = "#_1"
            };
            referenceToHeader.AddTransform(new XmlDsigExcC14NTransform());
            signedXml.AddReference(referenceToHeader);

            // TODO: Add constant for hardcoded string
            signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";

            signedXml.ComputeSignature();

            var xmlDigitalSignature = signedXml.GetXml();
            return xmlDigitalSignature;
        }
    }
}
