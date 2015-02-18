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
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 2;
        private readonly DbSeenDates _seenDb;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;

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
            if (Helpers.KeyExists(c.Args[1], _ssb.ServerInfo.CurrentPlayers))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[LAST SEEN]^7 {0} is here ^5right now!",
                            c.Args[1]));
                return;
            }
            var date = _seenDb.GetLastSeenDate(c.Args[1]);
            if (date != default(DateTime))
            {
                var daysAgo = Math.Truncate((DateTime.Now - date).TotalDays*100)/100;
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[LAST SEEN]^7 {0} was last seen on my server at:^5 {1}^7 (^3{2}^7 days ago)",
                            c.Args[1], date.ToString("g", CultureInfo.CreateSpecificCulture("en-us")), daysAgo));
            }
            else
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[LAST SEEN]^7 {0} has never been seen on my server.",
                            c.Args[1]));
            }
        }
    }
}