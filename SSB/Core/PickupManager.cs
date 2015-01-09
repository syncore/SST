using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;

namespace SSB.Core
{
    /// <summary>
    /// Class responsible for managing pickup games.
    /// </summary>
    public class PickupManager
    {
        private readonly SynServerBot _ssb;
        private bool _isPickupPreGame;
        private bool _isPickupInProgress;
        private readonly DbUsers _userDb;
        //private readonly PickupUsers _pickupUserDb;
        
        public PickupManager(SynServerBot ssb)
        {
            _ssb = ssb;
            _userDb = new DbUsers();
            // _pickupUserDb = new DbPickupUsers();
        }

        public bool IsPickupPreGame { get; set; }

        public bool IsPickupInProgress { get; set; }

        public async Task EvalPickupReset()
        {
            if (IsQlGameInProgress())
            {
                await ShowProgressInError();
                return;
            }
        }

        public async Task EvalPickupStart()
        {
            if (IsQlGameInProgress())
            {
                await ShowProgressInError();
                return;
            }
            if (_isPickupPreGame || _isPickupInProgress)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Another pickup game is already pending!");
                return;
            }
            await StartPickup();
        }

        private async Task StartPickup()
        {
            // Force bot to join a random team ("team A") since can't vote as spectator
            // Lock both teams ("lock")
            // Move all players except for the bot to spec
            // Callvote teamsize based on teamsize set in pickup module options
            // Force bot to spectators ("team s")
            // Start a 30 sec timer that will wait for two different players to !cap.
            // Player 1 cap: move to red
            // Player 2 cap: move to blue
            // If there is only one cap at the end of the 30 sec, use the cap who volunteered, and randomly
            // pick the other captain.
            // if no one (i.e. there aren't 2 caps) decides to be a cap within the 30 seconds, randomly assign 2 players as captains,
            // and force join one to red and one to blue
            
            
            
        }

        private async Task ResetPickup()
        {
            // Clear teams
            // Pick new captains
            // Basically start the process over
        }

        private async Task ShowProgressInError()
        {
            await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Pickup games can only be started or reset from warm-up mode!");
        }

        private bool IsQlGameInProgress()
        {
            return _ssb.ServerInfo.CurrentServerGameState == QlGameStates.Countdown ||
                   _ssb.ServerInfo.CurrentServerGameState == QlGameStates.InProgress;
        }
    }
}
