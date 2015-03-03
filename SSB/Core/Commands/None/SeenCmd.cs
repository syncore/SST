using System;
using System.Globalization;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Check date and time when player was last seen on the server.
    /// </summary>
    public class SeenCmd : IBotCommand
    {
        private readonly DbSeenDates _seenDb;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 2;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SeenCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public SeenCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _seenDb = new DbSeenDates();
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
            if (Helpers.KeyExists(c.Args[1], _ssb.ServerInfo.CurrentPlayers))
            {
                StatusMessage = string.Format(
                            "^5[LAST SEEN]^7 {0} is here ^5right now!",
                            c.Args[1]);
                await SendServerSay(c, StatusMessage);
            }
            var date = _seenDb.GetLastSeenDate(c.Args[1]);
            if (date != default(DateTime))
            {
                var daysAgo = Math.Truncate((DateTime.Now - date).TotalDays * 100) / 100;
                StatusMessage = string.Format(
                            "^5[LAST SEEN]^7 {0} was last seen on my server at:^5 {1}^7 (^3{2}^7 days ago)",
                            c.Args[1], date.ToString("g", CultureInfo.CreateSpecificCulture("en-us")), daysAgo);
                await SendServerSay(c, StatusMessage);
            }
            else
            {
                StatusMessage = string.Format(
                            "^5[LAST SEEN]^7 {0} has never been seen on my server.",
                            c.Args[1]);
                await SendServerSay(c, StatusMessage);
            }
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