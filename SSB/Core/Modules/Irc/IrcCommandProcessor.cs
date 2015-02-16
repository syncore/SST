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
        public const string IrcCommandPrefix = "!";
        private readonly IrcHandler _irc;
        private readonly Dictionary<string, IIrcCommand> _ircCommands;
        private readonly Dictionary<string, DateTime> _ircCommandUserTime;
        private readonly SynServerBot _ssb;
        private readonly string IrcCmdSay = "say";
        private readonly string IrcCmdSayTeam = "sayteam";
        private string IrcCmdMods = "mods";
        private string IrcCmdStatus = "status";
        private string IrcCmdUsers = "users";
        private string IrcCmdWho = "who";

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcCommandProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcCommandProcessor(SynServerBot ssb, IrcHandler irc)
        {
            _ssb = ssb;
            _irc = irc;
            _ircCommands = new Dictionary<string, IIrcCommand>
            {
                {IrcCmdSay, new IrcSayCmd(_ssb, _irc)},
                {IrcCmdSayTeam, new IrcSayTeamCmd(_ssb, _irc)}
                //{IrcCmdSayTeam, new IrcSayTeamCmd(_ssb)},
                //{IrcCmdWho, new IrcWhoCmd(_ssb)},
                //{IrcCmdStatus, new IrcStatusCmd(_ssb)},
                //{IrcCmdMods, new IrcModsCmd(_ssb)},
                //{IrcCmdUsers, new IrcUsersCmd(_ssb)},
            };
            _ircCommandUserTime = new Dictionary<string, DateTime>();
        }

        /// <summary>
        ///     Processes the IRC command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessIrcCommand(string fromUser, string msg)
        {
            char[] sep = {' '};
            var args = msg.Split(sep, 6);
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
                    fromUser, IrcCommandPrefix, cmdName);
                return;
            }
            _ircCommandUserTime[fromUser] = DateTime.Now;
            if (msg.Equals(IrcCommandPrefix))
            {
                return;
            }
            if (!Tools.KeyExists(cmdName, _ircCommands))
            {
                return;
            }
            if (!UserHasReqLevel(fromUser, _ircCommands[cmdName].UserLevel))
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 You do not have permission to use that command.");
                return;
            }
            var c = new CmdArgs(args, cmdName, fromUser);
            if (args.Length < _ircCommands[cmdName].MinArgs)
            {
                _ircCommands[cmdName].DisplayArgLengthError(c);
                return;
            }
            // Execute
            if (_ircCommands[cmdName].IsAsync)
            {
                await _ircCommands[cmdName].ExecAsync(c);
            }
            else
            {
                _ircCommands[cmdName].Exec(c);
            }
        }

        /// <summary>
        ///     Checks whether sufficient time has elapsed since the user last issued an IRC command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if sufficient time has elapsed, otherwise <c>false</c>.</returns>
        private bool SufficientTimeElapsed(string user)
        {
            if (!Tools.KeyExists(user, _ircCommandUserTime) ||
                _irc.GetIrcUserLevel(user) >= IrcUserLevel.Voice)
            {
                return true;
            }
            // 5 seconds between commands
            if (_ircCommandUserTime[user].AddSeconds(5) < DateTime.Now)
            {
                return true;
            }
            return false;
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