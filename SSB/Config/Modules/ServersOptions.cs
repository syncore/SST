namespace SSB.Config.Modules
{
    /// <summary>
    ///     Model class that represents the active servers module options for the configuration file.
    /// </summary>
    public class ServersOptions
    {
        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool isActive { get; set; }

        /// <summary>
        ///     Gets or sets the maximum active servers to display.
        /// </summary>
        /// <value>
        ///     The maximum active servers to display.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public uint maxServers { get; set; }

        /// <summary>
        ///     Gets or sets the time between queries for the purpose of rate-limiting.
        /// </summary>
        /// <value>
        ///     The time between queries for the purpose of rate-limiting.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public double timeBetweenQueries { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            maxServers = 5;
            timeBetweenQueries = 45;
        }
    }
}