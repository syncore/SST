using System.Collections.Generic;

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
        // ReSharper disable once InconsistentNaming
        public string accountName { get; set; }

        /// <summary>
        ///     Gets or sets the elo cache interval, which is
        ///     the time in minutes after which the cached elo data will expire.
        /// </summary>
        /// <value>
        ///     The elo cache interval.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public uint eloCacheExpiration { get; set; }

        /// <summary>
        ///     Gets or sets the list of SBB owner(s).
        /// </summary>
        /// <value>
        ///     The list of SSB owner(s).
        /// </value>
        // ReSharper disable once InconsistentNaming
        public HashSet<string> owners { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            // TODO: prior to release, change this default bot name to something completely random
            accountName = "syncore";
            var o = new HashSet<string> {"syncore"};
            owners = o;
            eloCacheExpiration = 300; // 5 hours
        }
    }
}