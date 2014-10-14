using System.Collections.Generic;

namespace SSB.Model.QlRanks
{
    /// <summary>
    /// Model representing the outer container object returned from the QLRanks API
    /// </summary>
    public class QlRanks
    {
        /// <summary>
        /// Gets or sets the players.
        /// </summary>
        /// <value>The players.</value>
        public List<QlRanksPlayer> players
        {
            get;
            set;
        }
    }
}