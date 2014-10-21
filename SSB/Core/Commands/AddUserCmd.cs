﻿using System;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands
{
    /// <summary>
    ///     Command: Add a user to the bot's internal user database.
    /// </summary>
    public class AddUserCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private readonly Users _users;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AddUserCmd(SynServerBot ssb)
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
                "^1[ERROR]^3 Usage: {0}{1} name accesslevel#^7 - access levels #s are: 1(user), 2(superuser), 3(admin)",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Uses the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        /// <remarks>
        ///     c.Args[1]: userToAdd, c.Args[2]: accessLevel
        /// </remarks>
        public void Exec(CmdArgs c)
        {
            // TODO: define this in terms of the enum instead of hardcoding values
            if (!c.Args[2].Equals("1") && !c.Args[2].Equals("2") && !c.Args[2].Equals("3"))
            {
                DisplayArgLengthError(c);
                return;
            }
            // TODO: define this in terms of the enum instead of hardcoding values
            if ((c.Args[2].Equals("3")) && ((_users.GetUserLevel(c.FromUser) != UserLevel.Owner)))
            {
                _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^7 Only owners can add admins."));
                return;
            }
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            DbResult result = _users.AddUserToDb(c.Args[1], (UserLevel) Convert.ToInt32(c.Args[2]), c.FromUser,
                date);
            if (result == DbResult.Success)
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Added user^2 {0} ^7to the^2 [{1}] ^7group.", c.Args[1],
                        (UserLevel) Convert.ToInt32(c.Args[2])));
            }
            else
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 Unable to add user^1 {0}^7 to the^1 [{1}] ^7group. Code:^1 {2}",
                        c.Args[1], (UserLevel) Convert.ToInt32(c.Args[2]), result));
            }
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task ExecAsync(CmdArgs c)
        {
            throw new NotImplementedException();
        }
    }
}