using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Core.Commands.None;
using SSB.Database;
using SSB.Enums;

namespace SSB.Core.Modules
{
    /// <summary>
    ///     Class associated with standard pickup players and related methods.
    /// </summary>
    public class PickupPlayers
    {
        private readonly PickupManager _manager;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupPlayers" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        /// <param name="manager">The pickup manager.</param>
        public PickupPlayers(SynServerBot ssb, PickupManager manager)
        {
            _ssb = ssb;
            _manager = manager;
        }

        /// <summary>
        ///     Performs the substituion for a regular player.
        /// </summary>
        /// <param name="outPlayer">The player to sub out.</param>
        /// <param name="team">The team to move the sub to (outPlayer's team).</param>
        /// <param name="inPlayer">The player to sub in.</param>
        public async Task DoPlayerSub(string outPlayer, Team team, string inPlayer)
        {
            // Set as successful sub so user doesn't get counted as no-show
            _ssb.ServerInfo.CurrentPlayers[outPlayer].HasMadeSuccessfulSubRequest = true;
            // Sub old player out
            await _ssb.QlCommands.CustCmdPutPlayerDelayed(outPlayer, Team.Spec, 2);
            // Sub new player in
            await _ssb.QlCommands.CustCmdPutPlayerDelayed(inPlayer, team, 2);
            Debug.WriteLine("Player sub: trying to put player {0} on team {1} with delay", inPlayer, team);
            // Set player as active
            _manager.AddActivePickupPlayer(inPlayer);
            // Announce
            await
                _ssb.QlCommands.QlCmdSay(string.Format(
                    "^5[PICKUP]^7 Subbed out old {0} ^7player: {1} for new {0} ^7player: {2}",
                    ((team == Team.Red) ? "^1RED" : "^5BLUE"), outPlayer, inPlayer));

            // Tell the player the rules
            await _manager.NotifyNewPlayer(inPlayer, team);
            // Remove from sub candidates
            _manager.RemoveEligibility(inPlayer);
            // Record the outgoing player's substituion for tracking/banning purposes
            _manager.Subs.Append(string.Format("{0}->{1},", inPlayer, outPlayer));
            var pickupDb = new DbPickups();
            pickupDb.IncrementUserSubsUsedCount(outPlayer);
        }

        /// <summary>
        ///     Processes the addition of a player to the eligible player list.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///     <c>true</c> if the player could be added, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     This is called in response to input received from <see cref="PickupAddCmd" />.
        /// </remarks>
        public async Task<bool> ProcessAddPlayer(string player)
        {
            if (_manager.AvailablePlayers.Contains(player) || _manager.SubCandidates.Contains(player))
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 You've already added yourself!",
                        player);
                return false;
            }
            // Deny player who is already on red or blue (i.e. already set to play this pickup) who tries
            // to get a head start for adding to the next pickup.
            if (_ssb.ServerInfo.IsActivePlayer(player) ||
                _manager.ActivePickupPlayers.Contains(player))
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR] You are already on a team for this pickup.", player);
                return false;
            }
            if (_manager.IsPickupInProgress)
            {
                // User will be signed up as a sub for the current in-progress game
                _manager.SubCandidates.Add(player);
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^5[PICKUP]^7 Game is already in progress, but you're now signed up as a sub if one is needed.",
                        player);
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[PICKUP] ^3{0} ^7has signed up as a substitute player for the current game.",
                            player));
                return true;
            }
            // Someone adds and teams are not yet full, i.e. a regular add situation OR a late add.
            // (Team selection might've already started but add anyway.)
            if (_manager.IsPickupPreGame && !_manager.AreTeamsFull)
            {
                // Add the player.
                _manager.AvailablePlayers.Add(player);
                await ShowNumPlayers();

                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^5[PICKUP]^3 {0},^7 you are now signed up for the next pickup game.",
                            player), player);
                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^5[PICKUP]^7 you may remove yourself before team selection begins with: ^3{0}{1}",
                            CommandList.GameCommandPrefix, CommandList.CmdPickupRemove),
                        player);

                // If we now have enough players, start the captain selection process.
                if ((_manager.AvailablePlayers.Count >= _ssb.Mod.Pickup.Teamsize*2) &&
                    !_manager.HasCaptainSelectionStarted)
                {
                    await _manager.StartCaptainSelection();
                }

                return true;
            }
            // Someone adds after teams have been picked (thus are full), but before everyone has readied up to launch game
            // Add as a substitute
            if (_manager.IsPickupPreGame && _manager.AreTeamsFull)
            {
                _manager.SubCandidates.Add(player);
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^5[PICKUP]^3 {0} ^7has signed up as a substitute player for the current game.",
                            player));
                return true;
            }
            await
                _ssb.QlCommands.QlCmdTell("^1[ERROR]^3 A pickup has not been set to start.",
                    player);
            await _manager.ShowPrivUserCanStartMsg();
            return false;
        }

        /// <summary>
        ///     Processes the removal of a player from the eligible player list.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///     <c>true</c> if the player could be removed, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     This is called in response to input received from <see cref="PickupRemoveCmd" />.
        /// </remarks>
        public async Task<bool> ProcessRemovePlayer(string player)
        {
            if (!_manager.AvailablePlayers.Contains(player) && !_manager.SubCandidates.Contains(player))
            {
                // NOTE: it might be a captain, in which case he would not be contained in either of these groups, therefore send
                // a generic error instead of (you can't remove since not added) rather than having another messy conditional
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 You cannot remove now. You weren't added or it is too late to remove.",
                        player);
                return false;
            }
            // Players can remove either before captains have started to pick players, or after the picking process is over.
            if (!_manager.HasTeamSelectionStarted)
            {
                // This will also remove the player from the substitutes, if applicable
                _manager.RemoveEligibility(player);
                await ShowNumPlayers();
                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^5[PICKUP]^3 {0},^7 you have now removed yourself.",
                            player), player);
                return true;
            }
            await
                _ssb.QlCommands.QlCmdTell(
                    "^1[ERROR]^3 You can't remove once team selection has started!", player);
            return false;
        }

        /// <summary>
        ///     Shows the numbers of players signed up and needed.
        /// </summary>
        private async Task ShowNumPlayers()
        {
            var activePlayers = string.Empty;
            var availPlayers = string.Empty;
            var subPlayers = string.Empty;

            if (_manager.ActivePickupPlayers.Count > 0)
            {
                activePlayers = string.Format("^2[Active: {0}] {1}",
                    _manager.ActivePickupPlayers.Count,
                    string.Join(", ", _manager.ActivePickupPlayers));
            }
            if (_manager.AvailablePlayers.Count > 0)
            {
                availPlayers = string.Format("^3[Available: {0}] {1}",
                    _manager.AvailablePlayers.Count,
                    string.Join(", ", _manager.AvailablePlayers));
            }
            if (_manager.SubCandidates.Count > 0)
            {
                subPlayers = string.Format("^6[Subs: {0}] {1}",
                    _manager.SubCandidates.Count, string.Join(", ", _manager.SubCandidates));
            }

            if (activePlayers.Length + availPlayers.Length + subPlayers.Length > 0)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^5[PICKUP] ^1[Size: {0}v{0}] {1} {2} {3}",
                    _ssb.Mod.Pickup.Teamsize, activePlayers, availPlayers, subPlayers));
            }
        }
    }
}