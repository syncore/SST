using SSB.Database;

namespace SSB.Core.Commands.Limits
{
    /// <summary>
    ///     Class for limting commands.
    /// </summary>
    public class Limiter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Limiter" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Limiter(SynServerBot ssb)
        {
            EloLimit = new EloLimit(ssb);
            AccountDateLimit = new AccountDateLimit(ssb);
        }

        /// <summary>
        ///     Gets the account date limiter.
        /// </summary>
        /// <value>
        ///     The account date limiter.
        /// </value>
        public AccountDateLimit AccountDateLimit { get; private set; }

        /// <summary>
        ///     Gets the elo limiter.
        /// </summary>
        /// <value>
        ///     The elo limiter.
        /// </value>
        public EloLimit EloLimit { get; private set; }
    }
}