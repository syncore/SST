namespace SSB.Model.Qlranks
{
    /// <summary>
    /// Model representing the capture the flag rank and elo information returned from the QLRanks API
    /// </summary>
    public class Ctf
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