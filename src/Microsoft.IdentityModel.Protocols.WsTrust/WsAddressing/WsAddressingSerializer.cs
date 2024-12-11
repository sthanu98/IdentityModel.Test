//------------------------------------------------------------------------------
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

using System.Xml;
using System.Xml.Serialization;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.WsTrust;
using Microsoft.IdentityModel.Protocols.WsTrust.SoapEnvelope;
using Microsoft.IdentityModel.Protocols.WsTrust.WsAddressing;
using Microsoft.IdentityModel.Protocols.WsUtility;
using Microsoft.IdentityModel.Xml;

namespace Microsoft.IdentityModel.Protocols.WsAddressing
{
    /// <summary>
    /// Base class for support of serializing versions of WS-Addressing.
    /// </summary>
    internal class WsAddressingSerializer
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public WsAddressingSerializer()
        {
        }

        /// <summary>
        /// Reads an <see cref="EndpointReference"/>
        /// </summary>
        /// <param name="reader">The xml dictionary reader.</param>
        /// <returns>An <see cref="EndpointReference"/> instance.</returns>
        public virtual EndpointReference ReadEndpointReference(XmlDictionaryReader reader)
        {
            //  <wsa:EndpointReference>
            //    <wsa:Address>xs:anyURI</wsa:Address>
            //    <wsa:ReferenceProperties>... </wsa:ReferenceProperties> ?
            //    <wsa:ReferenceParameters>... </wsa:ReferenceParameters> ?
            //    <wsa:PortType>xs:QName</wsa:PortType> ?
            //    <wsa:ServiceName PortName="xs:NCName"?>xs:QName</wsa:ServiceName> ?
            //    <wsp:Policy> ... </wsp:Policy>*
            //  </wsa:EndpointReference>

            XmlUtil.CheckReaderOnEntry(reader, WsAddressingElements.EndpointReference);
            foreach (string @namespace in WsAddressingConstants.KnownNamespaces)
            {
                if (reader.IsNamespaceUri(@namespace))
                {
                    bool isEmptyElement = reader.IsEmptyElement;
                    reader.ReadStartElement();
                    var endpointReference = new EndpointReference(reader.ReadElementContentAsString());
                    while (reader.IsStartElement())
                    {
                        bool isInnerEmptyElement = reader.IsEmptyElement;
                        XmlReader subtreeReader = reader.ReadSubtree();
                        var doc = new XmlDocument
                        {
                            PreserveWhitespace = true
                        };

                        doc.Load(subtreeReader);
                        endpointReference.AdditionalXmlElements.Add(doc.DocumentElement);
                        if (!isInnerEmptyElement)
                            reader.ReadEndElement();
                    }

                    if (!isEmptyElement)
                        reader.ReadEndElement();

                    return endpointReference;
                }
            }

            throw LogHelper.LogExceptionMessage(new XmlReadException(LogHelper.FormatInvariant(WsTrust.LogMessages.IDX15001, WsAddressingElements.EndpointReference, WsAddressingConstants.Addressing200408.Namespace, WsAddressingConstants.Addressing10.Namespace, reader.NamespaceURI)));
        }

        public static void WriteEndpointReference(XmlDictionaryWriter writer, WsSerializationContext serializationContext, EndpointReference endpointReference)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, endpointReference, nameof(endpointReference));

            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.EndpointReference, serializationContext.AddressingConstants.Namespace);
            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.Address, serializationContext.AddressingConstants.Namespace);
            writer.WriteString(endpointReference.Uri);
            writer.WriteEndElement();

            foreach (XmlElement element in endpointReference.AdditionalXmlElements)
                element.WriteTo(writer);

            writer.WriteEndElement();
        }

        public static void WriteMessageInfoHeaders(XmlDictionaryWriter writer, WsSerializationContext serializationContext, ActionHeader actionHeader, MessageIDHeader messageIDHeader, ToHeader toHeader, ReplyToHeader replyToHeader)
        {
            WriteActionHeader(writer, serializationContext, actionHeader);
            WriteMessageIDHeader(writer, serializationContext, messageIDHeader);
            WriteToHeader(writer, serializationContext, toHeader);
            WriteReplyToHeader(writer, serializationContext, replyToHeader);
        }

        public static void WriteActionHeader(XmlDictionaryWriter writer, WsSerializationContext serializationContext, ActionHeader actionHeader)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, actionHeader, nameof(actionHeader));

            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.Action, serializationContext.AddressingConstants.Namespace);
            if (ActionHeader.MustUnderstand)
            {
                writer.WriteAttributeString(SoapEnvelopeAttributes.MustUnderstand, SoapEnvelopeConstants.SoapEnvelope12Constants.Namespace, "1");
            }
            writer.WriteString(actionHeader.Action);
            writer.WriteEndElement();
        }

        public static void WriteMessageIDHeader(XmlDictionaryWriter writer, WsSerializationContext serializationContext, MessageIDHeader messageIDHeader)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, messageIDHeader, nameof(messageIDHeader));

            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.MessageId, serializationContext.AddressingConstants.Namespace);
            if (MessageIDHeader.MustUnderstand)
            {
                writer.WriteAttributeString(SoapEnvelopeAttributes.MustUnderstand, SoapEnvelopeConstants.SoapEnvelope12Constants.Namespace, "1");
            }
            writer.WriteString(messageIDHeader.MessageId.ToString());
            writer.WriteEndElement();
        }

        public static void WriteToHeader(XmlDictionaryWriter writer, WsSerializationContext serializationContext, ToHeader toHeader)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, toHeader, nameof(toHeader));

            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.To, serializationContext.AddressingConstants.Namespace);
            if (ToHeader.MustUnderstand)
            {
                writer.WriteAttributeString(SoapEnvelopeAttributes.MustUnderstand, SoapEnvelopeConstants.SoapEnvelope12Constants.Namespace, "1");
            }
            // TODO: Add ID attribute from ws-security
            writer.WriteAttributeString(WsUtilityAttributes.Id, WsUtilityConstants.WsUtility10.Namespace, "_1");
            writer.WriteString(toHeader.To.ToString());
            writer.WriteEndElement();
        }

        public static void WriteReplyToHeader(XmlDictionaryWriter writer, WsSerializationContext serializationContext, ReplyToHeader replyToHeader)
        {
            WsUtils.ValidateParamsForWritting(writer, serializationContext, replyToHeader, nameof(replyToHeader));

            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.ReplyTo, serializationContext.AddressingConstants.Namespace);
            writer.WriteStartElement(serializationContext.AddressingConstants.Prefix, WsAddressingElements.Address, serializationContext.AddressingConstants.Namespace);
            writer.WriteString(replyToHeader.ReplyTo.Uri.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
