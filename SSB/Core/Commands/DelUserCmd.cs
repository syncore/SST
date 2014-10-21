using System;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands
{
    /// <summary>
    ///     Command: Removes a user to the bot's internal user database.
    /// </summary>
    public class DelUserCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private readonly Users _users;

        private int _minArgs = 1;

        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DelUserCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public DelUserCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
        }

        /// <summary>
        ///     Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
        /// </value>
        public bool HasAsyncExecution { get; set; }

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
        public void DisplayArgLengthError(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} user",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Uses the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        /// <remarks>
        ///     c.Args[1]: User to delete.
        /// </remarks>
        public void Exec(CmdArgs c)
        {
            UserLevel todelUserLevel = _users.GetUserLevel(c.Args[1]);
            DbResult result = _users.DeleteUserFromDb(c.Args[1], c.FromUser, _users.GetUserLevel(c.FromUser));
            if (result == DbResult.Success)
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Removed user^2 {0} ^7from the^2 [{1}] ^7group.",
                        c.Args[1], todelUserLevel));
                //Refresh
                _users.RetrieveAllUsers();
            }
            else
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 Unable to remove user^1 {0}^7 from the^1 [{1}] ^7group. Code:^1 {2}",
                        c.Args[1], todelUserLevel, result));
            }
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task ExecAsync(CmdArgs c)
        {
            throw new NotImplementedException();
        }
    }
}