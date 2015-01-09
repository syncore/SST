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
            Pickup = new Pickup(s);
            Servers = new Servers(s);
        }

        /// <summary>
        ///     Gets the account date limiter module.
        /// </summary>
        /// <value>
        ///     The account date limiter module.
        /// </value>
        public AccountDateLimit AccountDateLimit { get; private set; }

        /// <summary>
        ///     Gets the accuracy scanner module.
        /// </summary>
        /// <value>
        ///     The accuracy scanner module.
        /// </value>
        public Accuracy Accuracy { get; private set; }

        /// <summary>
        ///     Gets the automatic voter module.
        /// </summary>
        /// <value>
        ///     The automatic voter module.
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
        ///     Gets the elo limiter modules.
        /// </summary>
        /// <value>
        ///     The elo limiter module.
        /// </value>
        public EloLimit EloLimit { get; private set; }

        /// <summary>
        ///     Gets the message of the day module.
        /// </summary>
        /// <value>
        ///     The message of the day module.
        /// </value>
        public Motd Motd { get; private set; }

        /// <summary>
        /// Gets the pickup module.
        /// </summary>
        /// <value>
        /// The pickup module.
        /// </value>
        public Pickup Pickup { get; private set; }
        
        /// <summary>
        /// Gets the servers module.
        /// </summary>
        /// <value>
        /// The servers module.
        /// </value>
        public Servers Servers { get; private set; }
    }
}