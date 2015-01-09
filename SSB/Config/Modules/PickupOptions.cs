namespace SSB.Config.Modules
{
    /// <summary>
    ///     Model class that represents the pickup module options for the configuration file.
    /// </summary>
    public class PickupOptions
    {
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the excessive no show ban time.
        /// </summary>
        /// <value>
        /// The excessive no show ban time.
        /// </value>
        public double excessiveNoShowBanTime { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the excessive no show ban time scale.
        /// </summary>
        /// <value>
        /// The excessive no show ban time scale.
        /// </value>
        public string excessiveNoShowBanTimeScale { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the excessive sub use ban time.
        /// </summary>
        /// <value>
        /// The excessive sub use ban time.
        /// </value>
        public double excessiveSubUseBanTime { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the excessive sub use ban time scale.
        /// </summary>
        /// <value>
        /// The excessive sub use ban time scale.
        /// </value>
        public string excessiveSubUseBanTimeScale { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        public bool isActive { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the maximum no shows per player.
        /// </summary>
        /// <value>
        /// The maximum no shows per player.
        /// </value>
        public uint maxNoShowsPerPlayer { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the maximum subs per player.
        /// </summary>
        /// <value>
        /// The maximum subs per player.
        /// </value>
        public uint maxSubsPerPlayer { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Gets or sets the size of the team.
        /// </summary>
        /// <value>
        ///     The size of the team.
        /// </value>
        public uint teamSize { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            maxSubsPerPlayer = 2;
            maxNoShowsPerPlayer = 1;
            excessiveNoShowBanTime = 14;
            excessiveNoShowBanTimeScale = "days";
            excessiveSubUseBanTime = 7;
            excessiveSubUseBanTimeScale = "days";
            teamSize = 4;
        }
    }
}