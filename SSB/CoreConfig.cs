using System.Collections.Generic;

namespace SSB
{
    /// <summary>
    ///     Model class representing the core configuration options of SSB.
    /// </summary>
    public class CoreConfig
    {
        /// <summary>
        ///     Gets or sets the list SBB owner(s).
        /// </summary>
        /// <value>
        ///     The list of SSB owner(s).
        /// </value>
        public HashSet<string> owners { get; set; }
    }
}