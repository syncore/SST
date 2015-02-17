﻿using System.Threading.Tasks;
using SSB.Core.Commands.None;
using SSB.Database;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core.Modules
{
    /// <summary>
    /// Class associated with pickup captains and related methods.
    /// </summary>
    public class PickupCaptains
    {
        private readonly PickupManager _manager;
        private readonly SynServerBot _ssb;

        public PickupCaptains(SynServerBot ssb, PickupManager manager)
        {
            _ssb = ssb;
            _manager = manager;
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
            get
            {
                return (!string.IsNullOrEmpty(BlueCaptain) &&
                    Helpers.KeyExists(BlueCaptain, _ssb.ServerInfo.CurrentPlayers));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it is the blue captain's turn to pick.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is the blue captain's turn to pick; otherwise, <c>false</c>.
        /// </value>
        public bool IsBlueTurnToPick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is the red captain's turn to pick.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is the red captain's turn to pick; otherwise, <c>false</c>.
        /// </value>
        public bool IsRedTurnToPick { get; set; }

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
            get
            {
                return (!string.IsNullOrEmpty(RedCaptain) &&
                    Helpers.KeyExists(RedCaptain, _ssb.ServerInfo.CurrentPlayers));
            }
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
            if (_manager.IsQlGameInProgress)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot become a pickup captain because a game is in progress.");
                return;
            }
            if (!_manager.HasCaptainSelectionStarted)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot become a pickup captain because captain selection hasn't started yet.");
                return;
            }
            if (!_manager.AvailablePlayers.Contains(player))
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 You can't cap because you're not signed up. ^7{0}{1}^3 to sign up.",
                    CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd));
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
                await SetCaptain(player, Team.Red, false);
            }
            else if (!BlueCaptainExists)
            {
                await SetCaptain(player, Team.Blue, false);
            }
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
            if ((RedCaptain.Equals(fromPlayer) && !IsRedTurnToPick) ||
                (BlueCaptain.Equals(fromPlayer) && !IsBlueTurnToPick))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 It is not your turn to pick!");
                return;
            }
            if (_manager.IsQlGameInProgress)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Cannot pick a player because a game is in progress.");
                return;
            }
            if (!_manager.HasTeamSelectionStarted)
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
        /// Sets the captain for a given team.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team to which the captain should be moved.</param>
        /// <param name="isCaptainSub">if set to <c>true</c> indicates that the captain is
        /// being set as part of a captain substitution process.</param>
        public async Task SetCaptain(string player, Team team, bool isCaptainSub)
        {
            switch (team)
            {
                case Team.Red:
                    RedCaptain = player;
                    // Captains from here on are treated differently from eligible players.
                    _manager.RemoveEligibility(player);
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format("^5[PICKUP]^1 {0}^7 is now the ^1RED^7 captain", player));
                    break;

                case Team.Blue:
                    BlueCaptain = player;
                    // Captains from here on are treated differently from eligible players.
                    _manager.RemoveEligibility(player);
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format("^5[PICKUP]^5 {0}^7 is now the ^5BLUE^7 captain", player));
                    break;
            }
            if (RedCaptainExists && BlueCaptainExists)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                    "^5[PICKUP]^7 Both captains have been selected! Team selection will {0}.",
                    ((isCaptainSub) ? "continue. Please wait..." : "begin. Please wait...")));
            }
        }

        /// <summary>
        /// Sets the picking team.
        /// </summary>
        /// <param name="team">The team.</param>
        public async Task SetPickingTeam(Team team)
        {
            switch (team)
            {
                case Team.Red:
                    IsRedTurnToPick = true;
                    IsBlueTurnToPick = false;
                    break;

                case Team.Blue:
                    IsRedTurnToPick = false;
                    IsBlueTurnToPick = true;
                    break;
            }

            await ShowWhosePick(team);
            await _manager.DisplayAvailablePlayers();
        }

        /// <summary>
        /// Performs the captain's player pick.
        /// </summary>
        /// <param name="player">The player to pick.</param>
        /// <param name="team">The team on which the player should be placed.</param>
        private async Task DoPlayerPick(string player, Team team)
        {
            if (!_manager.AvailablePlayers.Contains(player))
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 {0} is not an eligible player!",
                    player));
                await _manager.DisplayAvailablePlayers();
                await ShowWhosePick(team);
                return;
            }
            await _ssb.QlCommands.QlCmdSay(string.Format("^5[PICKUP] {0} ^7({1}{2}^7) picked {1}{3}",
                ((team == Team.Red) ? "^1RED" : "^5BLUE"), ((team == Team.Red) ? "^1" : "^5"),
                ((team == Team.Red) ? RedCaptain : BlueCaptain), player));

            if (team == Team.Red)
            {
                _manager.RemoveEligibility(player);
                await _ssb.QlCommands.CustCmdPutPlayer(player, Team.Red);
                _manager.AddActivePickupPlayer(player);
                await SetPickingTeam(Team.Blue);
            }
            else if (team == Team.Blue)
            {
                _manager.RemoveEligibility(player);
                await _ssb.QlCommands.CustCmdPutPlayer(player, Team.Blue);
                _manager.AddActivePickupPlayer(player);
                await SetPickingTeam(Team.Red);
            }

            // Notify player
            await _manager.NotifyNewPlayer(player, team);

            // Teams are full, we are ready to start
            if (_manager.AreTeamsFull)
            {
                //At this point, add the game to the pickupgames table
                var pickupDb = new DbPickups();
                pickupDb.AddPickupGame(_manager.CreatePickupInfo());

                _manager.HasTeamSelectionStarted = false;
                await _ssb.QlCommands.QlCmdSay("^5[PICKUP]^4 *** ^7TEAMS ARE ^3FULL.^7 PLEASE ^2*READY UP (F3)*^7 TO START THE GAME! ^4***");
                await _ssb.QlCommands.QlCmdSay("^5[PICKUP]^7 Any unpicked players or late-adders will be automatically added to the substitutes list when the game starts!");
            }
        }

        /// <summary>
        /// Displays a message indicating which captain has the turn to pick a player.
        /// </summary>
        /// <param name="team">The captain's team.</param>
        private async Task ShowWhosePick(Team team)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format("^5[PICKUP]^7 It's the {0}^7 captain ({1}{2}^7)'s pick. Type ^2!pick <name>^7 to pick a player.",
                ((team == Team.Red) ? "^1RED" : "^5BLUE"),
                ((team == Team.Red) ? "^1" : "^5"),
                ((team == Team.Red) ? RedCaptain : BlueCaptain)));
        }
    }
}