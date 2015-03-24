// ReSharper disable InconsistentNaming

namespace SSB.Config.Modules
{
    /// <summary>
    ///     Model class that represents the early quitter module options for the configuration file.
    /// </summary>
    public class EarlyQuitOptions
    {
        /// <summary>
        ///     Gets or sets the ban time value.
        /// </summary>
        /// <value>
        ///     The ban time value.
        /// </value>
        public double banTime { get; set; }

        /// <summary>
        ///     Gets or sets the ban time scale.
        /// </summary>
        /// <value>
        ///     The ban time scale.
        /// </value>
        public string banTimeScale { get; set; }

        /// <summary>
        ///     Gets or sets the index of the ban time scale.
        /// </summary>
        /// <value>
        ///     The index of the ban time scale.
        /// </value>
        public int banTimeScaleIndex { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        public bool isActive { get; set; }

        /// <summary>
        ///     Gets or sets the maximum quits allowed before banning the user.
        /// </summary>
        /// <value>
        ///     The maximum quits allowed before the user is banned.
        /// </value>
        public uint maxQuitsAllowed { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            banTime = 2;
            banTimeScale = "days";
            banTimeScaleIndex = 3; // days
            maxQuitsAllowed = 2;
        }
    }
}