﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.Protocols.WsTrust.WsUtility
{
    /// <summary>
    /// The Timestamp element provides a mechanism for expressing the creation and expiration times of the security semantics in a message.
    /// <para>see: http://docs.oasis-open.org/wss-m/wss/v1.1.1/os/wss-SOAPMessageSecurity-v1.1.1-os.html (1.1.1) </para>
    /// </summary>
    public class SecurityTimestamp
    {
        private DateTime? _created;
        private DateTime? _expires;

        /// <summary>
        /// Creates an instance of a <see cref="SecurityTimestamp"/>.
        /// The Timestamp element provides a mechanism for expressing the creation and expiration times of the security semantics in a message.
        /// </summary>
        /// <param name="created">creation time, will be converted to UTC.</param>
        /// <param name="expires">expiration time will be converted to UTC.</param>
        /// <param name="id"></param>
        /// <remarks>Value will be stored in UTC.</remarks>
        public SecurityTimestamp(DateTime created, DateTime expires, string id)
            : this((DateTime?)created, (DateTime?)expires, id)
        {
        }

        /// <summary>
        /// Creates an instance of a <see cref="SecurityTimestamp"/>.
        /// The Timestamp element provides a mechanism for expressing the creation and expiration times of the security semantics in a message.
        /// </summary>
        /// <param name="created">creation time, will be converted to UTC.</param>
        /// <param name="expires">expiration time will be converted to UTC.</param>
        /// <param name="id"></param>
        /// <remarks>Value will be stored in UTC.</remarks>
        public SecurityTimestamp(DateTime? created, DateTime? expires, string id)
        {
            if (created.HasValue && expires.HasValue && expires.Value <= created.Value)
                LogHelper.LogWarning(LogMessages.IDX15500);

            if (created.HasValue)
                Created = created.Value.ToUniversalTime();

            if (expires.HasValue)
                Expires = expires.Value.ToUniversalTime();

            Id = id;
        }

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        /// <remarks>Value will be stored in UTC.</remarks>
        public DateTime? Created
        {
            get => _created;
            set => _created = (value.HasValue) ? _created = value.Value.ToUniversalTime() : value;
        }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        /// <remarks>Value will be stored in UTC.</remarks>
        public DateTime? Expires
        {
            get => _expires;
            set => _expires = (value.HasValue) ? _expires = value.Value.ToUniversalTime() : value;
        }

        /// <summary>
        /// Gets the Id of SecurityTimestamp element
        /// </summary>
        public string Id { get; }
    }
}