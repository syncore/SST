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
    /// Command: Stop the current server.
    /// </summary>
    public class StopServerCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:STOPSERVER]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopServerCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public StopServerCmd(SynServerTool sst)
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
        public async Task<bool> ExecAsync(Cmd c)
        {
            int delay;
            if (int.TryParse(Helpers.GetArgVal(c, 1), out delay) && delay >= 0)
            {
                StatusMessage = string.Format(
                            "^1[ATTENTION] ^7This server will be shutting down in^1 ***{0}***^7 seconds. Thanks for playing!",
                            delay);
                await SendServerSay(c, StatusMessage);

                Log.Write(string.Format(
                    "Server will shut down in {0} seconds", delay), _logClassType, _logPrefix);

                // ReSharper disable once UnusedVariable (synchronous)
                var s = Task.Run(async delegate
                {
                    await Task.Delay(delay * 1000);
                    await _sst.QlCommands.SendToQlAsync("stopserver", false);
                });
                return true;
            }
            await DisplayArgLengthError(c);
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
                "^1[ERROR]^3 Usage: {0}{1} delay - delay must be a number in seconds and greater than 0",
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
