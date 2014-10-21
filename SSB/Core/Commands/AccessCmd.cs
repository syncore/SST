using System;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands
{
    /// <summary>
    ///     Command: Check user account access level.
    /// </summary>
    public class AccessCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private readonly Users _users;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccessCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        /// <param name="users">The users database.</param>
        public AccessCmd(SynServerBot ssb, Users users)
        {
            _ssb = ssb;
            _users = users;
        }

        /// <summary>
        /// Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        /// <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
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
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        ///     Uses the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        /// <remarks>
        ///     c.Args[1] if specified: user to check
        /// </remarks>
        public void Exec(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(c.Args.Length > 1
                ? string.Format("^5{0}'s^7 user level is: ^5[{1}]", c.Args[1],
                    _users.GetUserLevel(c.Args[1]))
                : string.Format("^5{0}'s^7 user level is: ^5[{1}]", c.FromUser,
                    _users.GetUserLevel(c.FromUser)));
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task ExecAsync(CmdArgs c)
        {
            throw new NotImplementedException();
        }
    }
}