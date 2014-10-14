using SSB.Model.Qlranks;

namespace SSB.Model.QlRanks
{
    /// <summary>
    /// Model representing an individual player within the players array that is returned from the
    /// QLRanks API
    /// </summary>
    public class QlRanksPlayer
    {
        /// <summary>
        /// Gets or sets the ca object.
        /// </summary>
        /// <value>The ca object.</value>
        public Ca ca
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the CTF object.
        /// </summary>
        /// <value>The CTF object.</value>
        public Ctf ctf
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the duel object.
        /// </summary>
        /// <value>The duel object.</value>
        public Duel duel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ffa object.
        /// </summary>
        /// <value>The ffa object.</value>
        public Ffa ffa
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the nick.
        /// </summary>
        /// <value>The nick.</value>
        public string nick
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the TDM object.
        /// </summary>
        /// <value>The TDM object.</value>
        public Tdm tdm
        {
            get;
            set;
        }
    }
}