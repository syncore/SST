﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: display general help information to irc users.
    /// </summary>
    public class IrcHelpCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;
        private readonly Dictionary<string, IIrcCommand> _cmdList;
        private int _ircMinArgs = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcHelpCmd" /> class.
        /// </summary>
        /// <param name="irc">The IRC interface.</param>
        /// <param name="cmdList">The command list.</param>
        public IrcHelpCmd(IrcManager irc, Dictionary<string, IIrcCommand> cmdList)
        {
            IsAsync = false;
            _irc = irc;
            _cmdList = cmdList;
        }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync { get; private set; }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _ircMinArgs; } }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public IrcUserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this command takes no arguments.
        /// </remarks>
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void Exec(CmdArgs c)
        {
            var cmds = _cmdList.Select(cmd =>
                string.Format("{0}{1}", IrcCommandList.IrcCommandPrefix, cmd.Key)).ToList();

            //TODO: update the URL
            _irc.SendIrcNotice(c.FromUser,
                string.Format(
                    "\u0003[COMMANDS]: \u0002{0}\u0002 - for more information, visit: http://ssb.syncore.org/help",
                    string.Join(", ", cmds)));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this is not an async command.
        /// </remarks>
        public Task ExecAsync(CmdArgs c)
        {
            return null;
        }
    }
}