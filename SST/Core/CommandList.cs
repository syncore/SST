using System;
using System.Collections.Generic;
using SST.Core.Commands.Admin;
using SST.Core.Commands.None;
using SST.Core.Commands.Owner;
using SST.Core.Commands.SuperUser;
using SST.Interfaces;

namespace SST.Core
{
    /// <summary>
    /// Class that contains the internal commands.
    /// </summary>
    public class CommandList
    {
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
        public const string CmdMap = "map";
        public const string CmdModule = "mod";
        public const string CmdMute = "mute";
        public const string CmdOp = "op";
        public const string CmdPause = "pause";
        public const string CmdPickup = "pickup";
        public const string CmdPickupAdd = "a";
        public const string CmdPickupCap = "cap";
        public const string CmdPickupLastGame = "last";
        public const string CmdPickupPick = "pick";
        public const string CmdPickupRemove = "r";
        public const string CmdPickupSub = "sub";
        public const string CmdPickupTopTen = "top10";
        public const string CmdPickupUserInfo = "pinfo";
        public const string CmdPickupWho = "who";
        public const string CmdRejectTeamSuggestion = "reject";
        public const string CmdReload = "reload";
        public const string CmdRestoreTeams = "restoreteams";
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
        public const string CmdUsers = "users";
        public const string CmdVersion = "version";
        public const string CmdVoteNo = "no";
        public const string CmdVoteYes = "yes";
        public const string GameCommandPrefix = "!";
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandList"/> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        public CommandList(SynServerTool sst)
        {
            _sst = sst;
            Commands = new Dictionary<string, IBotCommand>(StringComparer.InvariantCultureIgnoreCase);
            InitializeCommands();
        }

        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>The commands.</value>
        public Dictionary<string, IBotCommand> Commands { get; private set; }

        /// <summary>
        /// Initializes the commands.
        /// </summary>
        private void InitializeCommands()
        {
            Commands.Add(CmdAbort, new AbortCmd(_sst));
            Commands.Add(CmdAcc, new AccCmd(_sst));
            Commands.Add(CmdAccess, new AccessCmd(_sst));
            Commands.Add(CmdAccountDate, new AccountDateCmd(_sst));
            Commands.Add(CmdAddUser, new AddUserCmd(_sst));
            Commands.Add(CmdAllReady, new AllReadyCmd(_sst));
            Commands.Add(CmdForceJoinBlue, new ForceJoinBlueCmd(_sst));
            Commands.Add(CmdFindPlayer, new FindPlayerCmd(_sst));
            Commands.Add(CmdDelUser, new DelUserCmd(_sst));
            Commands.Add(CmdDeOp, new DeOpCmd(_sst));
            Commands.Add(CmdEarlyQuit, new EarlyQuitCmd(_sst));
            Commands.Add(CmdElo, new EloCmd(_sst));
            Commands.Add(CmdHelp, new HelpCmd(_sst));
            Commands.Add(CmdInvite, new InviteCmd(_sst));
            Commands.Add(CmdMap, new MapCmd(_sst));
            Commands.Add(CmdModule, new ModuleCmd(_sst));
            Commands.Add(CmdLock, new LockCmd(_sst));
            Commands.Add(CmdOp, new OpCmd(_sst));
            Commands.Add(CmdMute, new MuteCmd(_sst));
            Commands.Add(CmdVoteNo, new VoteNoCmd(_sst));
            Commands.Add(CmdKickBan, new KickBanCmd(_sst));
            Commands.Add(CmdPause, new PauseCmd(_sst));
            Commands.Add(CmdPickup, new PickupCmd(_sst));
            Commands.Add(CmdPickupAdd, new PickupAddCmd(_sst));
            Commands.Add(CmdPickupCap, new PickupCapCmd(_sst));
            Commands.Add(CmdPickupLastGame, new PickupLastGameCmd(_sst));
            Commands.Add(CmdPickupUserInfo, new PickupUserInfoCmd(_sst));
            Commands.Add(CmdPickupRemove, new PickupRemoveCmd(_sst));
            Commands.Add(CmdPickupPick, new PickupPickCmd(_sst));
            Commands.Add(CmdPickupSub, new PickupSubCmd(_sst));
            Commands.Add(CmdPickupTopTen, new PickupTopTenCmd(_sst));
            Commands.Add(CmdPickupWho, new PickupWhoCmd(_sst));
            Commands.Add(CmdForceJoinRed, new ForceJoinRedCmd(_sst));
            Commands.Add(CmdForceJoinSpec, new ForceJoinSpecCmd(_sst));
            Commands.Add(CmdRestoreTeams, new RestoreTeamsCmd(_sst));
            Commands.Add(CmdServers, new ServersCmd(_sst));
            Commands.Add(CmdSuggestTeams, new SuggestTeamsCmd(_sst));
            Commands.Add(CmdTimeBan, new TimeBanCmd(_sst));
            Commands.Add(CmdUnban, new UnbanCmd(_sst));
            Commands.Add(CmdUnlock, new UnlockCmd(_sst));
            Commands.Add(CmdUnmute, new UnmuteCmd(_sst));
            Commands.Add(CmdUnpause, new UnpauseCmd(_sst));
            Commands.Add(CmdUsers, new UsersCmd(_sst));
            Commands.Add(CmdReload, new ReloadCmd(_sst));
            Commands.Add(CmdSeen, new SeenCmd(_sst));
            Commands.Add(CmdShutdown, new ShutdownCmd(_sst));
            Commands.Add(CmdStopServer, new StopServerCmd(_sst));
            Commands.Add(CmdVoteYes, new VoteYesCmd(_sst));
            Commands.Add(CmdVersion, new VersionCmd(_sst));
            Commands.Add(CmdAcceptTeamSuggestion, new AcceptTeamSuggestCmd(_sst));
            Commands.Add(CmdRejectTeamSuggestion, new RejectTeamSuggestCmd(_sst));
        }
    }
}
