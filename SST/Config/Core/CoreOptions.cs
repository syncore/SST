// ReSharper disable InconsistentNaming

namespace SST.Config.Core
{
    /// <summary>
    ///     Model class representing the core configuration options of SST.
    /// </summary>
    public class CoreOptions
    {

        public const string defaultUnsetAccountName = "ChangeThisName";
        public const string defaultUnsetOwnerName = "ChangeThisName";

        /// <summary>
        ///     Gets or sets the name of the bot.
        /// </summary>
        /// <value>
        ///     The name of the bot.
        /// </value>
        public string accountName { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SST events should be appended
        ///     to the activity log in the UI.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SST events should be appended to the activity log
        ///     in the UI; otherwise, <c>false</c>.
        /// </value>
        public bool appendToActivityLog { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SST should attempt to automatically
        ///     monitor a server on program start, as opposed to requiring the user to manully
        ///     start server monitoring via the UI.
        /// </summary>
        /// <value>
        ///     <c>true</c> if automatic server monitoring should occur on start;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool autoMonitorServerOnStart { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SST should check for application
        ///     updates on program start.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SST should check for updates on start; otherwise,
        ///     <c>false</c>.
        /// </value>
        public bool checkForUpdatesOnStart { get; set; }

        /// <summary>
        ///     Gets or sets the elo cache interval, which is
        ///     the time in minutes after which the cached elo data will expire.
        /// </summary>
        /// <value>
        ///     The elo cache interval.
        /// </value>
        public uint eloCacheExpiration { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether all console text should be
        ///     hidden in Quake Live.
        /// </summary>
        /// <value>
        ///     <c>true</c> if console text should be hidden in Quake Live;
        ///     otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     This represents whether SST should set "con_noprint" to 1 or 0.
        /// </remarks>
        public bool hideAllQlConsoleText { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SST events should be logged
        ///     to the disk.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SST events should be logged to the disk;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool logSstEventsToDisk { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SST should display in
        ///     the system notification area when minimize or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SST should display in the system notification area
        ///     when minimized, otherwise, <c>false</c>.
        /// </value>
        public bool minimizeToTray { get; set; }

        /// <summary>
        ///     Gets or sets the SBB owner.
        /// </summary>
        /// <value>
        ///     The SST owner.
        /// </value>
        public string owner { get; set; }

        /// <summary>
        ///     Gets or sets the required time that must pass (in seconds) between user commands.
        /// </summary>
        /// <value>
        ///     The required time (in seconds) that must pass between user commands.
        /// </value>
        /// <remarks>
        ///     If sufficient time does not pass (i.e. time since user's last command is less than this value)
        ///     then the command will be ignored. This does not apply to Admins or higher.
        /// </remarks>
        public double requiredTimeBetweenCommands { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            accountName = defaultUnsetAccountName;
            appendToActivityLog = true;
            autoMonitorServerOnStart = false;
            checkForUpdatesOnStart = true;
            owner = defaultUnsetOwnerName;
            requiredTimeBetweenCommands = 6.5;
            eloCacheExpiration = 300; // 5 hours
            hideAllQlConsoleText = true;
            logSstEventsToDisk = false;
            minimizeToTray = true;
        }
    }
}