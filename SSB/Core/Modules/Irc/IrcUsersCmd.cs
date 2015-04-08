using System.Text;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: display the user levels of the players currently on the server.
    /// </summary>
    public class IrcUsersCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly SynServerBot _ssb;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;
        private readonly DbUsers _usersDb;
        private int _ircMinArgs = 0;
        private bool _isAsync = false;
        private bool _requiresMonitoring = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcUsersCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcUsersCmd(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
            _irc = irc;
            _usersDb = new DbUsers();
        }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _ircMinArgs; } }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync { get { return _isAsync; } }

        /// <summary>
        /// Gets a value indicating whether this command requires
        /// the bot to be monitoring a server before it can be used.
        /// </summary>
        /// <value>
        /// <c>true</c> if this command requires the bot to be monitoring
        /// a server; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresMonitoring
        {
            get { return _requiresMonitoring; }
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
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed,
        /// otherwise returns <c>false</c>.
        /// </returns>
        public bool Exec(CmdArgs c)
        {
            if (_ssb.ServerInfo.CurrentPlayers.Count == 0)
            {
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    string.Format("\u0003My server has no players at this time."));
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var player in _ssb.ServerInfo.CurrentPlayers)
                {
                    sb.Append(string.Format("\u0003\u0002{0}\u0002 ({1}), ",
                        player.Key, _usersDb.GetUserLevel(player.Key)));
                }

                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    string.Format("\u0003My server's current players have the following access levels: {0}",
                        sb.ToString().TrimEnd(',', ' ')));
            }

            return true;
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed,
        /// otherwise returns <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Not implemented, as this is not an async command.
        /// </remarks>
        public Task<bool> ExecAsync(CmdArgs c)
        {
            return null;
        }
    }
}