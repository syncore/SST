namespace SSB.Config.Modules
{
    public class AccuracyOptions
    {
        /// <summary>
        /// Gets or sets the interval between scans in seconds.
        /// </summary>
        /// <value>
        /// The interval between scans in seconds.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public uint intervalBetweenScans { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool isActive { get; set; }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            intervalBetweenScans = 0;
        }
    }
}