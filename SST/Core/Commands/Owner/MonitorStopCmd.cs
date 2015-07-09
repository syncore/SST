namespace SST.Core.Commands.Owner
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using SST.Enums;
    using SST.Interfaces;
    using SST.Model;
    using SST.Util;

    /// <summary>
    /// Command: stop SST server monitoring from in-game.
    /// </summary>
    public class MonitorStopCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = false;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:STOPMONITOR]";
        private readonly int _qlMinArgs = 1;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorStopCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public MonitorStopCmd(SynServerTool sst)
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
                StatusMessage = "^2[SUCCESS]^7 Stopping SST server monitoring. To restart, use the GUI or IRC.";
                await SendServerTell(c, StatusMessage);
                Log.Write(string.Format("Owner {0} issued command to stop SST server monitoring. Stopping.",
                    c.FromUser), _logClassType, _logPrefix);

                _sst.StopMonitoring();
                return true;
            }

            // Should not happen
            StatusMessage = "^1[ERROR]^3 Unable to stop SST server monitoring.";
            await SendServerTell(c, StatusMessage);

            Log.Write(string.Format(
                "Owner {0} issued command to stop SST server monitoring. Unable to stop.",
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
