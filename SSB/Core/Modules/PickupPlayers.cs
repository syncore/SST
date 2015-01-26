using System.Threading.Tasks;
using SSB.Core.Commands.None;
using SSB.Database;
using SSB.Enum;

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
            await _ssb.QlCommands.CustCmdPutPlayer(outPlayer, Team.Spec);
            // Sub new player in
            await _ssb.QlCommands.CustCmdPutPlayer(inPlayer, team);
            await _manager.NotifyNewPlayer(inPlayer, team);
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
        /// <remarks>
        ///     This is called in response to input received from <see cref="PickupAddCmd" />.
        /// </remarks>
        public async Task ProcessAddPlayer(string player)
        {
            if (_manager.EligiblePlayers.Contains(player))
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 You've already added yourself to the pickup!",
                        player);
                return;
            }
            // Deny player who is already on red or blue (i.e. already set to play this pickup) who tries
            // to get a head start for adding to the next pickup.
            if (_ssb.ServerInfo.IsActivePlayer(player))
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR] You are already on a team for this pickup.", player);
                return;
            }
            if (_manager.IsPickupInProgress)
            {
                if (_manager.InProgressSubCandidates.Contains(player))
                {
                    await
                        _ssb.QlCommands.QlCmdTell(
                            "^1[ERROR] You are already signed up as a sub for the current game!",
                            player);
                    return;
                }
                // User will be signed up as a sub for the current in-progress game
                _manager.InProgressSubCandidates.Add(player);
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^5[PICKUP]^7 Game is already in progress, but you're now signed up as a sub if one is needed.",
                        player);
            }
            if (_manager.IsPickupPreGame)
            {
                // Add the player.
                _manager.EligiblePlayers.Add(player);
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
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupRemove),
                        player);

                // If we now have enough players, start the captain selection process.
                if ((_manager.EligiblePlayers.Count >= _ssb.Mod.Pickup.Teamsize*2) &&
                    !_manager.HasCaptainSelectionStarted)
                {
                    await _manager.StartCaptainSelection();
                }
            }
            else
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 A pickup has not been set to start.");
                await _manager.ShowPrivUserCanStartMsg();
            }
        }

        /// <summary>
        ///     Processes the removal of a player from the eligible player list.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        ///     This is called in response to input received from <see cref="PickupRemoveCmd" />.
        /// </remarks>
        public async Task ProcessRemovePlayer(string player)
        {
            if (!_manager.EligiblePlayers.Contains(player))
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 Can't remove since you were not added to the next pickup!",
                        player);
                return;
            }
            if (!_manager.HasTeamSelectionStarted)
            {
                _manager.RemoveEligibility(player);
                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^5[PICKUP]^3 {0},^7 you have now removed yourself from the next pickup game.",
                            player), player);
            }
            else
            {
                await
                    _ssb.QlCommands.QlCmdTell(
                        "^1[ERROR]^3 You can't remove once team selection has started!", player);
            }
            if (_manager.InProgressSubCandidates.Contains(player))
            {
                _manager.RemoveEligibility(player);
                await
                    _ssb.QlCommands.QlCmdTell(
                        string.Format(
                            "^5[PICKUP] {0},^7 you are no longer signed up as a sub for the current game.",
                            player), player);
            }
        }

        /// <summary>
        ///     Shows the numbers of players signed up and needed.
        /// </summary>
        private async Task ShowNumPlayers()
        {
            var playersNeeded = ((_ssb.Mod.Pickup.Teamsize*2) -
                                 (_manager.EligiblePlayers.Count));
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[PICKUP]^2 {0}^7 players signed up (^3{1}^7 more needed)",
                        _manager.EligiblePlayers.Count,
                        ((playersNeeded < 0) ? "0" : playersNeeded.ToString())));
        }
    }
}