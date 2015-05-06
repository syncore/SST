using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace SST.Model.QuakeLiveApi
{
    /// <summary>
    ///     Model class representing the outer container object returned by a http://www.quakelive.com/browser/list/filter= URL
    /// </summary>
    public class FilterObject
    {
        /// <summary>
        ///     Gets or sets the lfg_requests.
        /// </summary>
        /// <value>
        ///     The lfg_requests.
        /// </value>
        public int lfg_requests { get; set; }

        /// <summary>
        ///     Gets or sets the matches_played.
        /// </summary>
        /// <value>
        ///     The matches_played.
        /// </value>
        public int matches_played { get; set; }

        /// <summary>
        ///     Gets or sets the servers.
        /// </summary>
        /// <value>
        ///     The servers.
        /// </value>
        public List<Server> servers { get; set; }
    }
}