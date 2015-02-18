using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Mute a player.
    /// </summary>
    public class MuteCmd : IBotCommand
    {
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 2;
        private readonly SynServerBot _ssb;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MuteCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public MuteCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdTell(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name is without clantag.",
                CommandProcessor.BotCommandPrefix, c.CmdName), c.FromUser);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            var id = _ssb.ServerEventProcessor.GetPlayerId(c.Args[1]);
            if (id != -1)
            {
                await _ssb.QlCommands.SendToQlAsync(string.Format("mute {0}", id), false);
                Debug.WriteLine("MUTE: Got player id {0} for player: {1}", id, c.Args[1]);
            }
            else
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 MUTE: Player not found. Use player name without clan tag.",
                        c.FromUser);
                Debug.WriteLine(string.Format("Unable to mute player {0} because ID could not be retrieved.",
                    c.Args[1]));
            }
        }
    }
}