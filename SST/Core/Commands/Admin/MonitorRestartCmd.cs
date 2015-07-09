namespace SST.Core.Commands.Admin
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using SST.Enums;
    using SST.Interfaces;
    using SST.Model;
    using SST.Util;

    /// <summary>
    /// Command: restart SST server monitoring from in-game.
    /// </summary>
    /// <remarks>
    /// This command has an access level of Admin as opposed to Owner (for MonitorStopCmd) since
    /// it's more likely that server admins will be available and there might be an urgent need to
    /// restart the tool.
    /// </remarks>
    public class MonitorRestartCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = false;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:RESTARTMONITOR]";
        private readonly int _qlMinArgs = 1;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorRestartCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public MonitorRestartCmd(SynServerTool sst)
        {
            _sst = sst;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs
        {
            get { return _qlMinArgs + 1; }
        }

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
            if (_sst.IsMonitoringServer)
            {
                await _sst.QlCommands.QlCmdSay("^3[ATTENTION] ^7Attempting to reload SST...wait^2 20-25^7 seconds if successful.", false);
                StatusMessage = "^2[SUCCESS]^7 Attempting to restart SST server monitoring.";
                await SendServerTell(c, StatusMessage);
                Log.Write(string.Format("Admin or higher {0} issued command to restart SST server monitoring. Restarting.",
                    c.FromUser), _logClassType, _logPrefix);
                _sst.RestartSst();
                return true;
            }

            // Should not happen
            StatusMessage = "^1[ERROR]^3 Unable to restart SST server monitoring.";
            await SendServerTell(c, StatusMessage);

            Log.Write(string.Format(
                "Admin or higher {0} issued command to restart SST server monitoring. Unable to restart.",
                c.FromUser), _logClassType, _logPrefix);

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
            return string.Empty;
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
            {
                await _sst.QlCommands.QlCmdSay(message, false);
            }
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
            {
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
            }
        }
    }
}
