using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core
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
        private readonly SynServerTool _sst;
        private readonly DbUsers _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandProcessor" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public CommandProcessor(SynServerTool sst)
        {
            _sst = sst;
            _users = new DbUsers();
            _cmdList = new CommandList(_sst);
            _cfgHandler = new ConfigHandler();
            _playerCommandTime = new Dictionary<string, DateTime>();
        }

        /// <summary>
        ///     Gets the commands.
        /// </summary>
        /// <value>
        ///     The commands.
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
            var args = msg.Split(sep);
            var cmdName = args[0].Substring(1);

            // Verify command
            if (!await CheckCommand(fromUser, cmdName, msg)) return;

            // Verify argument length
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
                    "Successfully executed player {0}'s {1} command: {2}", fromUser, cmdName, msg),
                    _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(string.Format(
                    "Unsuccessful execution of player {0}'s {1} command: {2}", fromUser,
                    cmdName, msg), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        ///     Determines whether the command can be executed without taking into account
        ///     the required argument length.
        /// </summary>
        /// <param name="fromUser">The sender of the command..</param>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="fullMessageText">The full message text.</param>
        /// <returns>
        ///     <c>true</c> if the command can be executed, without taking into account
        ///     required argument length of the command, otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> CheckCommand(string fromUser, string commandName, string fullMessageText)
        {
            if (!_sst.IsInitComplete)
            {
                await
                    _sst.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Initilization has not completed yet. Command ignored.");

                Log.Write(
                    string.Format("Initilization not yet completed; ignoring command from player {0}.",
                        fromUser), _logClassType, _logPrefix);

                return false;
            }
            if (!SufficientTimeElapsed(fromUser))
            {
                Log.Write(string.Format(
                    "Sufficient time has not elapsed since player {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, CommandList.GameCommandPrefix, commandName), _logClassType, _logPrefix);

                return false;
            }
            _playerCommandTime[fromUser] = DateTime.Now;
            if (fullMessageText.Equals(CommandList.GameCommandPrefix))
            {
                Log.Write(
                    string.Format(
                        "Player {0} entered command prefix {1} without specifying command. Ignoring.",
                        fromUser, CommandList.GameCommandPrefix), _logClassType, _logPrefix);

                return false;
            }
            if (!Helpers.KeyExists(commandName, _cmdList.Commands))
            {
                Log.Write(
                    string.Format("Player {0} entered command ({1}) that does not exist. Ignoring.",
                        fromUser, commandName), _logClassType, _logPrefix);

                return false;
            }
            if (!Helpers.KeyExists(fromUser, _sst.ServerInfo.CurrentPlayers))
            {
                // Player has not been indexed
                await _sst.QlCommands.QlCmdPlayers();
                Log.Write(
                    string.Format("Player {0} has not been indexed in current player list. Ignoring command & will re-scan player list.",
                        fromUser), _logClassType, _logPrefix);
                
                await _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 {0},^3 please give the bot time to sync your user info and then" +
                        " retry your {1} request in a few secs.",
                        fromUser, commandName));

                return false;
            }
            if (!UserHasReqLevel(fromUser, _cmdList.Commands[commandName].UserLevel))
            {
                await _sst.QlCommands.QlCmdSay("^1[ERROR]^3 You do not have permission to use that command.");
                Log.Write(
                    string.Format(
                        "Player {0} sent {1}{2} command but has permission less than {3} needed for {2}. Ingoring.",
                        fromUser, CommandList.GameCommandPrefix, commandName, Enum.GetName(typeof(UserLevel),
                            _cmdList.Commands[commandName].UserLevel)), _logClassType, _logPrefix);

                return false;
            }

            return true;
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

            var cfg = _cfgHandler.ReadConfiguration();
            return _playerCommandTime[user]
                .AddSeconds(cfg.CoreOptions.requiredTimeBetweenCommands) < DateTime.Now;
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