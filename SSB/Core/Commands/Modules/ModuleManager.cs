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
            Accuracy = new Accuracy(s);
            AutoVoter = new AutoVoter(s);
            EarlyQuit = new EarlyQuit(s);
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
        ///     Gets the accuracy scanner.
        /// </summary>
        /// <value>
        ///     The accuracy scanner.
        /// </value>
        public Accuracy Accuracy { get; private set; }

        /// <summary>
        ///     Gets the automatic voter.
        /// </summary>
        /// <value>
        ///     The automatic voter.
        /// </value>
        public AutoVoter AutoVoter { get; private set; }

        /// <summary>
        ///     Gets the early quit module.
        /// </summary>
        /// <value>
        ///     The early quit module.
        /// </value>
        public EarlyQuit EarlyQuit { get; private set; }

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