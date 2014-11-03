namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Class for limting commands.
    /// </summary>
    public class Module
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Module" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Module(SynServerBot ssb)
        {
            EloLimit = new EloLimit(ssb);
            AccountDateLimit = new AccountDateLimit(ssb);
            AutoVoter = new AutoVoter(ssb);
        }

        /// <summary>
        ///     Gets the account date limiter.
        /// </summary>
        /// <value>
        ///     The account date limiter.
        /// </value>
        public AccountDateLimit AccountDateLimit { get; private set; }

        /// <summary>
        /// Gets the automatic voter.
        /// </summary>
        /// <value>
        /// The automatic voter.
        /// </value>
        public AutoVoter AutoVoter { get; private set; }

        /// <summary>
        ///     Gets the elo limiter.
        /// </summary>
        /// <value>
        ///     The elo limiter.
        /// </value>
        public EloLimit EloLimit { get; private set; }
    }
}