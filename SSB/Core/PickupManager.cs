using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using SSB.Core.Commands.None;
using SSB.Database;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    /// Class responsible for managing pickup games.
    /// </summary>
    public class PickupManager
    {
        private const double CaptainSelectionTimeLimit = 60000;
        private readonly Timer _captainSelectionTimer;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _userDb;
        private bool _isBlueTurnToPick;
        private bool _isRedTurnToPick;
        //private readonly PickupUsers _pickupUserDb;

        /// <summary>
        /// Initializes a new instance of the <see cref="PickupManager"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PickupManager(SynServerBot ssb)
        {
            _ssb = ssb;
            _userDb = new DbUsers();
            // _pickupUserDb = new DbPickupUsers();
            EligiblePlayers = new List<string>();
            _captainSelectionTimer = new Timer();
        }

        /// <summary>
        /// Gets or sets the blue captain.
        /// </summary>
        /// <value>
        /// The blue captain.
        /// </value>
        public string BlueCaptain { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a blue captain.
        /// </summary>
        /// <value>
        /// <c>true</c> if there is a blue captain; otherwise, <c>false</c>.
        /// </value>
        public bool BlueCaptainExists
        {
            get { return !string.IsNullOrEmpty(BlueCaptain); }
        }

        /// <summary>
        /// Gets or sets the eligible players.
        /// </summary>
        /// <value>
        /// The eligible players.
        /// </value>
        public List<string> EligiblePlayers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the captain selection process has started.
        /// </summary>
        /// <value>
        /// <c>true</c> if the captain selection process started; otherwise, <c>false</c>.
        /// </value>
        public bool HasCaptainSelectionStarted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the team selection process has started.
        /// </summary>
        /// <value>
        /// <c>true</c> if the team selection process started; otherwise, <c>false</c>.
        /// </value>
        public bool HasTeamSelectionStarted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a pickup in progress.
        /// </summary>
        /// <value>
        /// <c>true</c> if a pickup in progress; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This covers the time period from when the pickup game launches until
        /// it is over.
        /// </remarks>
        public bool IsPickupInProgress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the server is in pickup pre-game mode.
        /// </summary>
        /// <value>
        /// <c>true</c> if the server is in pickup pre-game mode; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This covers the time period from the issuance of the pickup start command up until
        /// when the pickup game actually launches.
        /// </remarks>
        public bool IsPickupPreGame { get; set; }

        /// <summary>
        /// Gets or sets the red captain.
        /// </summary>
        /// <value>
        /// The red captain.
        /// </value>
        public string RedCaptain { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a red captain.
        /// </summary>
        /// <value>
        /// <c>true</c> if there is a red captain; otherwise, <c>false</c>.
        /// </value>
        public bool RedCaptainExists
        {
            get { return !string.IsNullOrEmpty(RedCaptain); }
        }

        /// <summary>
        /// Evaluates whether the current pickup can be reset, and if so, resets it.
        /// </summary>
        /// <returns></returns>
        public async Task EvalPickupReset()
        {
            if (IsQlGameInProgress())
            {
                await ShowProgressInError();
                return;
            }
            await ResetPickup();
        }

        /// <summary>
        /// Evaluates whether the server can be put into pickup pre-game mode when the pickup start command is issued,
        /// and if it can be, then puts the servers into the pickup pre-game mode.
        /// </summary>
        public async Task EvalPickupStart()
        {
            if (!_ssb.ServerInfo.IsATeamGame())
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Pickup can only be started on server running a team-based game.");
                return;
            }
            if (IsQlGameInProgress())
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
        /// Evaluates whether the current pickup can be stopped, and if so, stops it.
        /// </summary>
        public async Task EvalPickupStop()
        {
            if (IsQlGameInProgress())
            {
                await ShowProgressInError();
                return;
            }
            await StopPickup();
        }

        /// <summary>
        /// Evaluates the addition of a captain to see if it is possible to add the captain, and adds if so.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        /// This is called in response to input received from <see cref="PickupCapCmd"/>.
        /// </remarks>
        public async Task ProcessAddCaptain(string player)
        {
            if (IsQlGameInProgress())
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot become a pickup captain because a game is in progress.");
                return;
            }
            if (!HasCaptainSelectionStarted)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot become a pickup captain because captain selection hasn't started yet.");
                return;
            }
            if (RedCaptain.Equals(player))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You've already been assigned as the RED captain.");
                return;
            }
            if (BlueCaptain.Equals(player))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You've already been assigned as the BLUE captain.");
                return;
            }
            if (!RedCaptainExists)
            {
                await SetCaptain(player, Team.Red);
            }
            else if (!BlueCaptainExists)
            {
                await SetCaptain(player, Team.Blue);
            }
        }

        /// <summary>
        /// Processes the addition of a player to the eligible player list.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        /// This is called in response to input received from <see cref="PickupAddCmd"/>.
        /// </remarks>
        public async Task ProcessAddPlayer(string player)
        {
            if (IsQlGameInProgress())
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Cannot add to pickup because a game is in progress.");
                return;
            }
            if (EligiblePlayers.Contains(player))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You've already added yourself to the pickup!");
                return;
            }
            if (IsPickupPreGame || IsPickupInProgress)
            {
                // Deny player who is already on red or blue (i.e. already set to play this pickup) who tries
                // to get a head start for adding to the next pickup.
                if (_ssb.ServerInfo.IsActivePlayer(player))
                {
                    await _ssb.QlCommands.QlCmdSay("^1[ERROR] You are already on a team for this pickup.");
                    return;
                }
                // Add the player.
                EligiblePlayers.Add(player);
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^3[PICKUP] {0},^7 you are now signed up for the next pickup game.",
                            player));
                // If we now have enough players, start the captain selection process.
                if ((EligiblePlayers.Count >= _ssb.Mod.Pickup.Teamsize * 2) && !HasCaptainSelectionStarted)
                {
                    await StartCaptainSelection();
                }
            }
            else
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 A pickup has not been set to start.");
                await ShowPrivUserCanStartMsg();
            }
        }

        /// <summary>
        /// Processes the player sub.
        /// </summary>
        /// <param name="fromPlayer">The player sending the substitution request (the outgoing player).</param>
        /// <param name="playerToSub">The player to sub in.</param>
        public async Task ProcessPlayerSub(string fromPlayer, string playerToSub)
        {
            if (!Tools.KeyExists(playerToSub, _ssb.ServerInfo.CurrentPlayers))
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 {0} is not currently on the server!",
                    playerToSub));
                return;
            }
            if (!EligiblePlayers.Contains(playerToSub))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^1[ERROR]^3 {0} has not signed up with: ^7{1}{2}^3 yet!", playerToSub,
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd));
                return;
            }
            if (!_ssb.ServerInfo.IsActivePlayer(fromPlayer))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You may not request a sub.");
                return;
            }
            var team = _ssb.ServerInfo.CurrentPlayers[fromPlayer].Team;
            if (RedCaptain.Equals(fromPlayer) || BlueCaptain.Equals(fromPlayer))
            {
                await DoCaptainSub(fromPlayer, team, playerToSub);
            }
            else
            {
                await DoPlayerSub(fromPlayer, team, playerToSub);  
            } 
        }

        /// <summary>
        /// Performs the substituion for a regular player.
        /// </summary>
        /// <param name="outPlayer">The player to sub out.</param>
        /// <param name="team">The team to move the sub to (outPlayer's team).</param>
        /// <param name="inPlayer">The player to sub in.</param>
        private async Task DoPlayerSub(string outPlayer, Team team, string inPlayer)
        {
            // Sub old player out
            await _ssb.QlCommands.CustCmdPutPlayer(outPlayer, Team.Spec);
            // Sub new player in
            await _ssb.QlCommands.CustCmdPutPlayer(inPlayer, team);
            await NotifyNewPlayer(inPlayer, team);
            EligiblePlayers.Remove(inPlayer);
            // Record the outgoing player's substituion for tracking/banning purposes
            //TODO: increment player's subsUsed in database
        }

        /// <summary>
        /// Performs the substitution for a captain.
        /// </summary>
        /// <param name="outCaptain">The captain to sub out.</param>
        /// <param name="team">The team to move the sub to (outCaptain's team).</param>
        /// <param name="inCaptain">The captain to sub in.</param>
        /// <returns></returns>
        private async Task DoCaptainSub(string outCaptain, Team team, string inCaptain)
        {
            // Sub old captain out
            await _ssb.QlCommands.CustCmdPutPlayer(outCaptain, Team.Spec);
            // Sub new captain in and set
            await SetCaptain(inCaptain, team);
            // Examine which captain's pick it is and set it again so new captain becomes aware
            if (_isBlueTurnToPick)
            {
                await SetPickingTeam(Team.Blue);
            }
            else if (_isRedTurnToPick)
            {
                await SetPickingTeam(Team.Red);
            }
            // Record the outgoing captain's substitution for tracking/banning purposes.
            //TODO: increment player's subsUsed in database.
        }

        /// <summary>
        /// Processes and evaluates the captain's player pick and performs it if's possible to do so.
        /// </summary>
        /// <param name="fromPlayer">The name of the player sending the command.</param>
        /// <param name="playerToPick">The player to pick.</param>
        /// <remarks>
        /// This is called in response to input received from <see cref="PickupPickCmd"/>.
        /// </remarks>
        public async Task ProcessPlayerPick(string fromPlayer, string playerToPick)
        {
            if (!RedCaptain.Equals(fromPlayer) && !BlueCaptain.Equals(fromPlayer))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 You cannot pick because you are not a captain!");
                return;
            }
            if ((RedCaptain.Equals(fromPlayer) && !_isRedTurnToPick) ||
                (BlueCaptain.Equals(fromPlayer) && !_isBlueTurnToPick))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 It is not your turn to pick!");
                return;
            }
            if (IsQlGameInProgress())
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot pick a player because a game is in progress.");
                return;
            }
            if (!HasTeamSelectionStarted)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot pick a player because team selection hasn't started.");
                return;
            }

            if (RedCaptain.Equals(fromPlayer))
            {
                await DoPlayerPick(playerToPick, Team.Red);
            }
            else if (BlueCaptain.Equals(fromPlayer))
            {
                await DoPlayerPick(playerToPick, Team.Blue);
            }
        }

        /// <summary>
        /// Processes the removal of a player from the eligible player list.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        /// This is called in response to input received from <see cref="PickupRemoveCmd"/>.
        /// </remarks>
        public async Task ProcessRemovePlayer(string player)
        {
            if (IsQlGameInProgress())
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot remove from pickup because a game is in progress.");
                return;
            }
            if (!EligiblePlayers.Contains(player))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Can't remove since you were not added to the next pickup!");
                return;
            }
            if (!HasTeamSelectionStarted)
            {
                EligiblePlayers.Remove(player);
                await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[PICKUP] {0},^7 you have now removed yourself from the next pickup game.",
                        player));
            }
            else
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 You can't remove once team selection has started!");
            }
        }

        /// <summary>
        /// Event handler that is called upon the expiration of the captain selection timer.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private async void CaptainSelectionExpired(object sender, ElapsedEventArgs e)
        {
            // Players might have disconnected before the captain selection timer expired, so
            // make sure we have enough players one last time, and reset if we don't.
            if (EligiblePlayers.Count < (_ssb.Mod.Pickup.Teamsize * 2))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 There are no longer enough eligible players. Resetting...");
                await ShowPrivUserCanCancelMsg();
                await ResetPickup();
                return;
            }
            // We have zero captains -- no one bothered to !cap or it's a rare situation where both
            // captains disconnected before team selection.
            if (!RedCaptainExists && !BlueCaptainExists)
            {
                // Randomly pick the two captains and move to the proper teams so that team selection can begin.
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^3[PICKUP]^7 No one signed up to be captain! Randomly selecting 2 captains...");
                var rand = new Random();
                var captains = EligiblePlayers.OrderBy(x => rand.Next()).Take(2).ToArray();
                await SetCaptain(captains[0], Team.Red);
                await SetCaptain(captains[1], Team.Blue);
                await MoveCaptainsToTeams();
                await StartTeamSelection();
            }
            // We only have one captain
            else if ((!BlueCaptainExists && (RedCaptainExists)) ||
                (!RedCaptainExists && (BlueCaptainExists)))
            {
                // We have the red captain. Randomly pick the blue captain and move both captains to
                // the proper teams so that team selection can begin.
                if (!BlueCaptainExists && RedCaptainExists)
                {
                    await
                    _ssb.QlCommands.QlCmdSay(
                        "^3[PICKUP]^7 No ^5BLUE^7 captain found. Randomly selecting ^5BLUE^7 captain.");
                    var rand = new Random();
                    var captain = EligiblePlayers.OrderBy(x => rand.Next()).Take(1).ToArray();
                    await SetCaptain(captain[0], Team.Blue);
                    await MoveCaptainsToTeams();
                    await StartTeamSelection();
                }
                // We have the blue captain. Randomly pick the red captain and move both captains to
                // the proper teams so that team selection can begin.
                if (!RedCaptainExists && BlueCaptainExists)
                {
                    await
                   _ssb.QlCommands.QlCmdSay(
                       "^3[PICKUP]^7 No ^1RED^7 captain found. Randomly selecting ^1RED^7 captain.");
                    var rand = new Random();
                    var captain = EligiblePlayers.OrderBy(x => rand.Next()).Take(1).ToArray();
                    await SetCaptain(captain[0], Team.Red);
                    await MoveCaptainsToTeams();
                    await StartTeamSelection();
                }
            }
            HasCaptainSelectionStarted = false;
        }

        /// <summary>
        /// Clears the captains and eligible players.
        /// </summary>
        private void ClearCaptsAndEligiblePlayers()
        {
            EligiblePlayers.Clear();
            RedCaptain = string.Empty;
            BlueCaptain = string.Empty;
        }

        /// <summary>
        /// Moves all of the players except for the bot to spectator mode.
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
        /// Displays the list of eligible players, if any.
        /// </summary>
        private async Task DisplayEligiblePlayers()
        {
            await _ssb.QlCommands.QlCmdSay(string.Format("^7Eligible players: {0}",
                ((EligiblePlayers.Count != 0) ? "^3" + string.Join(",", EligiblePlayers) : "^1NO eligible players!")));
        }

        /// <summary>
        /// Performs the captain's player pick.
        /// </summary>
        /// <param name="player">The player to pick.</param>
        /// <param name="team">The team on which the player should be placed.</param>
        private async Task DoPlayerPick(string player, Team team)
        {
            if (!EligiblePlayers.Contains(player))
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 {0} is not an eligible player!",
                    player));
                await DisplayEligiblePlayers();
                return;
            }
            await _ssb.QlCommands.QlCmdSay(string.Format("^3[PICKUP]{0} ^7({1}{2}^7) picked {1}{3}",
                ((team == Team.Red) ? "^1RED" : "^5BLUE"), ((team == Team.Red) ? "^1" : "^5"),
                ((team == Team.Red) ? RedCaptain : BlueCaptain), player));

            if (team == Team.Red)
            {
                await _ssb.QlCommands.CustCmdPutPlayer(player, Team.Red);
                await SetPickingTeam(Team.Blue);
            }
            else if (team == Team.Blue)
            {
                await _ssb.QlCommands.CustCmdPutPlayer(player, Team.Blue);
                await SetPickingTeam(Team.Red);
            }
            // Notify player
            await NotifyNewPlayer(player, team);
            
            // Player can no longer be picked
            EligiblePlayers.Remove(player);

            // Teams are full, we are ready to start
            if ((_ssb.ServerInfo.GetTeam(Team.Red).Count == _ssb.Mod.Pickup.Teamsize) &&
                ((_ssb.ServerInfo.GetTeam(Team.Blue).Count == _ssb.Mod.Pickup.Teamsize)))
            {
                HasTeamSelectionStarted = false;
                await _ssb.QlCommands.QlCmdSay("^3[PICKUP]^7 Teams are now ^1FULL.^7 PLEASE ^*2READY UP*^7 TO START THE GAME!");
            }
        }

        /// <summary>
        /// Notifies the new player that he has been picked.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team for which the player was picked.</param>
        private async Task NotifyNewPlayer(string player, Team team)
        {
            await
                _ssb.QlCommands.QlCmdTell(player,
                    string.Format(
                        "^7You've been picked for {0}^7. If you must leave early: ^3!sub <spectator>^7 to avoid a no-show.",
                        ((team == Team.Red) ? "^1RED" : "^5BLUE")));
        }

        /// <summary>
        /// Determines whether a Quake Live game is in progress.
        /// </summary>
        /// <returns><c>true</c> if the QL game is in countdown or in-progress mode, otherwise <c>false</c>.</returns>
        private bool IsQlGameInProgress()
        {
            return _ssb.ServerInfo.CurrentServerGameState == QlGameStates.Countdown ||
                   _ssb.ServerInfo.CurrentServerGameState == QlGameStates.InProgress;
        }

        /// <summary>
        /// Moves the captains to their proper teams so that team selection will be possible.
        /// </summary>
        private async Task MoveCaptainsToTeams()
        {
            await _ssb.QlCommands.CustCmdPutPlayer(RedCaptain, Team.Red);
            await _ssb.QlCommands.CustCmdPutPlayer(BlueCaptain, Team.Blue);
            string[] capNotifyMsgs =
            {
                "^1You are now a captain. When it's your turn type ^2!pick <name>^7 to pick a player for your team.",
                "^7If you must leave early be sure to !sub <name> to avoid a no-show. Abusing this will get you banned."
            };
            foreach (var msg in capNotifyMsgs)
            {
                await _ssb.QlCommands.QlCmdTell(RedCaptain, msg);
                await _ssb.QlCommands.QlCmdTell(BlueCaptain, msg);
            }
        }

        /// <summary>
        /// Resets the pickup back to pre-game mode.
        /// </summary>
        private async Task ResetPickup()
        {
            await _ssb.QlCommands.QlCmdSay("^3[PICKUP]^7 Attempting to reset pickup...");
            ClearCaptsAndEligiblePlayers();
            await StartPickupPreGame();
        }

        /// <summary>
        /// Sets the captain for a given team.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team to which the captain should be moved.</param>
        private async Task SetCaptain(string player, Team team)
        {
            switch (team)
            {
                case Team.Red:
                    RedCaptain = player;
                    // Captains from here on are treated differently from eligible players.
                    EligiblePlayers.Remove(player);
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format("^3[PICKUP]^1 {0}^7 is now the ^1RED^7 captain", player));
                    break;

                case Team.Blue:
                    BlueCaptain = player;
                    // Captains from here on are treated differently from eligible players.
                    EligiblePlayers.Remove(player);
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format("^3[PICKUP]^5 {0}^7 is now the ^5BLUE^7 captain", player));
                    break;
            }
        }

        /// <summary>
        /// Sets the picking team.
        /// </summary>
        /// <param name="team">The team.</param>
        private async Task SetPickingTeam(Team team)
        {
            switch (team)
            {
                case Team.Red:
                    _isRedTurnToPick = true;
                    _isBlueTurnToPick = false;
                    break;

                case Team.Blue:
                    _isRedTurnToPick = false;
                    _isBlueTurnToPick = true;
                    break;
            }
            await _ssb.QlCommands.QlCmdSay(string.Format("^3[PICKUP]^7 It is the {0}^7 captain ({1})'s pick. Type ^2!pick <name>^7 to pick a player.",
                ((team == Team.Red) ? "^1RED" : "^5BLUE"),
                ((team == Team.Red) ? RedCaptain : BlueCaptain)));
            await DisplayEligiblePlayers();
        }

        /// <summary>
        /// Prepares the teams so that captains and players can be picked.
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
        /// Shows a public message including the names of current privileged users who may cancel the
        /// pickup, and provides the syntax for the command that cancels the pickup.
        /// </summary>
        private async Task ShowPrivUserCanCancelMsg()
        {
            var superUsers = _userDb.GetSuperUsersOrHigherOnServer(_ssb.ServerInfo.CurrentPlayers);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0} can completely cancel the pickup with^7 {1}{2} stop",
                    (string.IsNullOrEmpty(superUsers) ? "A super-user or higher." : superUsers.TrimEnd(',', ' ')),
                    CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }

        /// <summary>
        /// Shows a public message including the names of current privileged users who may reset the
        /// pickup, and provides the syntax for the command that resets the pickup.
        /// </summary>
        private async Task ShowPrivUserCanResetMsg()
        {
            var superUsers = _userDb.GetSuperUsersOrHigherOnServer(_ssb.ServerInfo.CurrentPlayers);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0} can reset the pickup with^7 {1}{2} reset",
                    (string.IsNullOrEmpty(superUsers) ? "A super-user or higher." : superUsers.TrimEnd(',', ' ')),
                    CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }

        /// <summary>
        /// Shows a public message including the names of current privileged users who may start the
        /// pickup, and provides the syntax for the command that starts the pickup.
        /// </summary>
        private async Task ShowPrivUserCanStartMsg()
        {
            var superUsers = _userDb.GetSuperUsersOrHigherOnServer(_ssb.ServerInfo.CurrentPlayers);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0} can start a new pickup with^7 {1}{2} start",
                    (string.IsNullOrEmpty(superUsers) ? "A super-user or higher." : superUsers.TrimEnd(',', ' ')),
                    CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }

        /// <summary>
        /// Shows the in-progress error.
        /// </summary>
        private async Task ShowProgressInError()
        {
            await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Pickup games can only be started, stopped or reset from warm-up mode!");
        }

        /// <summary>
        /// Starts the captain selection timer, giving all eligible players a certain amount of
        /// time to issue the cap command to sign up as a captain for the upcoming pickup game.
        /// </summary>
        private async Task StartCaptainSelection()
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
                    (CaptainSelectionTimeLimit / 1000), CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupCap));
        }

        /// <summary>
        /// Starts the pickup pre game mode that sets up the server for an upcoming pickup game.
        /// </summary>
        private async Task StartPickupPreGame()
        {
            // Housekeeping...
            ClearCaptsAndEligiblePlayers();
            IsPickupPreGame = true;
            IsPickupInProgress = false;
            HasCaptainSelectionStarted = false;
            HasTeamSelectionStarted = false;
            _isBlueTurnToPick = false;
            _isRedTurnToPick = false;
            
            // Lock down the server
            await SetupTeams();
            await _ssb.QlCommands.QlCmdSay(string.Format("^3[PICKUP]^7 Pickup mode is enabled. To be eligible to play, type: ^2{0}{1}",
                CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd));
            await _ssb.QlCommands.QlCmdSay(string.Format("^3[PICKUP]^7 At least ^2{0}^7 players needed before team & captains are picked.",
                (_ssb.Mod.Pickup.Teamsize * 2)));
        }

        /// <summary>
        /// Starts the process of team selection.
        /// </summary>
        private async Task StartTeamSelection()
        {
            HasTeamSelectionStarted = true;
            // Randomly decide which of the two captains receives the first pick
            var rand = new Random();
            int n = rand.Next(0, 2);
            if (n == 0)
            {
                await SetPickingTeam(Team.Red);
            }
            else
            {
                await SetPickingTeam(Team.Blue);
            }
        }

        /// <summary>
        /// Stops the pickup and unlocks the teams so that anyone can join.
        /// </summary>
        private async Task StopPickup()
        {
            await
                _ssb.QlCommands.QlCmdSay(string.Format("^3[PICKUP]^7 Stopping pickup. Teams will be free for anyone to join. ^2{0}{1} start^7 to start another.",
                CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
            ClearCaptsAndEligiblePlayers();
            await ClearTeams();
            _ssb.QlCommands.SendToQl("unlock", false);
        }
    }
}