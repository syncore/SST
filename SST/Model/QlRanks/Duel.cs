namespace SST.Model.Qlranks
{
    /// <summary>
    /// Model representing the duel rank and elo information returned from the QLRanks API
    /// </summary>
    public class Duel
    {
        /// <summary>
        /// Gets or sets the elo.
        /// </summary>
        /// <value>The elo.</value>
        public int elo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the rank.
        /// </summary>
        /// <value>The rank.</value>
        public int rank
        {
            get;
            set;
        }
    }
}