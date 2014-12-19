namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Class for limting commands.
    /// </summary>
    public class ModuleManager
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ModuleManager" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ModuleManager(SynServerBot ssb)
        {
            SynServerBot s = ssb;
            EloLimit = new EloLimit(s);
            AccountDateLimit = new AccountDateLimit(s);
            AutoVoter = new AutoVoter(s);
            Motd = new Motd(s);
        }

        /// <summary>
        ///     Gets the account date limiter.
        /// </summary>
        /// <value>
        ///     The account date limiter.
        /// </value>
        public AccountDateLimit AccountDateLimit { get; private set; }

        /// <summary>
        ///     Gets the automatic voter.
        /// </summary>
        /// <value>
        ///     The automatic voter.
        /// </value>
        public AutoVoter AutoVoter { get; private set; }

        /// <summary>
        ///     Gets the elo limiter.
        /// </summary>
        /// <value>
        ///     The elo limiter.
        /// </value>
        public EloLimit EloLimit { get; private set; }

        /// <summary>
        ///     Gets the motd.
        /// </summary>
        /// <value>
        ///     The motd.
        /// </value>
        public Motd Motd { get; private set; }
    }
}