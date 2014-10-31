using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Core.Commands.Limits;
using SSB.Core.Commands.None;
using SSB.Core.Commands.Owner;
using SSB.Core.Commands.SuperUser;
using SSB.Database;
using SSB.Enum;
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
        public const string BotCommandPrefix = "!";
        private readonly Dictionary<string, IBotCommand> _commands;
        private readonly Dictionary<string, DateTime> _playerCommandTime;
        private readonly SynServerBot _ssb;
        private readonly Users _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public CommandProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
            Limiter = new Limiter(_ssb);
            _playerCommandTime = new Dictionary<string, DateTime>();
            _commands = new Dictionary<string, IBotCommand>
            {
                {"abort", new AbortCmd(_ssb)},
                {"access", new AccessCmd(_ssb)},
                {"date", new AccountDateCmd(_ssb)},
                {"adduser", new AddUserCmd(_ssb)},
                {"allready", new AllReadyCmd(_ssb)},
                {"blue", new ForceJoinBlueCmd(_ssb)},
                {"deluser", new DelUserCmd(_ssb)},
                {"deop", new DeOpCmd(_ssb)},
                {"elo", new EloCmd(_ssb)},
                {"help", new HelpCmd(_ssb)},
                {"invite", new InviteCmd(_ssb)},
                {"limit", new LimitCmd(_ssb, Limiter)},
                {"lock", new LockCmd(_ssb)},
                {"op", new OpCmd(_ssb)},
                {"mute", new MuteCmd(_ssb)},
                {"no", new VoteNoCmd(_ssb)},
                {"kickban", new KickBanCmd(_ssb)},
                {"pause", new PauseCmd(_ssb)},
                {"red", new ForceJoinRedCmd(_ssb)},
                {"spec", new ForceJoinSpecCmd(_ssb)},
                {"unban", new UnbanCmd(_ssb)},
                {"unlock", new UnlockCmd(_ssb)},
                {"unmute", new UnmuteCmd(_ssb)},
                {"unpause", new UnpauseCmd(_ssb)},
                {"refresh", new RefreshCmd(_ssb)},
                {"seen", new SeenCmd(_ssb)},
                {"shutdown", new ShutdownCmd(_ssb)},
                {"stopserver", new StopServerCmd(_ssb)},
                {"yes", new VoteYesCmd(_ssb)},
            };
        }

        /// <summary>
        ///     Gets the limiter.
        /// </summary>
        /// <value>
        ///     The limiter.
        /// </value>
        public Limiter Limiter { get; private set; }

        /// <summary>
        ///     Processes the bot command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessBotCommand(string fromUser, string msg)
        {
            char[] sep = {' '};
            string[] args = msg.Split(sep, 5);
            string cmdName = args[0].Substring(1);
            if (!SufficientTimeElapsed(fromUser))
            {
                Debug.WriteLine(
                    "Sufficient time has not elapsed since {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, BotCommandPrefix, cmdName);
                return;
            }
            _playerCommandTime[fromUser] = DateTime.Now;
            if (msg.Equals(BotCommandPrefix))
            {
                return;
            }
            if (!Tools.KeyExists(cmdName, _commands))
            {
                return;
            }
            string user = _ssb.ServerInfo.CurrentPlayers[fromUser].ShortName;
            if (!UserHasReqLevel(user, _commands[cmdName].UserLevel))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^7 You do not have permission to use that command.");
                return;
            }
            var c = new CmdArgs(args, cmdName, user);
            if (args.Length < _commands[cmdName].MinArgs)
            {
                await _commands[cmdName].DisplayArgLengthError(c);
                return;
            }
            // Execute
            await _commands[cmdName].ExecAsync(c);
        }

        /// <summary>
        ///     Checks whether sufficient time has elapsed since the user last issued a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if sufficient time has elapsed, otherwise <c>false</c>.</returns>
        private bool SufficientTimeElapsed(string user)
        {
            if (!Tools.KeyExists(user, _playerCommandTime))
            {
                return true;
            }
            // 6.5 seconds between commands
            if (_playerCommandTime[user].AddSeconds(6.5) < DateTime.Now)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Checks whether the user has the required access level to access a command.
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