using System.Collections.Generic;
using SSB.Model.QlRanks;

namespace SSB.Util
{
    /// <summary>
    /// Simple elo caching class.
    /// </summary>
    internal static class EloCache
    {
        /// <summary>
        /// Initializes the <see cref="EloCache"/> class.
        /// </summary>
        static EloCache()
        {
            CachedEloData = new Dictionary<string, EloData>();
        }

        /// <summary>
        /// Gets the cached elo data.
        /// </summary>
        /// <value>
        /// The cached elo data.
        /// </value>
        public static Dictionary<string, EloData> CachedEloData { get; private set; }
    }
}