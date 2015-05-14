using SST.Config.Core;
using SST.Config.Modules;

namespace SST.Config
{
    /// <summary>
    /// Model class representing the SST configuration file.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Gets or sets the account date options.
        /// </summary>
        /// <value>The account date options.</value>
        public AccountDateOptions AccountDateOptions { get; set; }

        /// <summary>
        /// Gets or sets the accuracy options.
        /// </summary>
        /// <value>The accuracy options.</value>
        public AccuracyOptions AccuracyOptions { get; set; }

        /// <summary>
        /// Gets or sets the automatic voter options.
        /// </summary>
        /// <value>The automatic voter options.</value>
        public AutoVoterOptions AutoVoterOptions { get; set; }

        /// <summary>
        /// Gets or sets the core configuration options.
        /// </summary>
        /// <value>The core configuration options.</value>
        // ReSharper disable once InconsistentNaming
        public CoreOptions CoreOptions { get; set; }

        /// <summary>
        /// Gets or sets the early quit options.
        /// </summary>
        /// <value>The early quit options.</value>
        public EarlyQuitOptions EarlyQuitOptions { get; set; }

        /// <summary>
        /// Gets or sets the elo limit options.
        /// </summary>
        /// <value>The elo limit options.</value>
        public EloLimitOptions EloLimitOptions { get; set; }

        /// <summary>
        /// Gets or sets the irc options.
        /// </summary>
        /// <value>The irc options.</value>
        public IrcOptions IrcOptions { get; set; }

        /// <summary>
        /// Gets or sets the motd options.
        /// </summary>
        /// <value>The motd options.</value>
        public MotdOptions MotdOptions { get; set; }

        /// <summary>
        /// Gets or sets the pickup options.
        /// </summary>
        /// <value>The pickup options.</value>
        public PickupOptions PickupOptions { get; set; }

        /// <summary>
        /// Gets or sets the servers options.
        /// </summary>
        /// <value>The servers options.</value>
        public ServersOptions ServersOptions { get; set; }
    }
}
