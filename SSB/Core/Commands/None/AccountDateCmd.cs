using System;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Check an account's registration date.
    /// </summary>
    public class AccountDateCmd : IBotCommand
    {
        private readonly int _minArgs = 2;
        private readonly DbRegistrationDates _registrationDb;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;
        private bool _isIrcAccessAllowed = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountDateCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccountDateCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _registrationDb = new DbRegistrationDates();
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
        /// <remarks>
        /// c.Args[1] if specified: user to check
        /// </remarks>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            var date = _registrationDb.GetRegistrationDate(c.Args[1]);
            // Retrieve
            if (date != default(DateTime))
            {
                date = _registrationDb.GetRegistrationDate(c.Args[1]);
            }
            else
            {
                var qlDateChecker = new QlAccountDateChecker();
                date = await qlDateChecker.GetUserRegistrationDate(c.Args[1]);
            }
            // Display
            if (date == default(DateTime))
            {
                StatusMessage = string.Format(
                            "^1[ERROR]^7 Unable to retrieve {0}'s account registration date.",
                            c.Args[1]);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var daysAgo = Math.Truncate((DateTime.Now - date).TotalDays * 100) / 100;
            StatusMessage = string.Format(
                        "^5[DATE]^7 {0}'s account was registered on:^5 {1}^7 (^3{2}^7 days old)",
                        c.Args[1], date.ToString("d"), daysAgo);
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
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name is without clantag.",
                CommandList.GameCommandPrefix, c.CmdName);
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
    }
}