using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Help command.
    /// </summary>
    public class HelpCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private bool _isIrcAccessAllowed = true;
        private int _qlMinArgs = 0;

        private UserLevel _userLevel = UserLevel.None;

        public HelpCmd(SynServerBot ssb)
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
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise
        /// <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            var userDb = new DbUsers();
            var senderLevel = userDb.GetUserLevel(c.FromUser);
            var senderLevelName = Enum.GetName(typeof(UserLevel), senderLevel);
            var cmds = new StringBuilder();
            foreach (var cmd in _ssb.CommandProcessor.Commands.Where(cmd => senderLevel <= cmd.Value.UserLevel))
            {
                cmds.Append(string.Format("{0}, ", cmd.Key));
            }
            StatusMessage = string.Format(
                    "^7Your user level - ^3{0}^7 - has access to these commands (put ^3{1}^7 in front): ^5{2}",
                    (senderLevelName ?? "NONE"), CommandList.GameCommandPrefix, cmds.ToString().TrimEnd(',', ' '));

            await SendServerSay(c, StatusMessage);

            StatusMessage = "^7For detailed help visit the website at ^3ssb.syncore.org^7, or ^3#ssb_ql^7 on QuakeNet.";
            await SendServerSay(c, StatusMessage);
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