using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.IdentityModel.Protocols.WsTrust.WsAddressing;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsAddressing;

namespace Microsoft.IdentityModel.Protocols.WsTrust.SoapEnvelope
{
    /// <summary>
    /// Serializer for Soap Envelope
    /// </summary>
    public static class SoapEnvelopeSerializer
    {
        /// <summary>
        /// Write Soap Envelope Elements
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serializationContext"></param>
        /// <param name="addressingHeaders"></param>
        /// <param name="wsTrustRequest"></param>
        public static void WriteSoapEnvelopeHeaders(XmlDictionaryWriter writer, WsSerializationContext serializationContext, AddressingHeaders addressingHeaders, WsTrustRequest wsTrustRequest)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, addressingHeaders, nameof(addressingHeaders));
            WsUtils.ValidateParamsForWritting(writer, serializationContext, wsTrustRequest, nameof(wsTrustRequest));

            writer.WriteStartElement(serializationContext.SoapEnvelopeConstants.Prefix, SoapEnvelopeElements.Envelope, serializationContext.SoapEnvelopeConstants.Namespace);

            // Header element
            writer.WriteStartElement(serializationContext.SoapEnvelopeConstants.Prefix, SoapEnvelopeElements.Header, serializationContext.SoapEnvelopeConstants.Namespace);
            WsAddressingSerializer.WriteMessageInfoHeaders(writer, serializationContext, addressingHeaders.ActionHeader, addressingHeaders.MessageIDHeader, addressingHeaders.ToHeader, addressingHeaders.ReplyToHeader);
            // TODO: Add Security Header
            writer.WriteEndElement();


            // Body element
            writer.WriteStartElement(serializationContext.SoapEnvelopeConstants.Prefix, SoapEnvelopeElements.Body, serializationContext.SoapEnvelopeConstants.Namespace);
            var serializer = new WsTrustSerializer();
            serializer.WriteRequest(writer, WsTrustVersion.Trust13, wsTrustRequest);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        /// <summary>
        /// Test
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serializationContext"></param>
        /// <param name="addressingHeaders"></param>
        public static void WriteSoapEnvelopeHeaders(XmlDictionaryWriter writer, WsSerializationContext serializationContext, AddressingHeaders addressingHeaders)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, addressingHeaders, nameof(addressingHeaders));

            WsAddressingSerializer.WriteMessageInfoHeaders(writer, serializationContext, addressingHeaders.ActionHeader, addressingHeaders.MessageIDHeader, addressingHeaders.ToHeader, addressingHeaders.ReplyToHeader);
            // TODO: Add Security Header
        }
    }
}
