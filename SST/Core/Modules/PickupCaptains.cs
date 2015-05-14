using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Core.Commands.None;
using SST.Database;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Core.Modules
{
    /// <summary>
    /// Class associated with pickup captains and related methods.
    /// </summary>
    public class PickupCaptains
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[PICKUP]";
        private readonly PickupManager _manager;
        private readonly SynServerTool _sst;

        public PickupCaptains(SynServerTool sst, PickupManager manager)
        {
            _sst = sst;
            _manager = manager;
        }

        /// <summary>
        /// Gets or sets the blue captain.
        /// </summary>
        /// <value>The blue captain.</value>
        public string BlueCaptain { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a blue captain.
        /// </summary>
        /// <value><c>true</c> if there is a blue captain; otherwise, <c>false</c>.</value>
        public bool BlueCaptainExists
        {
            get
            {
                return (!string.IsNullOrEmpty(BlueCaptain) &&
                        Helpers.KeyExists(BlueCaptain, _sst.ServerInfo.CurrentPlayers));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it is the blue captain's turn to pick.
        /// </summary>
        /// <value><c>true</c> if it is the blue captain's turn to pick; otherwise, <c>false</c>.</value>
        public bool IsBlueTurnToPick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is the red captain's turn to pick.
        /// </summary>
        /// <value><c>true</c> if it is the red captain's turn to pick; otherwise, <c>false</c>.</value>
        public bool IsRedTurnToPick { get; set; }

        /// <summary>
        /// Gets or sets the red captain.
        /// </summary>
        /// <value>The red captain.</value>
        public string RedCaptain { get; set; }

        /// <summary>
        /// Gets a value indicating whether there is a red captain.
        /// </summary>
        /// <value><c>true</c> if there is a red captain; otherwise, <c>false</c>.</value>
        public bool RedCaptainExists
        {
            get
            {
                return (!string.IsNullOrEmpty(RedCaptain) &&
                        Helpers.KeyExists(RedCaptain, _sst.ServerInfo.CurrentPlayers));
            }
        }

        public string StatusMessage { get; set; }

        /// <summary>
        /// Evaluates the addition of a captain to see if it is possible to add the captain, and
        /// adds if so.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if it is possible to add the captain; otherwise <c>false</c>.</returns>
        /// <remarks>This is called in response to input received from <see cref="PickupCapCmd"/>.</remarks>
        public async Task<bool> ProcessAddCaptain(CmdArgs c)
        {
            if (_manager.IsQlGameInProgress)
            {
                StatusMessage = "^1[ERROR]^3 Cannot become a pickup captain because a game is in progress.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!_manager.HasCaptainSelectionStarted)
            {
                StatusMessage =
                    "^1[ERROR]^3 Cannot become a pickup captain because captain selection hasn't started yet.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!_manager.AvailablePlayers.Contains(c.FromUser))
            {
                StatusMessage =
                    string.Format(
                        "^1[ERROR]^3 You can't cap because you're not signed up. ^7{0}{1}^3 to sign up.",
                        CommandList.GameCommandPrefix, CommandList.CmdPickupAdd);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (RedCaptain.Equals(c.FromUser))
            {
                StatusMessage = "^1[ERROR]^3 You've already been assigned as the RED captain.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (BlueCaptain.Equals(c.FromUser))
            {
                StatusMessage = "^1[ERROR]^3 You've already been assigned as the BLUE captain.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!RedCaptainExists)
            {
                await SetCaptain(c.FromUser, Team.Red, false);
            }
            else if (!BlueCaptainExists)
            {
                await SetCaptain(c.FromUser, Team.Blue, false);
            }
            return true;
        }

        /// <summary>
        /// Processes and evaluates the captain's player pick and performs it if's possible to do so.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><C>true</C> if the player could be picked, otherwise <c>false</c>.</returns>
        /// <remarks>This is called in response to input received from <see cref="PickupPickCmd"/>.</remarks>
        public async Task<bool> ProcessPlayerPick(CmdArgs c)
        {
            if (!RedCaptain.Equals(c.FromUser) && !BlueCaptain.Equals(c.FromUser))
            {
                StatusMessage = "^1[ERROR]^3 You cannot pick because you are not a captain!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if ((RedCaptain.Equals(c.FromUser) && !IsRedTurnToPick) ||
                (BlueCaptain.Equals(c.FromUser) && !IsBlueTurnToPick))
            {
                StatusMessage = "^1[ERROR]^3 It is not your turn to pick!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (_manager.IsQlGameInProgress)
            {
                StatusMessage = "^1[ERROR]^3 Cannot pick a player because a game is in progress.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!_manager.HasTeamSelectionStarted)
            {
                StatusMessage = "^1[ERROR]^3 Cannot pick a player because team selection hasn't started.";
                await SendServerTell(c, StatusMessage);
                return false;
            }

            if (RedCaptain.Equals(c.FromUser))
            {
                await DoPlayerPick(c, Team.Red);
            }
            else if (BlueCaptain.Equals(c.FromUser))
            {
                await DoPlayerPick(c, Team.Blue);
            }
            return true;
        }

        /// <summary>
        /// Sets the captain for a given team.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team to which the captain should be moved.</param>
        /// <param name="isCaptainSub">
        /// if set to <c>true</c> indicates that the captain is being set as part of a captain
        /// substitution process.
        /// </param>
        public async Task SetCaptain(string player, Team team, bool isCaptainSub)
        {
            switch (team)
            {
                case Team.Red:
                    RedCaptain = player;
                    // Captains from here on are treated differently from eligible players.
                    _manager.RemoveEligibility(player);
                    await
                        _sst.QlCommands.QlCmdSay(
                            string.Format("^5[PICKUP]^1 {0}^7 is now the ^1RED^7 captain", player));
                    break;

                case Team.Blue:
                    BlueCaptain = player;
                    // Captains from here on are treated differently from eligible players.
                    _manager.RemoveEligibility(player);
                    await
                        _sst.QlCommands.QlCmdSay(
                            string.Format("^5[PICKUP]^5 {0}^7 is now the ^5BLUE^7 captain", player));
                    break;
            }
            if (RedCaptainExists && BlueCaptainExists)
            {
                await
                    _sst.QlCommands.QlCmdSay(string.Format(
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

            Log.Write(string.Format("It is the {0} captain's turn to pick.",
                (team == Team.Red) ? "RED" : "BLUE"), _logClassType, _logPrefix);

            // Avoid showing this when team is full
            if (_sst.ServerInfo.GetTeam(team).Count != _sst.Mod.Pickup.Teamsize)
            {
                await ShowWhosePick(team);
                await _manager.DisplayAvailablePlayers();
            }
        }

        /// <summary>
        /// Performs the captain's player pick.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="team">The team on which the player should be placed.</param>
        /// <returns></returns>
        private async Task DoPlayerPick(CmdArgs c, Team team)
        {
            if (!_manager.AvailablePlayers.Contains(Helpers.GetArgVal(c, 1)))
            {
                StatusMessage = string.Format("^1[ERROR]^3 {0} is not an eligible player!",
                    Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);
                await _manager.DisplayAvailablePlayers();
                await ShowWhosePick(team);
                return;
            }
            await _sst.QlCommands.QlCmdSay(string.Format("^5[PICKUP] {0} ^7({1}{2}^7) picked {1}{3}",
                ((team == Team.Red) ? "^1RED" : "^5BLUE"), ((team == Team.Red) ? "^1" : "^5"),
                ((team == Team.Red) ? RedCaptain : BlueCaptain), Helpers.GetArgVal(c, 1)));

            if (team == Team.Red)
            {
                _manager.RemoveEligibility(Helpers.GetArgVal(c, 1));
                await _sst.QlCommands.CustCmdPutPlayer(Helpers.GetArgVal(c, 1), Team.Red);
                _manager.AddActivePickupPlayer(Helpers.GetArgVal(c, 1));
                await SetPickingTeam(Team.Blue);

                Log.Write(string.Format("RED captain picked player {0}",
                    Helpers.GetArgVal(c, 1)), _logClassType, _logPrefix);
            }
            else if (team == Team.Blue)
            {
                _manager.RemoveEligibility(Helpers.GetArgVal(c, 1));
                await _sst.QlCommands.CustCmdPutPlayer(Helpers.GetArgVal(c, 1), Team.Blue);
                _manager.AddActivePickupPlayer(Helpers.GetArgVal(c, 1));
                await SetPickingTeam(Team.Red);

                Log.Write(string.Format("BLUE captain picked player {0}",
                    Helpers.GetArgVal(c, 1)), _logClassType, _logPrefix);
            }

            // Notify player
            await _manager.NotifyNewPlayer(Helpers.GetArgVal(c, 1), team);

            // Teams are full, we are ready to start
            if (_manager.AreTeamsFull)
            {
                //At this point, add the game to the pickupgames table
                var pickupDb = new DbPickups();
                pickupDb.AddPickupGame(_manager.CreatePickupInfo());

                _manager.HasTeamSelectionStarted = false;
                await
                    _sst.QlCommands.QlCmdSay(
                        "^5[PICKUP]^4 *** ^7TEAMS ARE ^3FULL.^7 PLEASE ^2*READY UP (F3)*^7 TO START THE GAME! ^4***");
                await
                    _sst.QlCommands.QlCmdSay(
                        "^5[PICKUP]^7 Any unpicked players or late-adders will be automatically added to the substitutes list when the game starts!");

                Log.Write("Teams are now full!", _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        private async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        /// Displays a message indicating which captain has the turn to pick a player.
        /// </summary>
        /// <param name="team">The captain's team.</param>
        private async Task ShowWhosePick(Team team)
        {
            await
                _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[PICKUP]^7 It's the {0}^7 captain ({1}{2}^7)'s pick. Type ^2!pick <name>^7 to pick a player.",
                        ((team == Team.Red) ? "^1RED" : "^5BLUE"),
                        ((team == Team.Red) ? "^1" : "^5"),
                        ((team == Team.Red) ? RedCaptain : BlueCaptain)));
        }
    }
}
