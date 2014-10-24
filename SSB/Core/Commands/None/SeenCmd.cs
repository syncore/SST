using System;
using System.Globalization;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Check date and time when player was last seen on the server.
    /// </summary>
    public class SeenCmd : IBotCommand
    {
        private readonly SeenDates _seenDb;
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SeenCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public SeenCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _seenDb = new SeenDates();
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
            PlayerInfo pinfo;
            if (_ssb.ServerInfo.CurrentPlayers.TryGetValue(c.Args[1], out pinfo))
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
                var daysAgo = Math.Truncate((DateTime.Now - date).TotalDays * 100) / 100;
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