// ReSharper disable InconsistentNaming
namespace SST.Config.Modules
{
    /// <summary>
    /// Model class that represents the account date module options for the configuration file.
    /// </summary>
    public class AccountDateOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value><c>true</c> if this module is active; otherwise, <c>false</c>.</value>

        public bool isActive { get; set; }

        /// <summary>
        /// Gets or sets the minimum days that an account must be registered.
        /// </summary>
        /// <value>The minimum days that an account must be registered.</value>
        public uint minimumDaysRequired { get; set; }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            minimumDaysRequired = 1;
        }
    }
}
