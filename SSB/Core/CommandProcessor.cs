using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Config;
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
        private readonly ConfigHandler _cfgHandler;
        private readonly CommandList _cmdList;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD]";
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
            _cfgHandler = new ConfigHandler();
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

                Log.Write(
                    string.Format("Initilization not yet completed; ignoring command from player {0}.",
                    fromUser), _logClassType, _logPrefix);

                return;
            }
            if (!SufficientTimeElapsed(fromUser))
            {
                Log.Write(string.Format(
                    "Sufficient time has not elapsed since player {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, CommandList.GameCommandPrefix, cmdName), _logClassType, _logPrefix);
                return;
            }
            _playerCommandTime[fromUser] = DateTime.Now;
            if (msg.Equals(CommandList.GameCommandPrefix))
            {
                Log.Write(
                    string.Format("Player {0} entered command prefix {1} without specifying command. Ignoring.",
                    fromUser, CommandList.GameCommandPrefix), _logClassType, _logPrefix);

                return;
            }
            if (!Helpers.KeyExists(cmdName, _cmdList.Commands))
            {
                Log.Write(
                    string.Format("Player {0} entered command ({1}) that does not exist. Ignoring.",
                    fromUser, cmdName), _logClassType, _logPrefix);

                return;
            }
            if (!Helpers.KeyExists(fromUser, _ssb.ServerInfo.CurrentPlayers))
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 {0},^3 please give the bot time to sync your user info and then" +
                        " retry your {1} request in^1 {2} ^3secs.",
                        fromUser, cmdName, _ssb.InitDelay));

                Log.Write(
                    string.Format("Player {0} does not exist in current player list. Ignoring command.",
                    fromUser), _logClassType, _logPrefix);

                return;
            }
            if (!UserHasReqLevel(fromUser, _cmdList.Commands[cmdName].UserLevel))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You do not have permission to use that command.");
                Log.Write(
                    string.Format("Player {0} sent {1}{2} command but has permission less than {3} needed for {2}. Ingoring.",
                    fromUser, CommandList.GameCommandPrefix, cmdName, Enum.GetName(typeof(UserLevel),
                    _cmdList.Commands[cmdName].UserLevel)), _logClassType, _logPrefix);

                return;
            }

            var c = new CmdArgs(args, cmdName, fromUser, msg, false);
            var minArgs = _cmdList.Commands[cmdName].QlMinArgs;

            if (args.Length < minArgs)
            {
                await _cmdList.Commands[cmdName].DisplayArgLengthError(c);
                Log.Write(string.Format(
                    "Player {0} specified invalid # of args for {1}{2} command. Required: {3}," +
                    " specified: {4}, received: {5}. Ignoring.",
                    fromUser, CommandList.GameCommandPrefix, cmdName, minArgs, args.Length, msg),
                    _logClassType, _logPrefix);

                return;
            }
            // Execute
            if (await _cmdList.Commands[cmdName].ExecAsync(c))
            {
                Log.Write(string.Format(
                    "Successfully executed player {0}'s {1}{2} command: {3}", fromUser,
                    CommandList.GameCommandPrefix, cmdName, msg),
                    _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(string.Format(
                    "Unsuccessful execution of player {0}'s {1}{2} command: {3}", fromUser,
                    CommandList.GameCommandPrefix, cmdName, msg),
                    _logClassType, _logPrefix);
            }
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

            _cfgHandler.ReadConfiguration();
            return _playerCommandTime[user]
                .AddSeconds(_cfgHandler.Config.CoreOptions.requiredTimeBetweenCommands) < DateTime.Now;
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