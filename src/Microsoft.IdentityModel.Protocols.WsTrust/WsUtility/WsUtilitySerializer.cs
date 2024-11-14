using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.IdentityModel.Protocols.WsUtility;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsSecurity;

namespace Microsoft.IdentityModel.Protocols.WsTrust.WsUtility
{
    /// <summary>
    /// Base class for support of serializing versions of WS-Security.
    /// see: https://www.oasis-open.org/committees/download.php/16790/wss-v1.1-spec-os-SOAPMessageSecurity.pdf (1.1)
    /// see: http://docs.oasis-open.org/wss-m/wss/v1.1.1/os/wss-SOAPMessageSecurity-v1.1.1-os.html (1.1.1)
    /// </summary>
    public static class WsUtilitySerializer
    {
        internal const string GeneratedDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffZ";

        internal static void WriteTimestamp(XmlDictionaryWriter writer, WsSerializationContext serializationContext, Timestamp timestamp)
        {
            writer.WriteStartElement(serializationContext.UtilityConstants.Prefix, WsUtilityElements.Timestamp, serializationContext.SecurityConstants.Namespace);
            writer.WriteAttributeString(WsUtilityAttributes.Id, "_0");
            if (timestamp.Created.HasValue)
            {
                writer.WriteStartElement(serializationContext.UtilityConstants.Prefix, WsUtilityElements.Created, serializationContext.UtilityConstants.Prefix);
                writer.WriteString(XmlConvert.ToString(timestamp.Created.Value.ToUniversalTime(), GeneratedDateTimeFormat));
                writer.WriteEndElement();
            }

            if (timestamp.Expires.HasValue)
            {
                writer.WriteStartElement(serializationContext.UtilityConstants.Prefix, WsUtilityElements.Expires, serializationContext.UtilityConstants.Prefix);
                writer.WriteString(XmlConvert.ToString(timestamp.Expires.Value.ToUniversalTime(), GeneratedDateTimeFormat));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
