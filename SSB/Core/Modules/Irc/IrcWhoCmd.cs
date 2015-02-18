using System.Text;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: display the current server's players.
    /// </summary>
    public class IrcWhoCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly SynServerBot _ssb;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcWhoCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcWhoCmd(SynServerBot ssb, IrcManager irc)
        {
            MinArgs = 0;
            IsAsync = false;
            _ssb = ssb;
            _irc = irc;
        }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync { get; private set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs { get; private set; }

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
            var sb = new StringBuilder();

            foreach (var player in _ssb.ServerInfo.CurrentPlayers)
            {
                if (player.Value.Team == Team.Blue)
                {
                    sb.Append(string.Format("\u000311{0}\u00030,", player.Key));
                }
                if (player.Value.Team == Team.Red)
                {
                    sb.Append(string.Format("\u000304{0}\u00030,", player.Key));
                }
                if (player.Value.Team == Team.Spec)
                {
                    sb.Append(string.Format("\u000314{0}\u00030,", player.Key));
                }
                if (player.Value.Team == Team.None)
                {
                    sb.Append(string.Format("\u00030{0}\u00030,", player.Key));
                }
            }

            _irc.SendIrcMessage(_irc.IrcSettings.ircChannel, string.Format("\u0003[\u0002{0}\u0002 players] {1}",
                _ssb.ServerInfo.CurrentPlayers.Count, sb.ToString().TrimEnd(',', ' ')));
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