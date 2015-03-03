using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     Class responsible for processing IRC commands.
    /// </summary>
    public class IrcCommandProcessor
    {
        private readonly IrcManager _irc;
        private readonly IrcCommandList _ircCmds;
        private readonly Dictionary<string, DateTime> _ircCommandUserTime;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcCommandProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcCommandProcessor(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
            _irc = irc;
            _ircCmds = new IrcCommandList(_ssb, _irc);
            _ircCommandUserTime = new Dictionary<string, DateTime>();
        }

        /// <summary>
        ///     Gets the commands.
        /// </summary>
        /// <value>
        ///     The Commands.
        /// </value>
        /// <remarks>
        ///     This is used by the IrcHelpCmd.
        /// </remarks>
        public Dictionary<string, IIrcCommand> Cmds
        {
            get { return _ircCmds.Commands; }
        }

        /// <summary>
        ///     Processes the IRC command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessIrcCommand(string fromUser, string msg)
        {
            char[] sep = {' '};
            var args = msg.Split(sep);
            var cmdName = args[0].Substring(1);
            if (!_ssb.IsInitComplete)
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 Initilization has not completed yet. Command ignored.");

                return;
            }
            if (!SufficientTimeElapsed(fromUser))
            {
                Debug.WriteLine(
                    "Sufficient time has not elapsed since {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, IrcCommandList.IrcCommandPrefix, cmdName);
                return;
            }
            _ircCommandUserTime[fromUser] = DateTime.Now;
            if (msg.Equals(IrcCommandList.IrcCommandPrefix))
            {
                return;
            }
            if (!Helpers.KeyExists(cmdName, _ircCmds.Commands))
            {
                return;
            }
            if (!UserHasReqLevel(fromUser, _ircCmds.Commands[cmdName].UserLevel))
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 You do not have permission to use that command.");
                return;
            }
            var c = new CmdArgs(args, cmdName, fromUser, msg, true);
            if (args.Length < _ircCmds.Commands[cmdName].MinArgs)
            {
                _ircCmds.Commands[cmdName].DisplayArgLengthError(c);
                return;
            }
            // Execute
            if (_ircCmds.Commands[cmdName].IsAsync)
            {
                await _ircCmds.Commands[cmdName].ExecAsync(c);
            }
            else
            {
                _ircCmds.Commands[cmdName].Exec(c);
            }
        }

        /// <summary>
        ///     Checks whether sufficient time has elapsed since the user last issued an IRC command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if sufficient time has elapsed, otherwise <c>false</c>.</returns>
        private bool SufficientTimeElapsed(string user)
        {
            if (!Helpers.KeyExists(user, _ircCommandUserTime) ||
                _irc.GetIrcUserLevel(user) >= IrcUserLevel.Voice)
            {
                return true;
            }
            // 5 seconds between commands
            return _ircCommandUserTime[user].AddSeconds(5) < DateTime.Now;
        }

        /// <summary>
        ///     Checks whether the user has the required IRC access level to issue a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="requiredLevel">The required access level.</param>
        /// <returns><c>true</c> if the user has the required access level, otherwise <c>false</c>.</returns>
        private bool UserHasReqLevel(string user, IrcUserLevel requiredLevel)
        {
            return _irc.GetIrcUserLevel(user) >= requiredLevel;
        }
    }
}