using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Core;
using SSB.Interfaces;
using SSB.Util;

namespace SSB.Modules
{
    /// <summary>
    ///     Elo limiter module.
    /// </summary>
    public class EloLimiter : ModuleManager, ISsbModule
    {
        private readonly SynServerBot _ssb;

        public EloLimiter(SynServerBot ssb)
            : base(ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Loads this instance.
        /// </summary>
        public void Load()
        {
            Load(GetType());
        }

        /// <summary>
        /// Removes players from server who do not meet the specified Elo requirements.
        /// </summary>
        public async Task RemoveEloPlayers(int minElo, int maxElo)
        {
            bool hasMaxElo = maxElo != 0;
            var qlrHelper = new QlRanksHelper();
            // First make sure that the elo is correct and fetch if not
            List<string> playersToUpdate = (from player in _ssb.CurrentPlayers where qlrHelper.PlayerHasInvalidEloData(player.Value) select player.Key).ToList();
            if (playersToUpdate.Any())
            {
                await qlrHelper.RetrieveEloDataFromApiAsync(_ssb.CurrentPlayers, playersToUpdate);
            }
            // Kick the players from the server...
            // TODO: Gametype, not CTF
            foreach (var player in _ssb.CurrentPlayers)
            {
                // Still have invalid elo data...skip.
                if (qlrHelper.PlayerHasInvalidEloData(player.Value))
                {
                    Debug.WriteLine(string.Format("Still have invalid elo data for {0}...skipping.", player.Key));
                    break;
                }
                if (player.Value.EloData.CtfElo < minElo)
                {
                    Debug.WriteLine("{0}'s Elo is less than min ({1})...Kicking.", player.Key, minElo);
                    _ssb.QlCommands.QlCmdKick(player.Key);
                }
                if (!hasMaxElo) continue;
                if (player.Value.EloData.CtfElo <= maxElo) continue;
                Debug.WriteLine("{0}'s Elo is greater than max ({1})...Kicking.", player.Key, maxElo);
                _ssb.QlCommands.QlCmdKick(player.Key);
            }
        }

        /// <summary>
        /// Unloads this instance.
        /// </summary>
        public void Unload()
        {
            Unload(GetType());
        }
    }
}