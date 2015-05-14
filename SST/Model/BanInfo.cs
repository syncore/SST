using System;
using System.Globalization;
using SST.Enums;

namespace SST.Model
{
    /// <summary>
    /// Model class that represents player ban information.
    /// </summary>
    public class BanInfo
    {
        /// <summary>
        /// Gets or sets the date and time on which the ban was added.
        /// </summary>
        /// <value>The date and time on which the ban was added.</value>
        public DateTime BanAddedDate { get; set; }

        /// <summary>
        /// Gets or sets the ban expiration date and time.
        /// </summary>
        /// <value>The ban expiration date and time.</value>
        public DateTime BanExpirationDate { get; set; }

        /// <summary>
        /// Gets the ban format display.
        /// </summary>
        /// <value>The ban format display.</value>
        public string BanFormatDisplay
        {
            get
            {
                return string.Format("{0} by {1} expires: {2}",
                    PlayerName, BannedBy, BanExpirationDate.
                    ToString("G", DateTimeFormatInfo.InvariantInfo));
            }
        }

        /// <summary>
        /// Gets or sets the name of the admin who set the ban.
        /// </summary>
        /// <value>The name of the admin who set the ban.</value>
        public string BannedBy { get; set; }

        /// <summary>
        /// Gets or sets the type of ban.
        /// </summary>
        /// <value>The type of ban.</value>
        public BanType BanType { get; set; }

        /// <summary>
        /// Gets or sets the name of the banned player.
        /// </summary>
        /// <value>The name of the banned player.</value>
        public string PlayerName { get; set; }
    }
}
