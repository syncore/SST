using System;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.SuperUser
{
    /// <summary>
    ///     Command: Refresh server information.
    /// </summary>
    public class ReloadCmd : IBotCommand
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:RELOAD]";
        private readonly SynServerBot _ssb;
        private bool _isIrcAccessAllowed = true;
        private int _qlMinArgs = 0;
        private UserLevel _userLevel = UserLevel.SuperUser;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReloadCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ReloadCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        /// <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay("^3[ATTENTION] ^2Reloading...please wait");
            StatusMessage = "^2[SUCCESS]^7 Attempting to reload server information";
            await SendServerTell(c, StatusMessage);
            Log.Write("Will attempt to reload server information.", _logClassType, _logPrefix);
            _ssb.ReloadInit();
            return true;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Empty;
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}