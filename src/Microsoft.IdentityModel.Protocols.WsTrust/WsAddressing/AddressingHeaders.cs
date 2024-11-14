using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.IdentityModel.Protocols.WsAddressing;

namespace Microsoft.IdentityModel.Protocols.WsTrust.WsAddressing
{
    /// <summary>
    /// Base class for all Ws-addressing Message Headers
    /// </summary>
    public class AddressingHeaders
    {
        /// <summary>
        /// Action Header
        /// </summary>
        public ActionHeader ActionHeader { get; set; }

        /// <summary>
        /// MessageID Header
        /// </summary>
        public MessageIDHeader MessageIDHeader { get; set; }

        /// <summary>
        /// ReplyTo Header
        /// </summary>
        public ReplyToHeader ReplyToHeader { get; set; }

        /// <summary>
        /// To Header
        /// </summary>
        public ToHeader ToHeader { get; set; }
    }

    /// <summary>
    /// Action Header
    /// </summary>
    public class ActionHeader
    {
        private const bool mustUnderstandValue = true;

        /// <summary>
        /// Gets Action
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action"></param>
        public ActionHeader(string action)
        {
            Action = action;
        }

        /// <summary>
        /// Gets MustUnderstand
        /// </summary>
        public static bool MustUnderstand
        {
            get { return mustUnderstandValue; }
        }
    }

    /// <summary>
    /// To Header
    /// </summary>
    public class ToHeader
    {
        private const bool mustUnderstandValue = true;

        /// <summary>
        /// Gets To
        /// </summary>
        public Uri To { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="to"></param>
        public ToHeader(Uri to)
        {
            To = to;
        }

        /// <summary>
        /// Gets MustUnderstand
        /// </summary>
        public static bool MustUnderstand
        {
            get { return mustUnderstandValue; }
        }
    }

    /// <summary>
    /// ReplyTo Header
    /// </summary>
    public class ReplyToHeader
    {
        private const bool mustUnderstandValue = false;

        /// <summary>
        /// Gets ReplyTo
        /// </summary>
        public EndpointReference ReplyTo { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replyTo"></param>
        public ReplyToHeader(EndpointReference replyTo)
        {
            ReplyTo = replyTo;
        }

        /// <summary>
        /// Gets MustUnderstand
        /// </summary>
        public static bool MustUnderstand
        {
            get { return mustUnderstandValue; }
        }
    }

    /// <summary>
    /// MessageID Header
    /// </summary>
    public class MessageIDHeader
    {
        private const bool mustUnderstandValue = false;

        /// <summary>
        /// Gets MessageID
        /// </summary>
        public UniqueId MessageId { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageId"></param>
        public MessageIDHeader(UniqueId messageId)
        {
            MessageId = messageId;
        }

        /// <summary>
        /// Gets MustUnderstand
        /// </summary>
        public static bool MustUnderstand
        {
            get { return mustUnderstandValue; }
        }
    }
}
