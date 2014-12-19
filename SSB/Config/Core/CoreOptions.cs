using System.Collections.Generic;

namespace SSB.Config.Core
{
    /// <summary>
    ///     Model class representing the core configuration options of SSB.
    /// </summary>
    public class CoreOptions
    {
        /// <summary>
        ///     Gets or sets the list of SBB owner(s).
        /// </summary>
        /// <value>
        ///     The list of SSB owner(s).
        /// </value>
        // ReSharper disable once InconsistentNaming
        public HashSet<string> owners { get; set; }
    }
}