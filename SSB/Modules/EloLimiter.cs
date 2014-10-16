using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Core;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Model.QlRanks;
using SSB.Util;

namespace SSB.Modules
{
    /// <summary>
    ///     Elo limiter module.
    /// </summary>
    public class EloLimiter : ModuleManager, ISsbModule
    {
        private readonly SynServerBot _ssb;
        private readonly Users _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="EloLimiter"/> class.
        /// </summary>
        /// <param name="ssb">The main class</param>
        public EloLimiter(SynServerBot ssb)
            : base(ssb)
        {
            _ssb = ssb;
            _users = new Users();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum required Elo.
        /// </summary>
        /// <value>
        /// The maximum required Elo.
        /// </value>
        public int MaximumRequiredElo { get; set; }

        /// <summary>
        /// Gets or sets the minimum required Elo.
        /// </summary>
        /// <value>
        /// The minimum required Elo.
        /// </value>
        public int MinimumRequiredElo { get; set; }

        /// <summary>
        ///     Removes players from server who do not meet the specified Elo requirements immediately after enabling the elo limiter.
        /// </summary>
        public async Task BatchRemoveEloPlayers()
        {
            var qlrHelper = new QlRanksHelper();
            // First make sure that the elo is correct and fetch if not
            List<string> playersToUpdate = (from player in _ssb.ServerInfo.CurrentPlayers
                                            where qlrHelper.PlayerHasInvalidEloData(player.Value)
                                            select player.Key).ToList();
            if (playersToUpdate.Any())
            {
                QlRanks qlr =
                    await
                        qlrHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, playersToUpdate);
                if (qlr == null)
                {
                    _ssb.QlCommands.QlCmdSay(
                        "^3* ^1[ERROR]^7 Unable to retrieve QlRanks data. Elo limit might not be enforced. ^3*");
                }
            }
            // Kick the players from the server...
            foreach (var player in _ssb.ServerInfo.CurrentPlayers)
            {
                // Still have invalid elo data...skip.
                if (qlrHelper.PlayerHasInvalidEloData(player.Value))
                {
                    Debug.WriteLine(string.Format("Still have invalid elo data for {0}...skipping.",
                        player.Key));
                    break;
                }
                KickPlayerIfEloNotMet(player.Key);
            }
        }

        /// <summary>
        /// Checks to see if the player meets the elo requirement on connect.
        /// </summary>
        /// <param name="player">The player.</param>
        public void CheckPlayerEloRequirement(string player)
        {
            long playerElo = GetEloTypeToCompare(player);
            // Likely invalid, skip.
            if (playerElo == 0) return;
            KickPlayerIfEloNotMet(player);
        }

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        public void Load()
        {
            Load(GetType());
        }

        /// <summary>
        ///     Unloads this instance.
        /// </summary>
        public void Unload()
        {
            Unload(GetType());
        }

        /// <summary>
        ///     Gets the elo type to compare based on the server's current gametype.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The elo value to use based on the server's current gametype.</returns>
        private long GetEloTypeToCompare(string player)
        {
            PlayerInfo pi;
            if (!_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out pi)) return 0;
            long elo = 0;
            switch (_ssb.ServerInfo.CurrentGameType)
            {
                case QlGameTypes.Ca:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.CaElo;
                    break;

                case QlGameTypes.Ctf:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.CtfElo;
                    break;

                case QlGameTypes.Duel:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.DuelElo;
                    break;

                case QlGameTypes.Ffa:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.FfaElo;
                    break;

                case QlGameTypes.Tdm:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.CaElo;
                    break;
            }
            return elo;
        }

        /// <summary>
        /// Kicks the player if player does not meet the server's elo requirements.
        /// </summary>
        /// <param name="player">The player.</param>
        private void KickPlayerIfEloNotMet(string player)
        {
            bool hasMaxElo = MaximumRequiredElo != 0;
            // Elo limits don't apply to SuperUsers or higher
            if (_users.GetUserLevel(player) >= UserLevel.SuperUser) return;
            // Can't kick ourselves, though QL doesn't allow it anyway, don't show kick msg.
            if (player.Equals(_ssb.BotName, StringComparison.InvariantCultureIgnoreCase)) return;

            long playerElo = GetEloTypeToCompare(player);

            if (playerElo == 0) return;
            if (playerElo < MinimumRequiredElo)
            {
                Debug.WriteLine("{0}'s {1} Elo is less than min ({2})...Kicking.", player, _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), MinimumRequiredElo);
                _ssb.QlCommands.QlCmdKickban(player);
                _ssb.QlCommands.QlCmdSay(string.Format("^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} {4}", player, _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), playerElo, MinimumRequiredElo, hasMaxElo ? string.Format("^7Max:^1 {0}", MaximumRequiredElo) : ""));
                return;
            }
            if (!hasMaxElo) return;
            if (playerElo <= MaximumRequiredElo) return;
            Debug.WriteLine("{0}'s {1} Elo is greater than max ({2})...Kicking.", player, _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), MaximumRequiredElo);
            _ssb.QlCommands.QlCmdKickban(player);
            _ssb.QlCommands.QlCmdSay(string.Format("^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} Max:^1 {4}", player, _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), playerElo, MinimumRequiredElo, MaximumRequiredElo));
        }
    }
}