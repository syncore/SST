using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.SuperUser
{
    /// <summary>
    /// Command: Lock the teams.
    /// </summary>
    public class LockCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:LOCK]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.SuperUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public LockCmd(SynServerTool sst)
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
            if (!Helpers.GetArgVal(c, 1).Equals("both") &&
                !Helpers.GetArgVal(c, 1).Equals("red") &&
                !Helpers.GetArgVal(c, 1).Equals("blue"))
            {
                await DisplayArgLengthError(c);
                return false;
            }

            switch (Helpers.GetArgVal(c, 1))
            {
                case "both":
                    await _sst.QlCommands.SendToQlAsync("lock", false);
                    StatusMessage = "^2[SUCCESS]^7 Attempted to lock ^3BOTH^7 teams.";
                    break;

                case "red":
                    await _sst.QlCommands.SendToQlAsync("lock red", false);
                    StatusMessage = "^2[SUCCESS]^7 Attempted to lock ^1RED^7 team.";
                    break;

                case "blue":
                    await _sst.QlCommands.SendToQlAsync("lock blue", false);
                    StatusMessage = "^2[SUCCESS]^7 Attempted to lock ^5BLUE^7 team.";
                    break;
            }
            await SendServerTell(c, StatusMessage);

            Log.Write(string.Format("Attempted to lock {0} team.",
                Helpers.GetArgVal(c, 1).ToUpper()), _logClassType, _logPrefix);

            return true;
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
                "^1[ERROR]^3 Usage: {0}{1} team^7 - teams are: ^1red,^4 blue,^3 both",
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
