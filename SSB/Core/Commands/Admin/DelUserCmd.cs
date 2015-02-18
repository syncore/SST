using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Removes a user from the bot's internal user database.
    /// </summary>
    public class DelUserCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private readonly DbUsers _users;
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 1;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DelUserCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public DelUserCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new DbUsers();
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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdTell(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} user - user is without clantag",
                CommandProcessor.BotCommandPrefix, c.CmdName), c.FromUser);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <remarks>
        ///     c.Args[1]: User to delete.
        /// </remarks>
        public async Task ExecAsync(CmdArgs c)
        {
            var todelUserLevel = _users.GetUserLevel(c.Args[1]);
            var result = _users.DeleteUserFromDb(c.Args[1], c.FromUser, _users.GetUserLevel(c.FromUser));
            if (result == UserDbResult.Success)
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Removed user^2 {0} ^7from the^2 [{1}] ^7group.",
                        c.Args[1], todelUserLevel));
            }
            else
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 Unable to remove user^1 {0}^7 from the^1 [{1}] ^7group. Code:^1 {2}",
                        c.Args[1], todelUserLevel, result));
            }
        }
    }
}