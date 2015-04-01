using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for processing bot commands.
    /// </summary>
    public class CommandProcessor
    {
        private readonly CommandList _cmdList;
        private readonly Dictionary<string, DateTime> _playerCommandTime;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public CommandProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new DbUsers();
            _cmdList = new CommandList(_ssb);
            _playerCommandTime = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>
        /// The commands.
        /// </value>
        public Dictionary<string, IBotCommand> Commands
        {
            get { return _cmdList.Commands; }
        }

        /// <summary>
        ///     Processes the bot command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessBotCommand(string fromUser, string msg)
        {
            char[] sep = { ' ' };
            string[] args = msg.Split(sep);
            string cmdName = args[0].Substring(1);
            if (!_ssb.IsInitComplete)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Initilization has not completed yet. Command ignored.");
                return;
            }
            if (!SufficientTimeElapsed(fromUser))
            {
                Debug.WriteLine(
                    "Sufficient time has not elapsed since {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, CommandList.GameCommandPrefix, cmdName);
                return;
            }
            _playerCommandTime[fromUser] = DateTime.Now;
            if (msg.Equals(CommandList.GameCommandPrefix))
            {
                return;
            }
            if (!Helpers.KeyExists(cmdName, _cmdList.Commands))
            {
                return;
            }
            if (!Helpers.KeyExists(fromUser, _ssb.ServerInfo.CurrentPlayers))
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 {0},^3 please give the bot time to sync your user info and then retry your {1} request in^1 {2} ^3secs.",
                        fromUser, cmdName, _ssb.InitDelay));
                return;
            }
            if (!UserHasReqLevel(fromUser, _cmdList.Commands[cmdName].UserLevel))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You do not have permission to use that command.");
                return;
            }
            var c = new CmdArgs(args, cmdName, fromUser, msg, false);
            if (args.Length < _cmdList.Commands[cmdName].QlMinArgs)
            {
                await _cmdList.Commands[cmdName].DisplayArgLengthError(c);
                return;
            }
            // Execute
            await _cmdList.Commands[cmdName].ExecAsync(c);
        }

        /// <summary>
        ///     Checks whether sufficient time has elapsed since the user last issued a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if sufficient time has elapsed, otherwise <c>false</c>.</returns>
        private bool SufficientTimeElapsed(string user)
        {
            if (!Helpers.KeyExists(user, _playerCommandTime) || _users.GetUserLevel(user) >= UserLevel.Admin)
            {
                return true;
            }
            // 6.5 seconds between commands
            return _playerCommandTime[user].AddSeconds(6.5) < DateTime.Now;
        }

        /// <summary>
        ///     Checks whether the user has the required access level to issue a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="requiredLevel">The required access level.</param>
        /// <returns><c>true</c> if the user has the required access level, otherwise <c>false</c>.</returns>
        private bool UserHasReqLevel(string user, UserLevel requiredLevel)
        {
            return _users.GetUserLevel(user) >= requiredLevel;
        }
    }
}