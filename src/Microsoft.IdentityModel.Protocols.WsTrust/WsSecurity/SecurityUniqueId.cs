using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.IdentityModel.Protocols.WsTrust.WsSecurity
{
    internal class SecurityUniqueId
    {
        private static long s_nextId = 0;
        private static readonly string s_commonPrefix = "uuid-" + Guid.NewGuid().ToString() + "-";
        private readonly long _id;
        private readonly string _prefix;
        private string _val;

        private SecurityUniqueId(string prefix, long id)
        {
            _id = id;
            _prefix = prefix;
            _val = null;
        }

        public static SecurityUniqueId Create()
        {
            return Create(s_commonPrefix);
        }

        public static SecurityUniqueId Create(string prefix)
        {
            return new SecurityUniqueId(prefix, Interlocked.Increment(ref s_nextId));
        }

        public string Value
        {
            get
            {
                if (_val == null)
                {
                    _val = _prefix + _id.ToString(CultureInfo.InvariantCulture);
                }

                return _val;
            }
        }
    }
}
