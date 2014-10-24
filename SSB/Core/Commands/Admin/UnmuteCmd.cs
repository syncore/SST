using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Unmute a player.
    /// </summary>
    public class UnmuteCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnmuteCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public UnmuteCmd(SynServerBot ssb)
        {
            _ssb = ssb;
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
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name is without clantag.",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            string id = _ssb.ServerEventProcessor.GetPlayerId(c.Args[1]).Result;
            if (!String.IsNullOrEmpty(id))
            {
                await _ssb.QlCommands.SendToQlAsync(string.Format("unmute {0}", id), false);
                Debug.WriteLine("UNMUTE: Got player id {0} for player: {1}", id, c.Args[1]);
            }
            else
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Player not found. Use player name without clan tag.");
                Debug.WriteLine(string.Format(
                    "Unable to unmute player {0} because ID could not be retrieved.",
                    c.Args[1]));
            }
        }
    }
}