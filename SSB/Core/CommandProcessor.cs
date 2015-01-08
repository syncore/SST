using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
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
        public const string CmdAbort = "abort";
        public const string CmdAcc = "acc";
        public const string CmdAcceptTeamSuggestion = "accept";
        public const string CmdAccess = "access";
        public const string CmdAccountDate = "date";
        public const string CmdAddUser = "adduser";
        public const string CmdAllReady = "allready";
        public const string CmdDelUser = "deluser";
        public const string CmdDeOp = "deop";
        public const string CmdEarlyQuit = "earlyquit";
        public const string CmdElo = "elo";
        public const string CmdFindPlayer = "findplayer";
        public const string CmdForceJoinBlue = "blue";
        public const string CmdForceJoinRed = "red";
        public const string CmdForceJoinSpec = "spec";
        public const string CmdHelp = "help";
        public const string CmdInvite = "invite";
        public const string CmdKickBan = "kickban";
        public const string CmdLock = "lock";
        public const string CmdModule = "mod";
        public const string CmdMute = "mute";
        public const string CmdOp = "op";
        public const string CmdPause = "pause";
        public const string CmdReload = "reload";
        public const string CmdRejectTeamSuggestion = "reject";
        public const string CmdSeen = "seen";
        public const string CmdServers = "servers";
        public const string CmdShutdown = "shutdown";
        public const string CmdStopServer = "stopserver";
        public const string CmdSuggestTeams = "suggest";
        public const string CmdTimeBan = "timeban";
        public const string CmdUnban = "unban";
        public const string CmdUnlock = "unlock";
        public const string CmdUnmute = "unmute";
        public const string CmdUnpause = "unpause";
        public const string CmdVoteNo = "no";
        public const string CmdVoteYes = "yes";
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
            _playerCommandTime = new Dictionary<string, DateTime>();
            _commands = new Dictionary<string, IBotCommand>
            {
                {CmdAbort, new AbortCmd(_ssb)},
                {CmdAcc, new AccCmd(_ssb)},
                {CmdAccess, new AccessCmd(_ssb)},
                {CmdAccountDate, new AccountDateCmd(_ssb)},
                {CmdAddUser, new AddUserCmd(_ssb)},
                {CmdAllReady, new AllReadyCmd(_ssb)},
                {CmdForceJoinBlue, new ForceJoinBlueCmd(_ssb)},
                {CmdFindPlayer, new FindPlayerCmd(_ssb)},
                {CmdDelUser, new DelUserCmd(_ssb)},
                {CmdDeOp, new DeOpCmd(_ssb)},
                {CmdEarlyQuit, new EarlyQuitCmd(_ssb)},
                {CmdElo, new EloCmd(_ssb)},
                {CmdHelp, new HelpCmd(_ssb)},
                {CmdInvite, new InviteCmd(_ssb)},
                {CmdModule, new ModuleCmd(_ssb)},
                {CmdLock, new LockCmd(_ssb)},
                {CmdOp, new OpCmd(_ssb)},
                {CmdMute, new MuteCmd(_ssb)},
                {CmdVoteNo, new VoteNoCmd(_ssb)},
                {CmdKickBan, new KickBanCmd(_ssb)},
                {CmdPause, new PauseCmd(_ssb)},
                {CmdForceJoinRed, new ForceJoinRedCmd(_ssb)},
                {CmdForceJoinSpec, new ForceJoinSpecCmd(_ssb)},
                {CmdServers, new ServersCmd(_ssb)},
                {CmdSuggestTeams, new SuggestTeamsCmd(_ssb)},
                {CmdTimeBan, new TimeBanCmd(_ssb)},
                {CmdUnban, new UnbanCmd(_ssb)},
                {CmdUnlock, new UnlockCmd(_ssb)},
                {CmdUnmute, new UnmuteCmd(_ssb)},
                {CmdUnpause, new UnpauseCmd(_ssb)},
                {CmdReload, new ReloadCmd(_ssb)},
                {CmdSeen, new SeenCmd(_ssb)},
                {CmdShutdown, new ShutdownCmd(_ssb)},
                {CmdStopServer, new StopServerCmd(_ssb)},
                {CmdVoteYes, new VoteYesCmd(_ssb)},
                {CmdAcceptTeamSuggestion, new AcceptTeamSuggestCmd(_ssb)},
                {CmdRejectTeamSuggestion, new RejectTeamSuggestCmd(_ssb)},
            };
        }

        /// <summary>
        ///     Processes the bot command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessBotCommand(string fromUser, string msg)
        {
            char[] sep = { ' ' };
            string[] args = msg.Split(sep, 5);
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
            if (!Tools.KeyExists(fromUser, _ssb.ServerInfo.CurrentPlayers))
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 {0},^3 please give the bot time to sync your user info and then retry your {1} request.",
                        fromUser, cmdName));
                return;
            }
            string user = _ssb.ServerInfo.CurrentPlayers[fromUser].ShortName;
            if (!UserHasReqLevel(user, _commands[cmdName].UserLevel))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You do not have permission to use that command.");
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
            if (!Tools.KeyExists(user, _playerCommandTime) || _users.GetUserLevel(user) >= UserLevel.Admin)
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