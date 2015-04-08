using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Enums;
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
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:IRC]";
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
            char[] sep = { ' ' };
            var args = msg.Split(sep);
            var cmdName = args[0].Substring(1);

            if (!CheckCommand(fromUser, cmdName, msg)) return;

            // See if command requires active server monitoring
            var c = new CmdArgs(args, cmdName, fromUser, msg, true);
            if (_ircCmds.Commands[cmdName].RequiresMonitoring &&
                (((!_ssb.IsMonitoringServer && !_ssb.IsInitComplete))))
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 This command requires that a server be monitored; your server" +
                    " is not currently being monitored.");
                return;
            }
            // Check argument length
            if (args.Length < _ircCmds.Commands[cmdName].IrcMinArgs)
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
        ///     Determines whether the command can be executed without taking into account
        ///     the command's requirement for server monitoring nor the argument length.
        /// </summary>
        /// <param name="fromUser">The sender of the command..</param>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="fullMessageText">The full message text.</param>
        /// <returns>
        ///     <c>true</c> if the command can be executed, without taking into account
        ///     the command's requirement for server monitoring nor the
        /// required argument length of the command, otherwise <c>false</c>.
        /// </returns>
        private bool CheckCommand(string fromUser, string commandName, string fullMessageText)
        {
            if (!Helpers.KeyExists(commandName, _ircCmds.Commands))
            {
                return false;
            }
            if (!SufficientTimeElapsed(fromUser))
            {
                Log.Write(string.Format(
                    "Sufficient time has not elapsed since {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, IrcCommandList.IrcCommandPrefix, commandName), _logClassType, _logPrefix);

                return false;
            }
            _ircCommandUserTime[fromUser] = DateTime.Now;
            if (fullMessageText.Equals(IrcCommandList.IrcCommandPrefix))
            {
                return false;
            }
            if (!UserHasReqLevel(fromUser, _ircCmds.Commands[commandName].UserLevel))
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 You do not have permission to use that command.");
                return false;
            }

            return true;
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
            // 3.5 seconds between commands
            return _ircCommandUserTime[user].AddSeconds(3.5) < DateTime.Now;
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