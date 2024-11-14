using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.IdentityModel.Protocols.WsTrust.SoapEnvelope
{
    /// <summary>
    /// Constants: SOAP Envelope elements names.
    /// <para>see: https://www.w3.org/TR/soap12-part1/ </para>
    /// <para>see: https://www.w3.org/2003/05/soap-envelope/ </para>
    /// </summary>
    public static class SoapEnvelopeElements
    {
        /// <summary>
        /// Gets the value for "Envelope"
        /// </summary>
        public const string Envelope = "Envelope";

        /// <summary>
        /// Gets the value for "Header"
        /// </summary>
        public const string Header = "Header";

        /// <summary>
        /// Gets the value for "Body"
        /// </summary>
        public const string Body = "Body";
    }
}
