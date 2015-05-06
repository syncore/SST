using System.Collections.Generic;
using SST.Model;

namespace SST.Config.Modules
{
    /// <summary>
    ///     Model class that represents the auto voter module options for the configuration file.
    /// </summary>
    public class AutoVoterOptions
    {
        /// <summary>
        ///     Gets or sets the automatic votes.
        /// </summary>
        /// <value>
        ///     The automatic votes.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public List<AutoVote> autoVotes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool isActive { get; set; }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            autoVotes = new List<AutoVote>();
        }
    }
}