using System.Collections.Generic;
using System.Linq;
using System.Text;
using SST.Interfaces;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    ///     Class for limting commands.
    /// </summary>
    public class ModuleManager
    {
        private readonly List<IModule> _moduleList;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModuleManager" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public ModuleManager(SynServerTool sst)
        {
            var s = sst;
            _moduleList = new List<IModule>();

            EloLimit = new EloLimit(s);
            _moduleList.Add(EloLimit);

            AccountDateLimit = new AccountDateLimit(s);
            _moduleList.Add(AccountDateLimit);

            Accuracy = new Accuracy(s);
            _moduleList.Add(Accuracy);

            AutoVoter = new AutoVoter(s);
            _moduleList.Add(AutoVoter);

            EarlyQuit = new EarlyQuit(s);
            _moduleList.Add(EarlyQuit);

            Motd = new Motd(s);
            _moduleList.Add(Motd);

            Pickup = new Pickup(s);
            _moduleList.Add(Pickup);

            Servers = new Servers(s);
            _moduleList.Add(Servers);

            Irc = new Irc(s);
            _moduleList.Add(Irc);
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
        /// Gets the active module count.
        /// </summary>
        /// <value>
        /// The active module count.
        /// </value>
        public int ActiveModuleCount
        {
            get { return ModuleList.Count(mod => mod.Active); }
        }

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
        ///     Gets the Internet Relay Chat (IRC) module.
        /// </summary>
        /// <value>
        ///     The Internet Relay Chat (IRC) module.
        /// </value>
        public Irc Irc { get; private set; }

        /// <summary>
        /// Gets the module list.
        /// </summary>
        /// <value>
        /// The module list.
        /// </value>
        public List<IModule> ModuleList
        {
            get { return _moduleList; }
        }

        /// <summary>
        ///     Gets the message of the day module.
        /// </summary>
        /// <value>
        ///     The message of the day module.
        /// </value>
        public Motd Motd { get; private set; }

        /// <summary>
        ///     Gets the pickup module.
        /// </summary>
        /// <value>
        ///     The pickup module.
        /// </value>
        public Pickup Pickup { get; private set; }

        /// <summary>
        ///     Gets the servers module.
        /// </summary>
        /// <value>
        ///     The servers module.
        /// </value>
        public Servers Servers { get; private set; }

        /// <summary>
        /// Gets the active modules.
        /// </summary>
        /// <returns>A list of the active modules as a string.</returns>
        public string GetActiveModules()
        {
            var active = ModuleList.Where(mod => mod.Active).ToList();
            if (active.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var mod in active)
            {
                sb.Append(string.Format("{0}, ", mod.ModuleName));
            }

            return sb.ToString().TrimEnd(',', ' ');
        }
    }
}