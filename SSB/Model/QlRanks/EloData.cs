namespace SSB.Model.QlRanks
{
    /// <summary>
    /// Model representing all of the elo information for supported QLRanks gametypes. Used as the
    /// value of a dictionary storing the QLRanks elo information for each player returned by the QL API.
    /// </summary>
    public class EloData
    {
        /// <summary>
        /// Gets or sets the CA Elo.
        /// </summary>
        /// <value>The CA Elo.</value>
        public long CaElo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the CTF Elo.
        /// </summary>
        /// <value>The CTF Elo.</value>
        public long CtfElo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the duel Elo.
        /// </summary>
        /// <value>The duel Elo.</value>
        public long DuelElo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the FFA Elo.
        /// </summary>
        /// <value>The FFA Elo.</value>
        public long FfaElo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the TDM Elo.
        /// </summary>
        /// <value>The TDM Elo.</value>
        public long TdmElo
        {
            get;
            set;
        }
    }
}