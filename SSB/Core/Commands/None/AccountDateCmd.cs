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
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name is without clantag.",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <remarks>
        ///     c.Args[1] if specified: user to check
        /// </remarks>
        public async Task ExecAsync(CmdArgs c)
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
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^7 Unable to retrieve {0}'s account registration date.",
                            c.Args[1]));
            }
            else
            {
                var daysAgo = Math.Truncate((DateTime.Now - date).TotalDays * 100) / 100;
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[DATE]^7 {0}'s account was registered on:^5 {1}^7 (^3{2}^7 days old)",
                            c.Args[1], date.ToString("d"), daysAgo));
            }
        }
    }
}