using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SST.Config;
using SST.Core.Commands.None;
using SST.Database;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Core.Modules
{
    /// <summary>
    ///     Class responsible for managing pickup games.
    /// </summary>
    public class PickupManager
    {
        private const double CaptainSelectionTimeLimit = 20000;
        private const double PickupResetOnEndGameLimit = 65000;
        private readonly DbBans _banDb;
        private readonly PickupCaptains _captains;
        private readonly Timer _captainSelectionTimer;
        private readonly Timer _endGameResetTimer;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:PICKUP]";
        private readonly PickupPlayers _players;
        private readonly SynServerTool _sst;
        private readonly DbUsers _userDb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupManager" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public PickupManager(SynServerTool sst)
        {
            _sst = sst;
            _captains = new PickupCaptains(_sst, this);
            _players = new PickupPlayers(_sst, this);
            _userDb = new DbUsers();
            _banDb = new DbBans();
            AvailablePlayers = new List<string>();
            ActivePickupPlayers = new List<string>();
            SubCandidates = new List<string>();
            Subs = new StringBuilder();
            NoShows = new StringBuilder();
            _captainSelectionTimer = new Timer();
            _endGameResetTimer = new Timer();
        }

        /// <summary>
        ///     Gets or sets the active pickup players.
        /// </summary>
        /// <value>
        ///     The active pickup players.
        /// </value>
        /// <remarks>
        ///     This is used to supplement QL's actual player team tracking of those players who are
        ///     on a team for the pickup. This is included because sometimes the game's actual team
        ///     tracking is not reliably updated.
        /// </remarks>
        public List<string> ActivePickupPlayers { get; set; }

        /// <summary>
        ///     Gets a value indicating whether the teams are full.
        /// </summary>
        /// <value>
        ///     <c>true</c> if teams are full; otherwise, <c>false</c>.
        /// </value>
        public bool AreTeamsFull
        {
            get
            {
                return (_sst.ServerInfo.GetTeam(Team.Red).Count == _sst.Mod.Pickup.Teamsize) &&
                       ((_sst.ServerInfo.GetTeam(Team.Blue).Count == _sst.Mod.Pickup.Teamsize));
            }
        }

        /// <summary>
        ///     Gets or sets the players that are eligible to be picked for a pickup game.
        /// </summary>
        /// <value>
        ///     The players that are eligible to be picked for a pickup game.
        /// </value>
        public List<string> AvailablePlayers { get; set; }

        /// <summary>
        ///     Gets the pickup captains class.
        /// </summary>
        /// <value>
        ///     The pickup captains class.
        /// </value>
        public PickupCaptains Captains
        {
            get { return _captains; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the captain selection process has started.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the captain selection process started; otherwise, <c>false</c>.
        /// </value>
        public bool HasCaptainSelectionStarted { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the team selection process has started.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the team selection process started; otherwise, <c>false</c>.
        /// </value>
        public bool HasTeamSelectionStarted { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the tool is currently setting up teams (locking server down).
        /// </summary>
        /// <value>
        ///     <c>true</c> if the bot is setting up teams (locking server down); otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     This value indicates whether the <see cref="SetupTeams()" /> method is executing, to serve as a guard
        ///     that indicates that the teams should not be unlocked due to no-shows, which would typically happen when an active
        ///     player goes to SPEC or disconnects; here, the tool moves back to spec after locking the teams, which would
        ///     otherwise trigger an unlock.
        /// </remarks>
        public bool IsBotSettingUpTeams { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the game is in intermission
        ///     (i.e. between time/frag/roundlimit reached and the end of the end-game
        ///     map vote, if enabled.)
        /// </summary>
        /// <value>
        ///     <c>true</c> if the game is in intermission; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     The primary purpose of this is to serve as a guard that will allow players to disconnect
        ///     during this period without it counting towards their no-show count.
        /// </remarks>
        public bool IsIntermission { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether a pickup in progress.
        /// </summary>
        /// <value>
        ///     <c>true</c> if a pickup in progress; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     This covers the time period from when the pickup game launches until
        ///     it is over.
        /// </remarks>
        public bool IsPickupInProgress { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the server is in pickup pre-game mode.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the server is in pickup pre-game mode; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     This covers the time period from the issuance of the pickup start command up until
        ///     when the pickup game actually launches.
        /// </remarks>
        public bool IsPickupPreGame { get; set; }

        /// <summary>
        ///     Gets a value that indicates whether a Quake Live game is in progress.
        /// </summary>
        /// <returns><c>true</c> if the QL game is in countdown or in-progress mode, otherwise <c>false</c>.</returns>
        public bool IsQlGameInProgress
        {
            get
            {
                return _sst.ServerInfo.CurrentServerGameState == QlGameStates.Countdown ||
                       _sst.ServerInfo.CurrentServerGameState == QlGameStates.InProgress;
            }
        }

        /// <summary>
        ///     Gets or sets the missing blue player count of players who leave
        ///     prematurely without securing a substitute replacement.
        /// </summary>
        /// <value>
        ///     The missing blue player count of players who leave
        ///     prematurely without securing a substitute replacement.
        /// </value>
        public int MissingBluePlayerCount { get; set; }

        /// <summary>
        ///     Gets or sets the missing red player count of players who leave
        ///     prematurely without securing a substitute replacement.
        /// </summary>
        /// <value>
        ///     The missing red player count of players who leave
        ///     prematurely without securing a substitute replacement.
        /// </value>
        public int MissingRedPlayerCount { get; set; }

        /// <summary>
        ///     Gets or sets the no-show players for purposes of record keeping.
        /// </summary>
        /// <value>
        ///     The no-show players for purposes of record keeping.
        /// </value>
        public StringBuilder NoShows { get; set; }

        /// <summary>
        ///     Gets pickup players class.
        /// </summary>
        /// <value>
        ///     The pickup players class.
        /// </value>
        public PickupPlayers Players
        {
            get { return _players; }
        }

        /// <summary>
        ///     Gets or sets the status message.
        /// </summary>
        /// <value>
        ///     The status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Gets or sets the players who are substitute players.
        /// </summary>
        /// <value>
        ///     The list of players who are eligible to be subbed in.
        /// </value>
        public List<string> SubCandidates { get; set; }

        /// <summary>
        ///     Gets or sets the substitite players for purposes of record keeping.
        /// </summary>
        /// <value>
        ///     The substitute players for purposes of record keeping.
        /// </value>
        public StringBuilder Subs { get; set; }

        /// <summary>
        ///     Adds the active pickup player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        ///     This is used for <see cref="ActivePickupPlayers" />, which is the tool's internal list
        ///     of those players who are on a team for the pickup. This is included because sometimes
        ///     the game's actual team tracking is not reliably updated.
        /// </remarks>
        public void AddActivePickupPlayer(string player)
        {
            player = player.ToLowerInvariant();
            if (!ActivePickupPlayers.Contains(player))
            {
                ActivePickupPlayers.Add(player);
                Log.Write(string.Format("{0} was added to the active pickup players.",
                    player), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        ///     Creates a pickup info object for the current pickup game.
        /// </summary>
        /// <returns>A <see cref="PickupInfo" /> object representing the current pickup game.</returns>
        public PickupInfo CreatePickupInfo()
        {
            var pickupInfo = new PickupInfo();
            var redTeam = new StringBuilder();
            var blueTeam = new StringBuilder();

            foreach (var player in _sst.ServerInfo.GetTeam(Team.Red))
            {
                redTeam.Append(string.Format("{0}, ", player.ShortName));
            }

            foreach (var player in _sst.ServerInfo.GetTeam(Team.Blue))
            {
                blueTeam.Append(string.Format("{0}, ", player.ShortName));
            }

            pickupInfo.RedTeam = redTeam.ToString().TrimEnd(',', ' ');
            pickupInfo.BlueTeam = blueTeam.ToString().TrimEnd(',', ' ');
            pickupInfo.RedCaptain = _captains.RedCaptain;
            pickupInfo.BlueCaptain = _captains.BlueCaptain;
            pickupInfo.Subs = Subs.ToString().TrimEnd(',', ' ');
            pickupInfo.NoShows = NoShows.ToString().TrimEnd(',', ' ');
            pickupInfo.StartDate = DateTime.Now;

            return pickupInfo;
        }

        /// <summary>
        ///     Displays the list of eligible players, if any.
        /// </summary>
        public async Task DisplayAvailablePlayers()
        {
            await _sst.QlCommands.QlCmdSay(string.Format("^7Eligible players: {0}",
                ((AvailablePlayers.Count != 0)
                    ? "^3" + string.Join(",", AvailablePlayers)
                    : "^1NO eligible players!")));
        }

        /// <summary>
        ///     Evauates a departing pickup user's noshow or sub status and punishes if necessary.
        /// </summary>
        /// <param name="player">The departing player.</param>
        /// <param name="playerWasActive">
        ///     if set to <c>true</c> indicates that the departing player was
        ///     active (i.e. was on the red or the blue team, thus not a spectator).
        /// </param>
        /// <param name="outgoingTeam">The departing player's team.</param>
        /// <returns></returns>
        public async Task EvalOutgoingPlayer(string player, bool playerWasActive,
            Team outgoingTeam)
        {
            if (!IsNoShowEvalApplicable(player, playerWasActive)) return;
            // No-show/sub database tracking operations
            await UpdateNoShowAndSubDatabase(player);
            var redSize = _sst.ServerInfo.GetTeam(Team.Red).Count;
            var blueSize = _sst.ServerInfo.GetTeam(Team.Blue).Count;
            // Reset because captain selection started and the captain left before completion of selection
            if (HasCaptainSelectionStarted &&
                (Captains.RedCaptain.Equals(player) || Captains.BlueCaptain.Equals(player)))
            {
                Log.Write(
                    string.Format("Captain {0} left prior to end of captain selection. Resetting pickup.",
                        player), _logClassType, _logPrefix);

                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[PICKUP]^7 Captain ^3{0}^7 has no-showed the game. Resetting pickup...",
                            player));
                await DoResetPickup();
            }
            // The captain left during the team selection process... Reset.
            if (HasTeamSelectionStarted &&
                (Captains.RedCaptain.Equals(player) || Captains.BlueCaptain.Equals(player)))
            {
                Log.Write(string.Format("Captain {0} left during team selection. Resetting pickup.",
                    player), _logClassType, _logPrefix);

                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[PICKUP]^7 Captain ^3{0}^7 has no-showed the game. Resetting pickup...",
                            player));
                await DoResetPickup();
            }
            // Team selection started and an eligible player leaves, notify of cancelation possibility
            if (HasTeamSelectionStarted && AvailablePlayers.Contains(player))
            {
                if ((_sst.Mod.Pickup.Teamsize * 2) - (redSize + blueSize) < AvailablePlayers.Count)
                {
                    await
                        _sst.QlCommands.QlCmdSay(string.Format(
                            "^5[PICKUP]^7 There might not be enough players to start because of ^3{0}^7's early quit",
                            player));
                    await
                        _sst.QlCommands.QlCmdSay(
                            "^5[PICKUP]^7 You can wait for more players to connect and sign up or the game can be canceled.");
                    await ShowPrivUserCanCancelMsg();
                }
            }
            // Player either A) leaves prematurely mid-game without getting sub, or B) leaves after being
            // picked and moved for a team, but before the game has started
            if ((IsPickupInProgress && playerWasActive) || (IsPickupPreGame && playerWasActive))
            {
                // Situation where departing player is moved to spec due to successful sub request. Do nothing.
                if (_sst.ServerInfo.CurrentPlayers[player].HasMadeSuccessfulSubRequest) return;

                Log.Write(string.Format("Player {0} left mid-game without securing a sub.",
                    player), _logClassType, _logPrefix);

                if (outgoingTeam == Team.Blue)
                {
                    await UnlockTeamDueToNoShow(player, Team.Blue);
                }
                else if (outgoingTeam == Team.Red)
                {
                    await UnlockTeamDueToNoShow(player, Team.Red);
                }
            }
        }

        /// <summary>
        ///     Evaluates whether the current pickup can be reset, and if so, resets it.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the evaluation passes;
        ///     otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> EvalPickupReset(CmdArgs c)
        {
            if (IsQlGameInProgress)
            {
                await ShowProgressInError(c);
                return false;
            }
            await DoResetPickup();
            return true;
        }

        /// <summary>
        ///     Evaluates whether the server can be put into pickup pre-game mode when the pickup start command is issued,
        ///     and if it can be, then puts the servers into the pickup pre-game mode.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the evaluation passes;
        ///     otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> EvalPickupStart(CmdArgs c)
        {
            if (!_sst.ServerInfo.IsATeamGame())
            {
                // Might have not gotten it the first time, so request again, in a few seconds.
                await _sst.QlCommands.SendToQlDelayedAsync("serverinfo", true, 5);
                _sst.QlCommands.ClearQlWinConsole();
                StatusMessage =
                    "^1[ERROR]^3 Pickup can only be started for team-based games. If this is an error, try again in 5 seconds.";
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} tried to start a pickup game, but we did not determine if this server is running" +
                    " a team game mode; requesting server info in 5 seconds.", c.FromUser), _logClassType,
                    _logPrefix);

                return false;
            }
            if (IsQlGameInProgress)
            {
                await ShowProgressInError(c);
                return false;
            }
            if (IsPickupInProgress)
            {
                StatusMessage = "^1[ERROR]^3 Another pickup game is already pending!";
                await SendServerTell(c, StatusMessage);

                Log.Write(
                    string.Format(
                        "{0} tried to start a pickup game but another pick up is in progress. Ignoring.",
                        c.FromUser), _logClassType, _logPrefix);
                return false;
            }
            await StartPickupPreGame();
            return true;
        }

        /// <summary>
        ///     Evaluates whether the current pickup can be stopped, and if so, stops it.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns></returns>
        public async Task<bool> EvalPickupStop(CmdArgs c)
        {
            if (IsQlGameInProgress)
            {
                await ShowProgressInError(c);
                return false;
            }
            await StopPickup(c);
            return true;
        }

        /// <summary>
        ///     Evaluates whether a user can be removed for no-show/sub abuse.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the user can be removed; otherwise
        ///     <c>false</c>.
        /// </returns>
        public async Task<bool> EvalPickupUnban(CmdArgs c)
        {
            if (c.Args.Length == (c.FromIrc ? 2 : 1))
            {
                StatusMessage = string.Format("^5[PICKUP]^7 Usage: ^3{0}{1} unban <player>",
                    CommandList.GameCommandPrefix, CommandList.CmdPickup);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var bInfo = _banDb.GetBanInfo(Helpers.GetArgVal(c, 1));
            if (bInfo == null)
            {
                StatusMessage = string.Format("^5[PICKUP]^7 Ban information not found for {0}",
                    Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (bInfo.BanType != BanType.AddedByPickupSubs ||
                bInfo.BanType != BanType.AddedByPickupNoShows)
            {
                var senderLevel = IsIrcOwner(c) ? UserLevel.Owner : _userDb.GetUserLevel(c.FromUser);
                // Don't allow SuperUsers to remove non pickup-related bans
                if (senderLevel == UserLevel.SuperUser)
                {
                    StatusMessage = string.Format(
                        "^5[PICKUP]^3 {0}^7 is banned but not for pickup no-show/sub abuse. Cannot remove.",
                        Helpers.GetArgVal(c, 1));
                    await SendServerTell(c, StatusMessage);

                    Log.Write(
                        "Super user {0} tried to issue a pickup un-ban for {1}, but player is banned for" +
                        " non-pickup related reasons; Ignoring. Only admins or higher can remove non-pickup related bans.",
                        _logClassType, _logPrefix);

                    return false;
                }
                // Notify admins and higher that they should use timeban del command to remove non-pickup related ban
                if (senderLevel > UserLevel.SuperUser)
                {
                    StatusMessage = string.Format(
                        "^5[PICKUP]^3 {0}^7 is banned but not for pickup no-show/sub abuse. To remove: ^3{1}{2} del {0}",
                        Helpers.GetArgVal(c, 1), CommandList.GameCommandPrefix,
                        CommandList.CmdTimeBan);
                    await SendServerTell(c, StatusMessage);
                    return false;
                }
            }
            // Remove the ban and reset the count, depending on type of ban.
            var bManager = new BanManager(_sst);
            await bManager.RemoveBan(bInfo);
            StatusMessage = string.Format("^5[PICKUP]^7 Removing pickup-related ban for ^3{0}",
                Helpers.GetArgVal(c, 1));
            await SendServerSay(c, StatusMessage);
            return true;
        }

        /// <summary>
        ///     Handles the start of intermission (period between end of game and end of endgame map-voting)
        /// </summary>
        public void HandleIntermissionStart()
        {
            IsIntermission = true;
            Log.Write("Start of intermission start detected.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Handles the end of the pickup (when the QL gamestate changes to WARM_UP)
        /// </summary>
        public void HandlePickupEnd()
        {
            // Pickup needs to have previously been in progress
            if (!IsPickupInProgress) return;
            Log.Write("Pickup game has now officially ended. Will proceed to update database.",
                _logClassType, _logPrefix);
            // Update the pickup DB table to incldue any changes that occurred between the
            // game start and the game end
            var pickupDb = new DbPickups();
            pickupDb.UpdateMostRecentPickupGame(CreatePickupInfo());
            // Update the games finished count for the player in the pickup users table
            foreach (var player in ActivePickupPlayers)
            {
                pickupDb.IncrementUserGamesFinishedCount(player);
            }
            // Set the end time
            pickupDb.UpdatePickupEndTime(DateTime.Now);
        }

        /// <summary>
        ///     Handles the pickup launch (when the QL gamestate changes to IN_PROGRESS)
        /// </summary>
        public void HandlePickupLaunch()
        {
            // Do not allow the game to start unless the teams are fully picked
            if (HasTeamSelectionStarted)
            {
                // ReSharper disable once UnusedVariable
                var r = DoResetPickup();
                return;
            }

            // Pickup pre-game extends from !pickup start to the game actually launching
            // So do nothing if for some reason we are not at this point
            if (!IsPickupPreGame) return;
            Log.Write("Pickup game has now officially started!", _logClassType, _logPrefix);
            
            // When game officially starts (IN_PROGRESS) update the pickup DB table to include
            // actual start time, any team member changes, subs &/or no-shows that occurred after the teams
            // were already full.
            var pickupDb = new DbPickups();
            pickupDb.UpdateMostRecentPickupGame(CreatePickupInfo());
            // Update the games started count and last played date for the player in the pickup users table
            foreach (var player in ActivePickupPlayers)
            {
                pickupDb.IncrementUserGamesStartedCount(player);
                pickupDb.UpdateUserLastPlayedDate(player);
            }
            // We are now in progress
            IsPickupPreGame = false;
            IsPickupInProgress = true;
            // Move any remaining eligible players who were not picked to the list of eligible substitutes and clear.
            foreach (
                var player in
                    AvailablePlayers.Where(player => !SubCandidates.Contains(player,
                        StringComparer.InvariantCultureIgnoreCase)))
            {
                SubCandidates.Add(player);

                Log.Write(string.Format("Adding un-picked player {0} to list of available substitutes",
                    player), _logClassType, _logPrefix);
            }
            AvailablePlayers.Clear();
        }

        /// <summary>
        ///     Handles the score or timelimit hit event.
        /// </summary>
        public void HandleScoreOrTimelimitHit()
        {
            // Pickup needs to have previously been in progress
            if (!IsPickupInProgress) return;
            Log.Write("Score or timelimit hit!", _logClassType, _logPrefix);
            IsIntermission = true;
            // Pickup has now officially ended
            HandlePickupEnd();
            // Start the pickup reset timer
            StartEndGameResetTimer();
            // ReSharper disable once UnusedVariable
            var i1 = _sst.QlCommands.QlCmdSay(string.Format(
                "^5[PICKUP]^7 Pickup is OVER. A new pickup should start ^3{0}^7 seconds after map restart or map change!",
                (PickupResetOnEndGameLimit / 1000)));
        }

        /// <summary>
        ///     Notifies the connecting user on how to sign up for next or current pickup game.
        /// </summary>
        /// <param name="user">The user.</param>
        public async Task NotifyConnectingUser(string user)
        {
            if (IsPickupPreGame)
            {
                await
                    _sst.QlCommands.QlCmdDelayedTell(
                        string.Format(
                            "^7This server is in pickup game mode. Type ^5{0}{1}^7 to sign up for the next game.",
                            CommandList.GameCommandPrefix, CommandList.CmdPickupAdd),
                        user, 25);
            }
            else if (IsPickupInProgress)
            {
                await
                    _sst.QlCommands.QlCmdDelayedTell(
                        string.Format(
                            "^7A pickup game is in progress. Type ^5{0}{1}^7 to sign up as a susbstitute player.",
                            CommandList.GameCommandPrefix, CommandList.CmdPickupAdd),
                        user, 25);
            }
        }

        /// <summary>
        ///     Notifies the new player that he has been picked.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team for which the player was picked.</param>
        public async Task NotifyNewPlayer(string player, Team team)
        {
            await
                _sst.QlCommands.QlCmdTell(string.Format(
                    "^7You've been added to {0}^7. If you must leave early: ^3!sub <spectator>^7 to avoid a no-show.",
                    ((team == Team.Red) ? "^1RED" : "^5BLUE")), player);
        }

        /// <summary>
        ///     Processes the player and/or captain substitution.
        /// </summary>
        /// <param name="fromPlayer">The player/captain sending the substitution request (the outgoing player/captain).</param>
        /// <param name="playerToSub">The player/captain to sub in.</param>
        /// <returns>
        ///     <c>true</c> if the sub was successfully processed; otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> ProcessSub(string fromPlayer, string playerToSub)
        {
            if (!IsPickupInProgress && !IsPickupPreGame)
            {
                await
                    _sst.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 You may not request a sub at this time.", fromPlayer);

                Log.Write(string.Format(
                    "{0} tried to request sub but subbing is not available (not in progress or in pre-game). Ignoring.",
                    fromPlayer), _logClassType, _logPrefix);

                return false;
            }
            if (_sst.ServerInfo.IsActivePlayer(playerToSub) ||
                ActivePickupPlayers.Contains(playerToSub))
            {
                await
                    _sst.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 Your replacement cannot already be in the pickup.",
                        fromPlayer);

                Log.Write(string.Format(
                    "{0} tried to request sub but replacement player {1} is already in pickup. Ignoring.",
                    fromPlayer, playerToSub), _logClassType, _logPrefix);

                return false;
            }
            if (!Helpers.KeyExists(playerToSub, _sst.ServerInfo.CurrentPlayers))
            {
                await
                    _sst.QlCommands.QlCmdTell(
                        string.Format("^1[ERROR]^3 {0} is not currently on the server!",
                            playerToSub), fromPlayer);

                Log.Write(string.Format(
                    "{0} tried to request sub but replacement player {1} is not on the server. Ignoring.",
                    fromPlayer, playerToSub), _logClassType, _logPrefix);

                return false;
            }
            if (IsPickupPreGame && !AvailablePlayers.Contains(playerToSub))
            {
                // use a /say here, instead of the usual /tell for errors,
                // so new people know how to sign up
                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 {0} has not signed up with: ^7{1}{2}^3 yet!",
                            playerToSub,
                            CommandList.GameCommandPrefix, CommandList.CmdPickupAdd));
                return false;
            }
            if (IsPickupInProgress && !SubCandidates.Contains(playerToSub))
            {
                // use a /say here, instead of the usual /tell for errors,
                // so new people know how to sign up
                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 {0} has not signed up with: ^7{1}{2}^3 yet!",
                            playerToSub,
                            CommandList.GameCommandPrefix, CommandList.CmdPickupAdd));
                return false;
            }
            if (!_sst.ServerInfo.IsActivePlayer(fromPlayer) && !ActivePickupPlayers.Contains(fromPlayer))
            {
                await
                    _sst.QlCommands.QlCmdTell("^1[ERROR]^3 You may not request a sub.",
                        fromPlayer);
                return false;
            }
            if (_captains.RedCaptain.Equals(fromPlayer) ||
                _captains.BlueCaptain.Equals(fromPlayer))
            {
                // Captains are not allowed to sub during pre-game.
                if (IsPickupPreGame)
                {
                    await
                        _sst.QlCommands.QlCmdTell(
                            "^1[ERROR]^3 Captains can't request subs before the game starts!",
                            fromPlayer);

                    Log.Write(string.Format(
                        "Captain {0} tried to request sub replacement but pickup hasn't started. Ignoring.",
                        fromPlayer), _logClassType, _logPrefix);

                    return false;
                }
                // However, captains CAN requests sub during the game, treat as a regular sub
                if (IsPickupInProgress)
                {
                    Log.Write(string.Format(
                        "Sub request: Captain {0} on TEAM: {1} is requesting that {2} play as his substitute",
                        fromPlayer, _sst.ServerInfo.CurrentPlayers[fromPlayer].Team, playerToSub),
                        _logClassType, _logPrefix);

                    await
                        _players.DoPlayerSub(fromPlayer, _sst.ServerInfo.CurrentPlayers[fromPlayer].Team,
                            playerToSub);
                    return true;
                }
            }
            else
            {
                Log.Write(string.Format(
                    "Sub request: Player {0} on TEAM: {1} is requesting that {2} play as his substitute",
                    fromPlayer, _sst.ServerInfo.CurrentPlayers[fromPlayer].Team, playerToSub), _logClassType,
                    _logPrefix);

                await
                    _players.DoPlayerSub(fromPlayer, _sst.ServerInfo.CurrentPlayers[fromPlayer].Team,
                        playerToSub);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Removes the active pickup player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        ///     This is used for <see cref="ActivePickupPlayers" />, which is the tool's internal list
        ///     of those players who are on a team for the pickup. This is included because sometimes
        ///     the game's actual team tracking is not reliably updated.
        /// </remarks>
        public void RemoveActivePickupPlayer(string player)
        {
            if (ActivePickupPlayers.Remove(player.ToLowerInvariant()))
            {
                Log.Write(string.Format("{0} was removed from the active pickup players",
                    player), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        ///     Removes the player from the list of eligible players and/or eligible sub candidates.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        public void RemoveEligibility(string player)
        {
            if (AvailablePlayers.Remove(player.ToLowerInvariant()))
            {
                Log.Write(string.Format("{0} was removed from the eligible players",
                    player), _logClassType, _logPrefix);
            }
            if (SubCandidates.Remove(player.ToLowerInvariant()))
            {
                Log.Write(string.Format(
                    "{0} was removed from the in-progress sub candidates",
                    player), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        ///     Resets the pickup status. This is the overall housekeeping method.
        /// </summary>
        public void ResetPickupStatus()
        {
            AvailablePlayers.Clear();
            ActivePickupPlayers.Clear();
            SubCandidates.Clear();
            Subs.Clear();
            NoShows.Clear();
            MissingBluePlayerCount = 0;
            MissingRedPlayerCount = 0;
            IsBotSettingUpTeams = false;
            HasCaptainSelectionStarted = false;
            HasTeamSelectionStarted = false;
            IsIntermission = false;
            IsPickupPreGame = false;
            IsPickupInProgress = false;
            _captains.IsBlueTurnToPick = false;
            _captains.IsRedTurnToPick = false;
            _captains.RedCaptain = string.Empty;
            _captains.BlueCaptain = string.Empty;
            _captainSelectionTimer.Elapsed -= CaptainSelectionExpired;
            foreach (var player in _sst.ServerInfo.CurrentPlayers)
            {
                player.Value.HasMadeSuccessfulSubRequest = false;
            }
            Log.Write("Resetting pickup status data.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Shows a public message including the names of current privileged users who may cancel the
        ///     pickup, and provides the syntax for the command that cancels the pickup.
        /// </summary>
        public async Task ShowPrivUserCanCancelMsg()
        {
            var superUsers =
                _userDb.GetSuperUsersOrHigherOnServer(_sst.ServerInfo.CurrentPlayers);
            await
                _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^3{0}^7 can completely cancel the pickup with^3 {1}{2} stop",
                        (string.IsNullOrEmpty(superUsers)
                            ? "A super-user or higher"
                            : superUsers.TrimEnd(',', ' ')),
                        CommandList.GameCommandPrefix, CommandList.CmdPickup));
        }

        /// <summary>
        ///     Shows a public message including the names of current privileged users who may reset the
        ///     pickup, and provides the syntax for the command that resets the pickup.
        /// </summary>
        public async Task ShowPrivUserCanResetMsg()
        {
            var superUsers =
                _userDb.GetSuperUsersOrHigherOnServer(_sst.ServerInfo.CurrentPlayers);
            await
                _sst.QlCommands.QlCmdSay(
                    string.Format("^3{0}^7 can reset the pickup with^3 {1}{2} reset",
                        (string.IsNullOrEmpty(superUsers)
                            ? "A super-user or higher"
                            : superUsers.TrimEnd(',', ' ')),
                        CommandList.GameCommandPrefix, CommandList.CmdPickup));
        }

        /// <summary>
        ///     Shows a public message including the names of current privileged users who may start the
        ///     pickup, and provides the syntax for the command that starts the pickup.
        /// </summary>
        public async Task ShowPrivUserCanStartMsg()
        {
            var superUsers =
                _userDb.GetSuperUsersOrHigherOnServer(_sst.ServerInfo.CurrentPlayers);
            await
                _sst.QlCommands.QlCmdSay(
                    string.Format("^3{0}^7 can start a new pickup with^3 {1}{2} start",
                        (string.IsNullOrEmpty(superUsers)
                            ? "A super-user or higher"
                            : superUsers.TrimEnd(',', ' ')),
                        CommandList.GameCommandPrefix, CommandList.CmdPickup));
        }

        /// <summary>
        ///     Starts the captain selection timer, giving all eligible players a certain amount of
        ///     time to issue the cap command to sign up as a captain for the upcoming pickup game.
        /// </summary>
        public async Task StartCaptainSelection()
        {
            _captainSelectionTimer.AutoReset = false;
            _captainSelectionTimer.Interval = CaptainSelectionTimeLimit;
            _captainSelectionTimer.Elapsed += CaptainSelectionExpired;
            _captainSelectionTimer.Start();
            HasCaptainSelectionStarted = true;
            await
                _sst.QlCommands.QlCmdSay(
                    "^5[PICKUP]^7 Captain selection has started. **^22^7** captains are needed!");
            await
                _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[PICKUP]^7 You have ^5{0}^7 seconds to type^2 {1}{2}^7 to become a captain!",
                        (CaptainSelectionTimeLimit / 1000), CommandList.GameCommandPrefix,
                        CommandList.CmdPickupCap));

            Log.Write(string.Format("Captain selection countdown started. Will last for {0} seconds",
                (CaptainSelectionTimeLimit / 1000)), _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Event handler that is called upon the expiration of the captain selection timer.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private async void CaptainSelectionExpired(object sender, ElapsedEventArgs e)
        {
            // Too many people might have removed, leaving an inadequate number of players from which to pick caps. Reset.
            if (AvailablePlayers.Count < 2)
            {
                await ResetDueToInadequatePlayerCount();
                return;
            }

            // We have zero captains -- no one bothered to !cap or it's a rare situation where both
            // captains disconnected before team selection.
            if (!_captains.RedCaptainExists && !_captains.BlueCaptainExists)
            {
                Log.Write("0 players volunteered to be captains. Randomly selecting both captains," +
                          " then proceeding to team selection.", _logClassType, _logPrefix);

                // Randomly pick the two captains and move to the proper teams so that team selection can begin.

                await
                    _sst.QlCommands.QlCmdSay(
                        "^5[PICKUP]^7 No one signed up to be captain! Randomly selecting 2 captains...");
                var rand = new Random();
                var captains = AvailablePlayers.OrderBy(x => rand.Next()).Take(2).ToArray();
                await _captains.SetCaptain(captains[0], Team.Red, false);
                await _captains.SetCaptain(captains[1], Team.Blue, false);
                await MoveCaptainsToTeams();
                await StartTeamSelection();
            }
            // We only have one captain
            else if ((!_captains.BlueCaptainExists && (_captains.RedCaptainExists)) ||
                     (!_captains.RedCaptainExists && (_captains.BlueCaptainExists)))
            {
                Log.Write(
                    "1 player volunteered to be a captain. Randomly selecting remaining captain, then " +
                    "proceeding to team selection.", _logClassType, _logPrefix);

                // We have the red captain. Randomly pick the blue captain and move both captains to
                // the proper teams so that team selection can begin.
                if (!_captains.BlueCaptainExists && _captains.RedCaptainExists)
                {
                    await
                        _sst.QlCommands.QlCmdSay(
                            "^5[PICKUP]^7 No ^5BLUE^7 captain found. Randomly selecting ^5BLUE^7 captain.");
                    var rand = new Random();
                    var captain = AvailablePlayers.OrderBy(x => rand.Next()).Take(1).ToArray();
                    await _captains.SetCaptain(captain[0], Team.Blue, false);
                    await MoveCaptainsToTeams();
                    await StartTeamSelection();
                }
                // We have the blue captain. Randomly pick the red captain and move both captains to
                // the proper teams so that team selection can begin.
                if (!_captains.RedCaptainExists && _captains.BlueCaptainExists)
                {
                    await
                        _sst.QlCommands.QlCmdSay(
                            "^5[PICKUP]^7 No ^1RED^7 captain found. Randomly selecting ^1RED^7 captain.");
                    var rand = new Random();
                    var captain = AvailablePlayers.OrderBy(x => rand.Next()).Take(1).ToArray();
                    await _captains.SetCaptain(captain[0], Team.Red, false);
                    await MoveCaptainsToTeams();
                    await StartTeamSelection();
                }
            }
            // We have both captains
            else if (_captains.RedCaptainExists && _captains.BlueCaptainExists)
            {
                Log.Write("2 players volunteered to be captains. Proceeding to team selection.",
                    _logClassType, _logPrefix);

                await MoveCaptainsToTeams();
                await StartTeamSelection();
            }
            HasCaptainSelectionStarted = false;
        }

        /// <summary>
        ///     Moves all of the players to spectator mode.
        /// </summary>
        private async Task ClearTeams()
        {
            foreach (var player in _sst.ServerInfo.CurrentPlayers.ToList().Where(player =>
                !player.Value.ShortName.Equals(_sst.AccountName,
                    StringComparison.InvariantCultureIgnoreCase)))
            {
                await _sst.QlCommands.CustCmdPutPlayer(player.Value.ShortName, Team.Spec);
            }
        }

        /// <summary>
        ///     Resets the pickup back to pre-game mode.
        /// </summary>
        /// <remarks>
        ///     This is in response to the 'reset' argument passed to <see cref="PickupCmd" />.
        /// </remarks>
        private async Task DoResetPickup()
        {
            await _sst.QlCommands.QlCmdSay("^3[PICKUP]^7 Attempting to reset pickup...");
            ResetPickupStatus();
            await StartPickupPreGame();
        }

        /// <summary>
        ///     Event handler that is called upon the end of the pre-defined intermission period.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        /// <remarks>
        ///     This is a method that is used to automatically reset the pickup after a pre-determined time
        ///     after the intermission. It is used to reset and re-setup the teams after the previous game ends
        ///     (typically after end-of-game map voting).
        /// </remarks>
        private void EndGameResetExpired(object sender, ElapsedEventArgs e)
        {
            Log.Write("Intermission. Re-establishing pickup.", _logClassType, _logPrefix);
            // ReSharper disable once UnusedVariable
            // Synchronous
            var s = StartPickupPreGame();
        }

        /// <summary>
        ///     Determines whether the command was sent from the owner of
        ///     the bot via IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was sent from IRC and from
        ///     an the IRC owner.
        /// </returns>
        private bool IsIrcOwner(CmdArgs c)
        {
            if (!c.FromIrc) return false;
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();
            return
                (c.FromUser.Equals(cfg.IrcOptions.ircAdminNickname,
                    StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Determines whether to perform the no-show evaluation for the specified player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="wasActive">
        ///     if set to <c>true</c> indicates that the departing player was
        ///     active (i.e. was on the red or the blue team, thus not a spectator).
        /// </param>
        /// <returns><c>true</c> if the evaluation is to be performed, otherwise <c>false</c>.</returns>
        private bool IsNoShowEvalApplicable(string player, bool wasActive)
        {
            // Game is in intermission (timelimit hit to end of end-game map voting); do not eval no shows
            if (IsIntermission) return false;
            // Player signed up as a captain, but left during captain selection process; evaluation is applicable.
            if (HasCaptainSelectionStarted &&
                (Captains.RedCaptain.Equals(player) || Captains.BlueCaptain.Equals(player)))
                return true;
            // Player signed up as a captain, but left during team selection process; evaluation is applicable.
            if (HasTeamSelectionStarted &&
                (Captains.RedCaptain.Equals(player) || Captains.BlueCaptain.Equals(player)))
                return true;
            // Player signed up to play, but leaves during the team picking process, evaluation is applicable.
            if (HasTeamSelectionStarted && AvailablePlayers.Contains(player)) return true;
            // Player was picked for a team and moved to team, but game has not officially started yet; evaluation is applicable.
            if (IsPickupPreGame && wasActive) return true;
            // Game is in progress, player was on a team (wasActive) and leaves prematurely, evaluation is applicable.
            if (IsPickupInProgress && wasActive) return true;

            return false;
        }

        /// <summary>
        ///     Moves the captains to their proper teams so that team selection will be possible.
        /// </summary>
        private async Task MoveCaptainsToTeams()
        {
            await _sst.QlCommands.CustCmdPutPlayerDelayed(_captains.RedCaptain, Team.Red, 2);
            await _sst.QlCommands.CustCmdPutPlayerDelayed(_captains.BlueCaptain, Team.Blue, 2);
            // Captains are now active pickup players
            AddActivePickupPlayer(_captains.RedCaptain);
            AddActivePickupPlayer(_captains.BlueCaptain);
            // Let them know how the procedure is to occur
            string[] capNotifyMsgs =
            {
                "^1You are now a captain. When it's your turn type ^2!pick <name>^7 to pick a player for your team.",
                "^7If you must leave early be sure to !sub <name> to avoid a no-show. Abusing this will get you banned."
            };
            foreach (var msg in capNotifyMsgs)
            {
                await _sst.QlCommands.QlCmdTell(msg, _captains.RedCaptain);
                await _sst.QlCommands.QlCmdTell(msg, _captains.BlueCaptain);
            }

            Log.Write("Captains moved to teams and notified of picking procedure.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Resets the pickup due to inadequate eligible player count.
        /// </summary>
        private async Task ResetDueToInadequatePlayerCount()
        {
            Log.Write("Resetting pickup due to inadequate player count.",
                _logClassType, _logPrefix);

            await
                _sst.QlCommands.QlCmdSay(
                    "^1[ERROR]^3 There are no longer enough eligible players. Resetting...");
            await ShowPrivUserCanCancelMsg();
            await DoResetPickup();
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        private async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        private async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Prepares the teams so that captains and players can be picked.
        /// </summary>
        private async Task SetupTeams()
        {
            Log.Write("Preparing teams for captain and player picking",
                _logClassType, _logPrefix);

            IsBotSettingUpTeams = true;
            // Lock both teams.
            await _sst.QlCommands.SendToQlAsync("lock", false);
            // Teams might be full, i.e. if a game just ended, so clear first
            await ClearTeams();
            // Force the bot to join a team, since votes can't be called in spectator mode.
            var botId = _sst.ServerInfo.CurrentPlayers[_sst.AccountName].Id;
            await _sst.QlCommands.SendToQlAsync(string.Format("put {0} r", botId), false);
            // Callvote the teamsize based on the specified teamsize in the pickup module options.
            await
                _sst.QlCommands.SendToQlAsync(
                    string.Format("cv teamsize {0}", _sst.Mod.Pickup.Teamsize), false);
            // Force the bot back to spectators.
            await _sst.QlCommands.SendToQlAsync(string.Format("put {0} s", botId), false);
            IsBotSettingUpTeams = false;
        }

        /// <summary>
        ///     Shows the in-progress error.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task ShowProgressInError(CmdArgs c)
        {
            StatusMessage =
                "^1[ERROR]^3 Pickup games can only be started, stopped or reset from warm-up mode!";
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Starts the pickup reset timer on game's end.
        /// </summary>
        private void StartEndGameResetTimer()
        {
            Log.Write(string.Format(
                "Starting end-game pickup reset timer. Will try to start a new pickup in {0} seconds",
                (PickupResetOnEndGameLimit / 1000)), _logClassType, _logPrefix);

            _endGameResetTimer.AutoReset = false;
            _endGameResetTimer.Interval = PickupResetOnEndGameLimit;
            _endGameResetTimer.Elapsed += EndGameResetExpired;
            _endGameResetTimer.Start();
        }

        /// <summary>
        ///     Starts the pickup pre game mode that sets up the server for an upcoming pickup game.
        /// </summary>
        private async Task StartPickupPreGame()
        {
            // Housekeeping...
            ResetPickupStatus();

            // Lock down the server
            await SetupTeams();
            await _sst.QlCommands.QlCmdSay(string.Format(
                "^5[PICKUP]^7 Pickup mode is enabled. To be eligible to play, type: ^2{0}{1}",
                CommandList.GameCommandPrefix, CommandList.CmdPickupAdd));

            await _sst.QlCommands.QlCmdSay(string.Format(
                "^5[PICKUP]^7 At least ^2{0}^7 players needed before teams and captains are picked.",
                (_sst.Mod.Pickup.Teamsize * 2)));

            IsPickupPreGame = true;
            Log.Write("Pickup is in pre-game.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Starts the process of team selection.
        /// </summary>
        private async Task StartTeamSelection()
        {
            // We should have both captains (+2) selected at this point.
            // However, players might have disconnected before the captain selection timer expired, so
            // make sure we have enough players one last time, and reset if we don't.
            if ((AvailablePlayers.Count + 2) < (_sst.Mod.Pickup.Teamsize * 2))
            {
                await ResetDueToInadequatePlayerCount();
                return;
            }

            HasTeamSelectionStarted = true;
            Log.Write("Team selection started.", _logClassType, _logPrefix);

            // Randomly decide which of the two captains receives the first pick
            var rand = new Random();
            var n = rand.Next(0, 2);
            if (n == 0)
            {
                await _captains.SetPickingTeam(Team.Red);
            }
            else
            {
                await _captains.SetPickingTeam(Team.Blue);
            }
        }

        /// <summary>
        ///     Stops (cancels) the pickup and unlocks the teams so that anyone can join.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task StopPickup(CmdArgs c)
        {
            StatusMessage = string.Format(
                "^3[PICKUP]^7 Canceling pickup. Teams unlocked so anyone can join. ^2{0}{1} start^7 to start another.",
                CommandList.GameCommandPrefix, CommandList.CmdPickup);
            await SendServerSay(c, StatusMessage);

            ResetPickupStatus();
            await _sst.QlCommands.SendToQlAsync("unlock", false);
            Log.Write("Pickup has been stopped. Teams unlocked.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Unlocks a team due to a player's premature mid-game departure.
        /// </summary>
        /// <param name="player">The departing player.</param>
        /// <param name="team">The team to unlock.</param>
        private async Task UnlockTeamDueToNoShow(string player, Team team)
        {
            // Ignore when bot moves back to spec during initial server lockdown setup.
            if (IsBotSettingUpTeams) return;

            if (team == Team.Blue)
            {
                MissingBluePlayerCount++;
            }
            else if (team == Team.Red)
            {
                MissingRedPlayerCount++;
            }
            await
                _sst.QlCommands.SendToQlAsync(
                    ((team == Team.Blue) ? "unlock blue" : "unlock red"), false);
            await
                _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[PICKUP]^7 Unlocking {0}^7 because ^3{1}^7 no-showed the game.",
                        ((team == Team.Blue) ? "^5BLUE" : "^1RED"), player));

            Log.Write(string.Format("Unlocked {0} team due to player {1}'s premature departue.",
                ((team == Team.Blue) ? "^5BLUE" : "^1RED"), player), _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Updates the departing player's no-show/sub information in the pickup database.
        /// </summary>
        /// <param name="player">The departing player.</param>
        private async Task UpdateNoShowAndSubDatabase(string player)
        {
            if (!Helpers.KeyExists(player, _sst.ServerInfo.CurrentPlayers)) return;
            var pickupDb = new DbPickups();
            if (_sst.ServerInfo.CurrentPlayers[player].HasMadeSuccessfulSubRequest)
            {
                // Check whether non-exempt user (lower than SuperUser) has exceeded the permissible number of sub requests used
                if (_userDb.GetUserLevel(player) >= UserLevel.SuperUser) return;
                var subsUsed = pickupDb.GetUserSubsUsedCount(player);
                if (subsUsed > _sst.Mod.Pickup.MaxSubsPerPlayer)
                {
                    Log.Write(string.Format("Non-exempt player {0} has used too many subs. Punishing.",
                        player), _logClassType, _logPrefix);

                    // Add a timeban if so
                    var expirationDate =
                        ExpirationDateGenerator.GenerateExpirationDate(
                            _sst.Mod.Pickup.ExcessiveSubUseBanTime,
                            _sst.Mod.Pickup.ExcessiveSubUseBanTimeScale);
                    _banDb.AddUserToDb(player, "pickupMod", DateTime.Now, expirationDate,
                        BanType.AddedByPickupSubs);

                    await
                        _sst.QlCommands.QlCmdSay(
                            string.Format(
                                "^5[PICKUP]^3 {0}^7 has requested too many subs, banning until ^1{1}",
                                player,
                                expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));

                    // UI: reflect changes
                    _sst.UserInterface.RefreshCurrentBansDataSource();
                }
            }
            else
            {
                // Sub request was not successfully made, count as no-show
                pickupDb.IncrementUserNoShowCount(player);

                // Ban if the limit is exceeded and the user is not exempt (superusers & higher are exempt)
                if (_userDb.GetUserLevel(player) >= UserLevel.SuperUser) return;
                var noShows = pickupDb.GetUserNoShowCount(player);
                if (noShows > _sst.Mod.Pickup.MaxNoShowsPerPlayer)
                {
                    Log.Write(string.Format("Non-exempt player {0} has no-showed too many games. Punishing.",
                        player), _logClassType, _logPrefix);

                    var expirationDate =
                        ExpirationDateGenerator.GenerateExpirationDate(
                            _sst.Mod.Pickup.ExcessiveNoShowBanTime,
                            _sst.Mod.Pickup.ExcessiveNoShowBanTimeScale);
                    _banDb.AddUserToDb(player, "pickupMod", DateTime.Now, expirationDate,
                        BanType.AddedByPickupNoShows);

                    await
                        _sst.QlCommands.QlCmdSay(
                            string.Format(
                                "^5[PICKUP]^3 {0}^7 has no-showed too many games, banning until ^1{1}",
                                player,
                                expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));

                    // UI: reflect changes
                    _sst.UserInterface.RefreshCurrentBansDataSource();
                }
            }
        }
    }
}