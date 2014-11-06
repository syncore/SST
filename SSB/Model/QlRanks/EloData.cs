using SSB.Enum;

namespace SSB.Model.QlRanks
{
    /// <summary>
    ///     Model representing all of the elo information for supported QLRanks gametypes. Used as the
    ///     value of a dictionary storing the QLRanks elo information for each player returned by the QL API.
    ///     long type used because that's how it is received by the JSON.NET library.
    /// </summary>
    public class EloData
    {
        /// <summary>
        ///     Gets or sets the CA Elo.
        /// </summary>
        /// <value>The CA Elo.</value>
        public long CaElo { get; set; }

        /// <summary>
        ///     Gets or sets the CTF Elo.
        /// </summary>
        /// <value>The CTF Elo.</value>
        public long CtfElo { get; set; }

        /// <summary>
        ///     Gets or sets the duel Elo.
        /// </summary>
        /// <value>The duel Elo.</value>
        public long DuelElo { get; set; }

        /// <summary>
        ///     Gets or sets the FFA Elo.
        /// </summary>
        /// <value>The FFA Elo.</value>
        public long FfaElo { get; set; }

        /// <summary>
        ///     Gets or sets the TDM Elo.
        /// </summary>
        /// <value>The TDM Elo.</value>
        public long TdmElo { get; set; }

        /// <summary>
        ///     Gets the type of the Elo from game type.
        /// </summary>
        /// <param name="gametype">The gametype.</param>
        /// <returns>The type Elo value associated with a particular <see cref="QlGameTypes" /> enum.</returns>
        public long GetEloFromGameType(QlGameTypes gametype)
        {
            long elo = 0;
            switch (gametype)
            {
                case QlGameTypes.Ca:
                    elo = CaElo;
                    break;

                case QlGameTypes.Ctf:
                    elo = CtfElo;
                    break;

                case QlGameTypes.Duel:
                    elo = DuelElo;
                    break;

                case QlGameTypes.Ffa:
                    elo = FfaElo;
                    break;

                case QlGameTypes.Tdm:
                    elo = TdmElo;
                    break;
            }
            return elo;
        }
    }
}