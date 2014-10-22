﻿using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.SuperUser
{
    /// <summary>
    ///     Command: Lock the teams
    /// </summary>
    public class LockCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.SuperUser;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LockCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public LockCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            HasAsyncExecution = false;
        }

        /// <summary>
        ///     Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
        /// </value>
        public bool HasAsyncExecution { get; private set; }

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
        public void DisplayArgLengthError(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} team^7 - teams are: ^1red,^4 blue,^3 both",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        public void Exec(CmdArgs c)
        {
            switch (c.Args[1])
            {
                case "both":
                    _ssb.QlCommands.SendToQl("lock", false);
                    break;

                case "red":
                    _ssb.QlCommands.SendToQl("lock red", false);
                    break;

                case "blue":
                    _ssb.QlCommands.SendToQl("lock blue", false);
                    break;
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