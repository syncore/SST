﻿using System;

namespace SSB.Model
{
    /// <summary>
    /// Model class that represents player ban information.
    /// </summary>
    public class BanInfo
    {
        /// <summary>
        /// Gets or sets the name of the banned player.
        /// </summary>
        /// <value>
        /// The name of the banned player.
        /// </value>
        public string PlayerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the admin who set the ban.
        /// </summary>
        /// <value>
        /// The name of the admin who set the ban.
        /// </value>
        public string BannedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time on which the ban was added.
        /// </summary>
        /// <value>
        /// The date and time on which the ban was added.
        /// </value>
        public DateTime BanAddedDate { get; set; }

        /// <summary>
        /// Gets or sets the ban expiration date and time.
        /// </summary>
        /// <value>
        /// The ban expiration date and time.
        /// </value>
        public DateTime BanExpirationDate { get; set; }
    }
}