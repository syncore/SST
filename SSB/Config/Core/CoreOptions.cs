// ReSharper disable InconsistentNaming

namespace SSB.Config.Core
{
    /// <summary>
    ///     Model class representing the core configuration options of SSB.
    /// </summary>
    public class CoreOptions
    {
        /// <summary>
        ///     Gets or sets the name of the bot.
        /// </summary>
        /// <value>
        ///     The name of the bot.
        /// </value>
        public string accountName { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SSB events should be appended
        ///     to the activity log in the UI.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SSB events should be appended to the activity log
        ///     in the UI; otherwise, <c>false</c>.
        /// </value>
        public bool appendToActivityLog { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SSB should attempt to automatically
        ///     monitor a server on program start, as opposed to requiring the user to manully
        ///     start server monitoring via the UI.
        /// </summary>
        /// <value>
        ///     <c>true</c> if automatic server monitoring should occur on start;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool autoMonitorServerOnStart { get; set; }

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
        ///     This represents whether SSB should set "con_noprint" to 1 or 0.
        /// </remarks>
        public bool hideAllQlConsoleText { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SSB events should be logged
        ///     to the disk.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SSB events should be logged to the disk;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool logSsbEventsToDisk { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SSB should display in
        ///     the system notification area when minimize or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SSB should display in the system notification area
        ///     when minimized, otherwise, <c>false</c>.
        /// </value>
        public bool minimizeToTray { get; set; }

        /// <summary>
        ///     Gets or sets the SBB owner.
        /// </summary>
        /// <value>
        ///     The SSB owner.
        /// </value>
        public string owner { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            // TODO: prior to release, change these default bot & account names to something completely random
            accountName = "syncore";
            appendToActivityLog = true;
            autoMonitorServerOnStart = false;
            owner = "syncore";
            eloCacheExpiration = 300; // 5 hours
            hideAllQlConsoleText = true;
            logSsbEventsToDisk = false;
            minimizeToTray = true;
        }
    }
}