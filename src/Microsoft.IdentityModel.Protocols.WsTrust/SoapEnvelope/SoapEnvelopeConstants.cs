using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.IdentityModel.Protocols.WsTrust.SoapEnvelope
{
    /// <summary>
    /// Constants: SOAP Envelope namespace and prefix.
    /// <para>see: https://www.w3.org/2003/05/soap-envelope/ </para>
    /// </summary>
    public abstract class SoapEnvelopeConstants : WsConstantsBase
    {
        /// <summary>
        /// Gets the list of namespaces that are recognized by this runtime.
        /// </summary>
        public static IList<string> KnownNamespaces { get; } = new List<string> { "http://www.w3.org/2003/05/soap-envelope" };

        /// <summary>
        /// Gets constants for SOAP Envelope 1.2
        /// </summary>
        public static SoapEnvelope12Constants SoapEnvelope12Constants { get; } = new SoapEnvelope12Constants();
    }

    /// <summary>
    /// Constants: SOAP Envelope 1.2 namespace and prefix.
    /// </summary>
    public class SoapEnvelope12Constants : SoapEnvelopeConstants
    {
        /// <summary>
        /// Instantiates SOAP Envelope 1.2
        /// </summary>
        public SoapEnvelope12Constants()
        {
            Namespace = "http://www.w3.org/2003/05/soap-envelope";
            Prefix = "s";
        }
    }
}
