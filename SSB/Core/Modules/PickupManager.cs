﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SSB.Core.Commands.None;
using SSB.Database;
using SSB.Enum;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Modules
{
    /// <summary>
    ///     Class responsible for managing pickup games.
    /// </summary>
    public class PickupManager
    {
        private const double CaptainSelectionTimeLimit = 60000;
        private readonly PickupCaptains _captains;
        private readonly Timer _captainSelectionTimer;
        private readonly PickupPlayers _players;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _userDb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupManager" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PickupManager(SynServerBot ssb)
        {
            _ssb = ssb;
            _captains = new PickupCaptains(_ssb, this);
            _players = new PickupPlayers(_ssb, this);
            _userDb = new DbUsers();
            EligiblePlayers = new List<string>();
            InProgressSubCandidates = new List<string>();
            Subs = new StringBuilder();
            NoShows = new StringBuilder();
            _captainSelectionTimer = new Timer();
        }

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
        ///     Gets or sets the eligible players.
        /// </summary>
        /// <value>
        ///     The eligible players.
        /// </value>
        public List<string> EligiblePlayers { get; set; }

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
        ///     Gets or sets the players who have added while a game is in progress.
        /// </summary>
        /// <value>
        ///     The list of players who are eligible to be subbed in after a pickup is already
        ///     in progress.
        /// </value>
        public List<string> InProgressSubCandidates { get; set; }

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
                return _ssb.ServerInfo.CurrentServerGameState == QlGameStates.Countdown ||
                       _ssb.ServerInfo.CurrentServerGameState == QlGameStates.InProgress;
            }
        }

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
        ///     Gets or sets the substitite players for purposes of record keeping.
        /// </summary>
        /// <value>
        ///     The substitute players for purposes of record keeping.
        /// </value>
        public StringBuilder Subs { get; set; }

        /// <summary>
        /// Creates a pickup info object for the current pickup game.
        /// </summary>
        /// <returns>A <see cref="PickupInfo"/> object representing the current pickup game.</returns>
        public PickupInfo CreatePickupInfo()
        {
            var pickupInfo = new PickupInfo();
            var redTeam = new StringBuilder();
            var blueTeam = new StringBuilder();

            foreach (var player in _ssb.ServerInfo.GetTeam(Team.Red))
            {
                redTeam.Append(string.Format("{0}, ", player.ShortName));
            }

            foreach (var player in _ssb.ServerInfo.GetTeam(Team.Blue))
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
        public async Task DisplayEligiblePlayers()
        {
            await _ssb.QlCommands.QlCmdSay(string.Format("^7Eligible players: {0}",
                ((EligiblePlayers.Count != 0)
                    ? "^3" + string.Join(",", EligiblePlayers)
                    : "^1NO eligible players!")));
        }

        /// <summary>
        ///     Evauates a departing pickup user's noshow or sub status and punishes if necessary.
        /// </summary>
        /// <param name="player">The departing player.</param>
        /// <param name="wasActive">
        ///     if set to <c>true</c> indicates that the departing player was
        ///     active (i.e. was on the red or the blue team, thus not a spectator).
        /// </param>
        public async Task EvalOutgoingPlayer(string player, bool wasActive)
        {
            if (!IsNoShowEvalApplicable(player, wasActive)) return;
            if (!Tools.KeyExists(player, _ssb.ServerInfo.CurrentPlayers)) return;
            var pickupDb = new DbPickups();
            if (_ssb.ServerInfo.CurrentPlayers[player].HasMadeSuccessfulSubRequest)
            {
                // Check whether non-exempt user (lower than SuperUser) has exceeded the permissible number of sub requests used
                if (_userDb.GetUserLevel(player) >= UserLevel.SuperUser) return;
                var subsUsed = pickupDb.GetUserSubsUsedCount(player);
                if (subsUsed > _ssb.Mod.Pickup.MaxSubsPerPlayer)
                {
                    // Add a timeban if so
                    var banDb = new DbBans();
                    var expirationDate =
                        ExpirationDateGenerator.GenerateExpirationDate(
                            _ssb.Mod.Pickup.ExcessiveSubUseBanTime,
                            _ssb.Mod.Pickup.ExcessiveSubUseBanTimeScale);
                    banDb.AddUserToDb(player, "bot-Internal", DateTime.Now, expirationDate,
                        BanType.AddedByPickupSubs);
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format(
                                "^5[PICKUP]^3 {0}^7 has requested too many subs, banning until ^1{1}", player,
                                expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));
                }
            }
            else
            {
                // Sub request was not successfully made, count as no-show
                pickupDb.IncrementUserNoShowCount(player);

                // Ban if the limit is exceeded and the user is not exempt (superusers & higher are exempt)
                if (_userDb.GetUserLevel(player) >= UserLevel.SuperUser) return;
                var noShows = pickupDb.GetUserNoShowCount(player);
                if (noShows > _ssb.Mod.Pickup.MaxNoShowsPerPlayer)
                {
                    var banDb = new DbBans();
                    var expirationDate =
                        ExpirationDateGenerator.GenerateExpirationDate(
                            _ssb.Mod.Pickup.ExcessiveNoShowBanTime,
                            _ssb.Mod.Pickup.ExcessiveNoShowBanTimeScale);
                    banDb.AddUserToDb(player, "bot-Internal", DateTime.Now, expirationDate,
                        BanType.AddedByPickupNoShows);
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format(
                                "^5[PICKUP]^3 {0}^7 has no-showed too many games, banning until ^1{1}", player,
                                expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));
                }
            }
        }

        /// <summary>
        ///     Evaluates whether the current pickup can be reset, and if so, resets it.
        /// </summary>
        /// <returns></returns>
        public async Task EvalPickupReset()
        {
            if (IsQlGameInProgress)
            {
                await ShowProgressInError();
                return;
            }
            await DoResetPickup();
        }

        /// <summary>
        ///     Evaluates whether the server can be put into pickup pre-game mode when the pickup start command is issued,
        ///     and if it can be, then puts the servers into the pickup pre-game mode.
        /// </summary>
        public async Task EvalPickupStart()
        {
            if (!_ssb.ServerInfo.IsATeamGame())
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Pickup can only be started on server running a team-based game.");
                return;
            }
            if (IsQlGameInProgress)
            {
                await ShowProgressInError();
                return;
            }
            if (IsPickupPreGame || IsPickupInProgress)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Another pickup game is already pending!");
                return;
            }
            await StartPickupPreGame();
        }

        /// <summary>
        ///     Evaluates whether the current pickup can be stopped, and if so, stops it.
        /// </summary>
        public async Task EvalPickupStop()
        {
            if (IsQlGameInProgress)
            {
                await ShowProgressInError();
                return;
            }
            await StopPickup();
        }

        /// <summary>
        /// Handles the pickup end (when the QL gamestate changes to WARM_UP)
        /// </summary>
        public void HandlePickupEnd()
        {
            // Pickup needs to have previously been in progress
            if (!IsPickupInProgress) return;
            Debug.WriteLine("QL (WARM_UP): Pickup game has now officially ended!");
            // Game officially ended (QL: WARM_UP gamestate), update the pickup DB table to
            // incldue any changes that occurred between the game start and the game end
            var pickupDb = new DbPickups();
            pickupDb.UpdateMostRecentPickupGame(CreatePickupInfo());
            // Set the end time
            pickupDb.UpdatePickupEndTime(DateTime.Now);
            // And get ready for the next pickup game
            // Synchronous
            // ReSharper disable once UnusedVariable
            Task s = StartPickupPreGame();
        }

        /// <summary>
        /// Handles the pickup launch (when the QL gamestate changes to IN_PROGRESS)
        /// </summary>
        public void HandlePickupLaunch()
        {
            // Pickup pre-game extends from !pickup start to the game actually launching
            // So do nothing if for some reason we are not at this point
            if (!IsPickupPreGame) return;
            Debug.WriteLine("QL (IN_PROGRESS): Pickup game has now officially started!");
            // When game officially starts (IN_PROGRESS) update the pickup DB table to include
            // actual start time, any team member changes, subs &/or no-shows that occurred after the teams
            // were already full.
            var pickupDb = new DbPickups();
            pickupDb.UpdateMostRecentPickupGame(CreatePickupInfo());
            // We are now in progress
            IsPickupPreGame = false;
            IsPickupInProgress = true;
            // Move any remaining eligible players who were not picked to the list of eligible substitutes and clear.
            foreach (var player in EligiblePlayers.Where(player => !InProgressSubCandidates.Contains(player)))
            {
                InProgressSubCandidates.Add(player);
            }
            EligiblePlayers.Clear();
        }

        /// <summary>
        ///     Notifies the new player that he has been picked.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team for which the player was picked.</param>
        public async Task NotifyNewPlayer(string player, Team team)
        {
            await
                _ssb.QlCommands.QlCmdTell(player,
                    string.Format(
                        "^7You've been picked for {0}^7. If you must leave early: ^3!sub <spectator>^7 to avoid a no-show.",
                        ((team == Team.Red) ? "^1RED" : "^5BLUE")));
        }

        /// <summary>
        /// Notifies the connecting user on how to sign up for next or current pickup game.
        /// </summary>
        /// <param name="user">The user.</param>
        public async Task NotifyConnectingUser(string user)
        {
            if (IsPickupPreGame)
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^7This server is in pickup game mode. Type ^5{0}{1}^7 to sign up for the next game.",
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd), user);
            }
            else if (IsPickupInProgress)
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^7A pickup game is in progress. Type ^5{0}{1}^7 to sign up as a susbstitute player.",
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd), user);
            }
        }

        /// <summary>
        ///     Processes the player and/or captain substitution.
        /// </summary>
        /// <param name="fromPlayer">The player/captain sending the substitution request (the outgoing player/captain).</param>
        /// <param name="playerToSub">The player/captain to sub in.</param>
        public async Task ProcessSub(string fromPlayer, string playerToSub)
        {
            if (!IsPickupInProgress || !IsPickupPreGame)
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You may not request a sub at this time.");
                return;
            }

            if (!Tools.KeyExists(playerToSub, _ssb.ServerInfo.CurrentPlayers))
            {
                await
                    _ssb.QlCommands.QlCmdTell(string.Format("^1[ERROR]^3 {0} is not currently on the server!",
                        playerToSub), fromPlayer);
                return;
            }
            if (IsPickupPreGame && !EligiblePlayers.Contains(playerToSub))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^1[ERROR]^3 {0} has not signed up with: ^7{1}{2}^3 yet!", playerToSub,
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd));
                return;
            }
            if (IsPickupInProgress && !InProgressSubCandidates.Contains(playerToSub))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^1[ERROR]^3 {0} has not signed up with: ^7{1}{2}^3 yet!", playerToSub,
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd));
                return;
            }
            if (!_ssb.ServerInfo.IsActivePlayer(fromPlayer))
            {
                await _ssb.QlCommands.QlCmdTell("^1[ERROR]^3 You may not request a sub.", fromPlayer);
                return;
            }
            var team = _ssb.ServerInfo.CurrentPlayers[fromPlayer].Team;
            if (_captains.RedCaptain.Equals(fromPlayer) || _captains.BlueCaptain.Equals(fromPlayer))
            {
                await _captains.DoCaptainSub(fromPlayer, team, playerToSub);
            }
            else
            {
                await _players.DoPlayerSub(fromPlayer, team, playerToSub);
            }
        }

        /// <summary>
        ///     Removes the player from the list of eligible players and/or eligible sub candidates.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        public void RemoveEligibility(string player)
        {
            if (EligiblePlayers.Remove(player))
            {
                Debug.WriteLine(string.Format("Removed {0} from eligible players",
                    player));
            }
            if (InProgressSubCandidates.Remove(player))
            {
                Debug.WriteLine(string.Format("Removed {0} from the in-progress sub candidates",
                    player));
            }
        }

        /// <summary>
        ///     Shows a public message including the names of current privileged users who may cancel the
        ///     pickup, and provides the syntax for the command that cancels the pickup.
        /// </summary>
        public async Task ShowPrivUserCanCancelMsg()
        {
            var superUsers = _userDb.GetSuperUsersOrHigherOnServer(_ssb.ServerInfo.CurrentPlayers);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^3{0} can completely cancel the pickup with^7 {1}{2} stop",
                        (string.IsNullOrEmpty(superUsers)
                            ? "A super-user or higher."
                            : superUsers.TrimEnd(',', ' ')),
                        CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }

        /// <summary>
        ///     Shows a public message including the names of current privileged users who may reset the
        ///     pickup, and provides the syntax for the command that resets the pickup.
        /// </summary>
        public async Task ShowPrivUserCanResetMsg()
        {
            var superUsers = _userDb.GetSuperUsersOrHigherOnServer(_ssb.ServerInfo.CurrentPlayers);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0} can reset the pickup with^7 {1}{2} reset",
                (string.IsNullOrEmpty(superUsers) ? "A super-user or higher." : superUsers.TrimEnd(',', ' ')),
                CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }

        /// <summary>
        ///     Shows a public message including the names of current privileged users who may start the
        ///     pickup, and provides the syntax for the command that starts the pickup.
        /// </summary>
        public async Task ShowPrivUserCanStartMsg()
        {
            var superUsers = _userDb.GetSuperUsersOrHigherOnServer(_ssb.ServerInfo.CurrentPlayers);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0} can start a new pickup with^7 {1}{2} start",
                (string.IsNullOrEmpty(superUsers) ? "A super-user or higher." : superUsers.TrimEnd(',', ' ')),
                CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
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
                _ssb.QlCommands.QlCmdSay(
                    "^3[PICKUP]^7 Captain selection has started. ^32^7 captains are needed!");
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^3[PICKUP]^7 You have {0} seconds to type^2 {1}{2}^7 to become a captain!",
                        (CaptainSelectionTimeLimit / 1000), CommandProcessor.BotCommandPrefix,
                        CommandProcessor.CmdPickupCap));
        }

        /// <summary>
        ///     Event handler that is called upon the expiration of the captain selection timer.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private async void CaptainSelectionExpired(object sender, ElapsedEventArgs e)
        {
            // Players might have disconnected before the captain selection timer expired, so
            // make sure we have enough players one last time, and reset if we don't.
            if (EligiblePlayers.Count < (_ssb.Mod.Pickup.Teamsize * 2))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 There are no longer enough eligible players. Resetting...");
                await ShowPrivUserCanCancelMsg();
                await DoResetPickup();
                return;
            }
            // We have zero captains -- no one bothered to !cap or it's a rare situation where both
            // captains disconnected before team selection.
            if (!_captains.RedCaptainExists && !_captains.BlueCaptainExists)
            {
                // Randomly pick the two captains and move to the proper teams so that team selection can begin.
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^3[PICKUP]^7 No one signed up to be captain! Randomly selecting 2 captains...");
                var rand = new Random();
                var captains = EligiblePlayers.OrderBy(x => rand.Next()).Take(2).ToArray();
                await _captains.SetCaptain(captains[0], Team.Red);
                await _captains.SetCaptain(captains[1], Team.Blue);
                await MoveCaptainsToTeams();
                await StartTeamSelection();
            }
            // We only have one captain
            else if ((!_captains.BlueCaptainExists && (_captains.RedCaptainExists)) ||
                     (!_captains.RedCaptainExists && (_captains.BlueCaptainExists)))
            {
                // We have the red captain. Randomly pick the blue captain and move both captains to
                // the proper teams so that team selection can begin.
                if (!_captains.BlueCaptainExists && _captains.RedCaptainExists)
                {
                    await
                        _ssb.QlCommands.QlCmdSay(
                            "^3[PICKUP]^7 No ^5BLUE^7 captain found. Randomly selecting ^5BLUE^7 captain.");
                    var rand = new Random();
                    var captain = EligiblePlayers.OrderBy(x => rand.Next()).Take(1).ToArray();
                    await _captains.SetCaptain(captain[0], Team.Blue);
                    await MoveCaptainsToTeams();
                    await StartTeamSelection();
                }
                // We have the blue captain. Randomly pick the red captain and move both captains to
                // the proper teams so that team selection can begin.
                if (!_captains.RedCaptainExists && _captains.BlueCaptainExists)
                {
                    await
                        _ssb.QlCommands.QlCmdSay(
                            "^3[PICKUP]^7 No ^1RED^7 captain found. Randomly selecting ^1RED^7 captain.");
                    var rand = new Random();
                    var captain = EligiblePlayers.OrderBy(x => rand.Next()).Take(1).ToArray();
                    await _captains.SetCaptain(captain[0], Team.Red);
                    await MoveCaptainsToTeams();
                    await StartTeamSelection();
                }
            }
            HasCaptainSelectionStarted = false;
        }

        /// <summary>
        ///     Moves all of the players except for the bot to spectator mode.
        /// </summary>
        private async Task ClearTeams()
        {
            foreach (var player in _ssb.ServerInfo.CurrentPlayers.Where(player =>
                !player.Value.ShortName.Equals(_ssb.BotName, StringComparison.InvariantCultureIgnoreCase)))
            {
                await _ssb.QlCommands.CustCmdPutPlayer(player.Value.ShortName, Team.Spec);
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
            await _ssb.QlCommands.QlCmdSay("^3[PICKUP]^7 Attempting to reset pickup...");
            ResetPickupStatus();
            await StartPickupPreGame();
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
            // Player signed up to play, but leaves during the team picking process, evaluation is applicable.
            if (HasTeamSelectionStarted && EligiblePlayers.Contains(player)) return true;
            // Game is in progress, player was on a team (wasActive) and leaves prematurely, evaluation is applicable.
            if (IsPickupInProgress && wasActive) return true;

            return false;
        }

        /// <summary>
        ///     Moves the captains to their proper teams so that team selection will be possible.
        /// </summary>
        private async Task MoveCaptainsToTeams()
        {
            await _ssb.QlCommands.CustCmdPutPlayer(_captains.RedCaptain, Team.Red);
            await _ssb.QlCommands.CustCmdPutPlayer(_captains.BlueCaptain, Team.Blue);
            string[] capNotifyMsgs =
            {
                "^1You are now a captain. When it's your turn type ^2!pick <name>^7 to pick a player for your team.",
                "^7If you must leave early be sure to !sub <name> to avoid a no-show. Abusing this will get you banned."
            };
            foreach (var msg in capNotifyMsgs)
            {
                await _ssb.QlCommands.QlCmdTell(_captains.RedCaptain, msg);
                await _ssb.QlCommands.QlCmdTell(_captains.BlueCaptain, msg);
            }
        }

        /// <summary>
        ///     Resets the pickup status. This is the overall housekeeping method.
        /// </summary>
        private void ResetPickupStatus()
        {
            EligiblePlayers.Clear();
            InProgressSubCandidates.Clear();
            Subs.Clear();
            NoShows.Clear();
            HasCaptainSelectionStarted = false;
            HasTeamSelectionStarted = false;
            IsPickupPreGame = false;
            IsPickupInProgress = false;
            _captains.IsBlueTurnToPick = false;
            _captains.IsRedTurnToPick = false;
            _captains.RedCaptain = string.Empty;
            _captains.BlueCaptain = string.Empty;
            foreach (var player in _ssb.ServerInfo.CurrentPlayers)
            {
                player.Value.HasMadeSuccessfulSubRequest = false;
            }
        }

        /// <summary>
        ///     Prepares the teams so that captains and players can be picked.
        /// </summary>
        private async Task SetupTeams()
        {
            // Unlock, in case we're locked already
            _ssb.QlCommands.SendToQl("unlock", false);
            // Teams might be full, i.e. if a game just ended, so clear first
            await ClearTeams();
            // Force the bot to join a random team, since votes can't be called in spectator mode.
            _ssb.QlCommands.SendToQl("team a", false);
            // Lock both teams.
            _ssb.QlCommands.SendToQl("lock", false);
            // Clear teams
            await ClearTeams();
            // Callvote the teamsize based on the specified teamsize in the pickup module options.
            _ssb.QlCommands.SendToQl(string.Format("cv teamsize {0}", _ssb.Mod.Pickup.Teamsize), false);
            // Force the bot back to spectators.
            _ssb.QlCommands.SendToQl("team s", false);
        }

        /// <summary>
        ///     Shows the in-progress error.
        /// </summary>
        private async Task ShowProgressInError()
        {
            await
                _ssb.QlCommands.QlCmdSay(
                    "^1[ERROR]^3 Pickup games can only be started, stopped or reset from warm-up mode!");
        }

        /// <summary>
        ///     Starts the pickup pre game mode that sets up the server for an upcoming pickup game.
        /// </summary>
        private async Task StartPickupPreGame()
        {
            // Housekeeping... Reset, in case this method is called from !pickup start
            ResetPickupStatus();
            IsPickupPreGame = true;

            // Lock down the server
            await SetupTeams();
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[PICKUP]^7 Pickup mode is enabled. To be eligible to play, type: ^2{0}{1}",
                        CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd));
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[PICKUP]^7 At least ^2{0}^7 players needed before team & captains are picked.",
                        (_ssb.Mod.Pickup.Teamsize * 2)));
        }

        /// <summary>
        ///     Starts the process of team selection.
        /// </summary>
        private async Task StartTeamSelection()
        {
            HasTeamSelectionStarted = true;
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
        ///     Stops the pickup and unlocks the teams so that anyone can join.
        /// </summary>
        private async Task StopPickup()
        {
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[PICKUP]^7 Stopping pickup. Teams will be free for anyone to join. ^2{0}{1} start^7 to start another.",
                        CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
            ResetPickupStatus();
            await ClearTeams();
            _ssb.QlCommands.SendToQl("unlock", false);
        }
    }
}