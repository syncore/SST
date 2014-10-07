using System.Collections.Generic;

namespace SSB
{
    /// <summary>
    ///     Model class representing the core configuration options of SSB.
    /// </summary>
    public class CoreConfig
    {
        /// <summary>
        ///     Gets or sets the list of initial SSB administrators.
        /// </summary>
        /// <value>
        ///     The list of initial SSB administrators.
        /// </value>
        public List<string> admins { get; set; }
    }
}