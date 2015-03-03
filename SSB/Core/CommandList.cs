using System;
using System.Collections.Generic;
using SSB.Core.Commands.Admin;
using SSB.Core.Commands.None;
using SSB.Core.Commands.Owner;
using SSB.Core.Commands.SuperUser;
using SSB.Interfaces;

namespace SSB.Core
{
    /// <summary>
    /// Class that contains the internal commands.
    /// </summary>
    public class CommandList
    {
        public const string GameCommandPrefix = "!";
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
        public const string CmdPickupPick = "pick";
        public const string CmdPickupRemove = "r";
        public const string CmdPickupSub = "sub";
        public const string CmdPickupWho = "who";
        public const string CmdRejectTeamSuggestion = "reject";
        public const string CmdReload = "reload";
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
        private readonly SynServerBot _ssb;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandList"/> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        public CommandList(SynServerBot ssb)
        {
            _ssb = ssb;
            Commands = new Dictionary<string, IBotCommand>(StringComparer.InvariantCultureIgnoreCase);
            InitializeCommands();
        }

        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>
        /// The commands.
        /// </value>
        public Dictionary<string, IBotCommand> Commands { get; private set; }

        /// <summary>
        /// Initializes the commands.
        /// </summary>
        private void InitializeCommands()
        {
            Commands.Add(CmdAbort, new AbortCmd(_ssb));
            Commands.Add(CmdAcc, new AccCmd(_ssb));
            Commands.Add(CmdAccess, new AccessCmd(_ssb));
            Commands.Add(CmdAccountDate, new AccountDateCmd(_ssb));
            Commands.Add(CmdAddUser, new AddUserCmd(_ssb));
            Commands.Add(CmdAllReady, new AllReadyCmd(_ssb));
            Commands.Add(CmdForceJoinBlue, new ForceJoinBlueCmd(_ssb));
            Commands.Add(CmdFindPlayer, new FindPlayerCmd(_ssb));
            Commands.Add(CmdDelUser, new DelUserCmd(_ssb));
            Commands.Add(CmdDeOp, new DeOpCmd(_ssb));
            Commands.Add(CmdEarlyQuit, new EarlyQuitCmd(_ssb));
            Commands.Add(CmdElo, new EloCmd(_ssb));
            Commands.Add(CmdHelp, new HelpCmd(_ssb));
            Commands.Add(CmdInvite, new InviteCmd(_ssb));
            Commands.Add(CmdMap, new MapCmd(_ssb));
            Commands.Add(CmdModule, new ModuleCmd(_ssb));
            Commands.Add(CmdLock, new LockCmd(_ssb));
            Commands.Add(CmdOp, new OpCmd(_ssb));
            Commands.Add(CmdMute, new MuteCmd(_ssb));
            Commands.Add(CmdVoteNo, new VoteNoCmd(_ssb));
            Commands.Add(CmdKickBan, new KickBanCmd(_ssb));
            Commands.Add(CmdPause, new PauseCmd(_ssb));
            Commands.Add(CmdPickup, new PickupCmd(_ssb));
            Commands.Add(CmdPickupAdd, new PickupAddCmd(_ssb));
            Commands.Add(CmdPickupCap, new PickupCapCmd(_ssb));
            Commands.Add(CmdPickupRemove, new PickupRemoveCmd(_ssb));
            Commands.Add(CmdPickupPick, new PickupPickCmd(_ssb));
            Commands.Add(CmdPickupSub, new PickupSubCmd(_ssb));
            Commands.Add(CmdPickupWho, new PickupWhoCmd(_ssb));
            Commands.Add(CmdForceJoinRed, new ForceJoinRedCmd(_ssb));
            Commands.Add(CmdForceJoinSpec, new ForceJoinSpecCmd(_ssb));
            Commands.Add(CmdServers, new ServersCmd(_ssb));
            Commands.Add(CmdSuggestTeams, new SuggestTeamsCmd(_ssb));
            Commands.Add(CmdTimeBan, new TimeBanCmd(_ssb));
            Commands.Add(CmdUnban, new UnbanCmd(_ssb));
            Commands.Add(CmdUnlock, new UnlockCmd(_ssb));
            Commands.Add(CmdUnmute, new UnmuteCmd(_ssb));
            Commands.Add(CmdUnpause, new UnpauseCmd(_ssb));
            Commands.Add(CmdReload, new ReloadCmd(_ssb));
            Commands.Add(CmdSeen, new SeenCmd(_ssb));
            Commands.Add(CmdShutdown, new ShutdownCmd(_ssb));
            Commands.Add(CmdStopServer, new StopServerCmd(_ssb));
            Commands.Add(CmdVoteYes, new VoteYesCmd(_ssb));
            Commands.Add(CmdAcceptTeamSuggestion, new AcceptTeamSuggestCmd(_ssb));
            Commands.Add(CmdRejectTeamSuggestion, new RejectTeamSuggestCmd(_ssb));
        }
    }
}