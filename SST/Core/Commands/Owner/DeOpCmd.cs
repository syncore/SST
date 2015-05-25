using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Owner
{
    /// <summary>
    /// Command: Remove a player's operator privileges.
    /// </summary>
    public class DeOpCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:DEOP]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeOpCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public DeOpCmd(SynServerTool sst)
        {
            _sst = sst;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        public async Task<bool> ExecAsync(Cmd c)
        {
            var id = _sst.ServerEventProcessor.GetPlayerId(Helpers.GetArgVal(c, 1));
            if (id != -1)
            {
                await _sst.QlCommands.SendToQlAsync(string.Format("deop {0}", id), false);
                StatusMessage = string.Format("^2[SUCCESS]^7 Attemped to de-op ^2{0}", Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "Attempted to de-op player {0} (id: {1}) using QL's op system.",
                    Helpers.GetArgVal(c, 1), id), _logClassType, _logPrefix);
                return true;
            }

            StatusMessage = "^1[ERROR]^3 Player not found. Use player name without clan tag.";
            await SendServerTell(c, StatusMessage);

            Log.Write(string.Format(
                    "Unable to send de-op for player {0} because player ID could not be retrieved.",
                    Helpers.GetArgVal(c, 1)), _logClassType, _logPrefix);
            return false;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name does NOT include clan tag.",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}",
                c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}
