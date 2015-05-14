// ReSharper disable InconsistentNaming
namespace SST.Config.Modules
{
    /// <summary>
    /// Model class that represents the elo limiter module options for the configuration file.
    /// </summary>
    public class EloLimitOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value><c>true</c> if this module is active; otherwise, <c>false</c>.</value>
        public bool isActive { get; set; }

        /// <summary>
        /// Gets or sets the maximum required Elo.
        /// </summary>
        /// <value>The maximum required Elo.</value>
        public int maximumRequiredElo { get; set; }

        /// <summary>
        /// Gets or sets the minimum required Elo.
        /// </summary>
        /// <value>The minimum required Elo.</value>
        public int minimumRequiredElo { get; set; }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            maximumRequiredElo = 0;
            minimumRequiredElo = 0;
        }
    }
}
