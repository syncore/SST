namespace SST.Config.Modules
{
    /// <summary>
    /// Model class that represents the accuracy module options for the configuration file.
    /// </summary>
    public class AccuracyOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value><c>true</c> if this module is active; otherwise, <c>false</c>.</value>
        // ReSharper disable once InconsistentNaming
        public bool isActive { get; set; }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
        }
    }
}
