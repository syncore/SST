﻿using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Help command
    /// </summary>
    public class HelpCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;

        private int _minArgs = 0;

        private UserLevel _userLevel = UserLevel.None;

        public HelpCmd(SynServerBot ssb)
        {
            _ssb = ssb;
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
        public Task DisplayArgLengthError(CmdArgs c)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task ExecAsync(CmdArgs c)
        {
            //TODO: implement
            await _ssb.QlCommands.QlCmdSay("^7The ^2!help ^7command will go here.");
        }
    }
}