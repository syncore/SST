using System.Text;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: display the modules that are currently active.
    /// </summary>
    public class IrcModsCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly SynServerBot _ssb;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;
        private bool _isAsync = false;
        private int _ircMinArgs = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcModsCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcModsCmd(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
            _irc = irc;
        }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync
        {
            get { return _isAsync; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs
        {
            get { return _ircMinArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public IrcUserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this command takes no arguments.
        /// </remarks>
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void Exec(CmdArgs c)
        {
            var activeMods = _ssb.Mod.GetActiveModules();
            if (activeMods.Count == 0)
            {
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    string.Format("\u0003My server has no active modules loaded at this time"));
                return;
            }

            var sb = new StringBuilder();
            foreach (var mod in activeMods)
            {
                sb.Append(string.Format("\u00033{0}\u0003, ", mod.ModuleName));
            }

            _irc.SendIrcMessage(_irc.IrcSettings.ircChannel, string.Format("\u0003My server has \u0002{0}\u0002 active {1} loaded: {2}",
                activeMods.Count, (activeMods.Count > 1 ? "modules" : "module"), sb.ToString().TrimEnd(',', ' ')));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this is not an async command.
        /// </remarks>
        public Task ExecAsync(CmdArgs c)
        {
            return null;
        }
    }
}