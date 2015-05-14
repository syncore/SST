using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    /// Command: Check an account's registration date.
    /// </summary>
    public class AccountDateCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:DATE]";
        private readonly int _qlMinArgs = 2;
        private readonly DbRegistrationDates _registrationDb;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountDateCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public AccountDateCmd(SynServerTool sst)
        {
            _sst = sst;
            _registrationDb = new DbRegistrationDates();
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
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        /// <remarks>Helpers.GetArgVal(c, 1) if specified: user to check</remarks>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            // DB
            var date = _registrationDb.GetRegistrationDate(Helpers.GetArgVal(c, 1));
            if (date == default(DateTime))
            {
                // Not in DB, so retrieve from QL website
                var qlDateChecker = new QlAccountDateChecker();
                date = await qlDateChecker.GetUserRegistrationDate(Helpers.GetArgVal(c, 1));
            }
            // Display
            if (date == default(DateTime))
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^7 Unable to retrieve {0}'s account registration date.",
                    Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format("Could not retrieve account registration date for player {0}",
                    Helpers.GetArgVal(c, 1)), _logClassType, _logPrefix);

                return false;
            }
            var daysAgo = Math.Truncate((DateTime.Now - date).TotalDays * 100) / 100;
            StatusMessage = string.Format(
                "^5[DATE]^7 {0}'s account was registered on:^5 {1}^7 (^3{2} ^7days old)",
                Helpers.GetArgVal(c, 1), date.ToString("d"), daysAgo);
            await SendServerSay(c, StatusMessage);
            return true;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name is without clantag.",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}
