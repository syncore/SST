using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Indefinitely pause a match.
    /// </summary>
    public class PauseCmd : IBotCommand
    {
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 0;
        private readonly SynServerBot _ssb;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PauseCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PauseCmd(SynServerBot ssb)
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
        /// <remarks>
        ///     Not implemented because the cmd in this class requires no args.
        /// </remarks>
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (GetGameState().Result != QlGameStates.InProgress) return;
            await _ssb.QlCommands.SendToQlAsync("pause", false);
            await
                _ssb.QlCommands.QlCmdSay("^7Pausing game indefinitely... Use the unpause command to un-pause.");
        }

        /// <summary>
        ///     Gets the state of the game.
        /// </summary>
        /// <returns>The state of the game.</returns>
        private async Task<QlGameStates> GetGameState()
        {
            var serverId = _ssb.ServerInfo.CurrentServerId;
            if (string.IsNullOrEmpty(serverId))
            {
                Debug.WriteLine("PAUSE: Server id is empty. Now trying to request serverinfo...");
                await _ssb.QlCommands.QlCmdServerInfo();
            }
            var qlApiQuery = new QlRemoteInfoRetriever();
            var gs = await qlApiQuery.GetGameState(serverId);
            return gs;
        }
    }
}