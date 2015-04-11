using System;

namespace SST.Model
{
    /// <summary>
    /// Model class representing a pickup game.
    /// </summary>
    public class PickupInfo
    {
        /// <summary>
        /// Gets or sets the blue captain.
        /// </summary>
        /// <value>
        /// The blue captain.
        /// </value>
        public string BlueCaptain { get; set; }

        /// <summary>
        /// Gets or sets the blue team.
        /// </summary>
        /// <value>
        /// The blue team.
        /// </value>
        public string BlueTeam { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>
        /// The end date.
        /// </value>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the no shows.
        /// </summary>
        /// <value>
        /// The no shows.
        /// </value>
        public string NoShows { get; set; }

        /// <summary>
        /// Gets or sets the red captain.
        /// </summary>
        /// <value>
        /// The red captain.
        /// </value>
        public string RedCaptain { get; set; }

        /// <summary>
        /// Gets or sets the red team.
        /// </summary>
        /// <value>
        /// The red team.
        /// </value>
        public string RedTeam { get; set; }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>
        /// The start date.
        /// </value>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the subs.
        /// </summary>
        /// <value>
        /// The subs.
        /// </value>
        public string Subs { get; set; }
    }
}