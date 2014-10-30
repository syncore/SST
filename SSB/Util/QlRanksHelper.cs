using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Model;
using SSB.Model.QlRanks;

namespace SSB.Util
{
    /// <summary>
    ///     Helper class for various QlRanks-related functions.
    /// </summary>
    public class QlRanksHelper
    {
        /// <summary>
        /// Creates the new player elo data.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <param name="player">The player.</param>
        public void CreateNewPlayerEloData(Dictionary<string, PlayerInfo> currentPlayers, string player)
        {
            if (!Tools.KeyExists(player, currentPlayers)) return;
            currentPlayers[player].EloData = new EloData();
        }

        /// <summary>
        /// Evaluates whether cached Elo information already exists for a player.
        /// </summary>
        /// <param name="shortPlayerName">Short name of the player (not including clan tag).</param>
        /// <returns>
        ///   <c>true</c> if cached Elo info already exists for a player, otherwise <c>false</c>.
        /// </returns>
        public bool DoesCachedEloExist(string shortPlayerName)
        {
            bool exists = false;
            if (!Tools.KeyExists(shortPlayerName, EloCache.CachedEloData))
            {
                EloCache.CachedEloData.Add(shortPlayerName, new EloData());
                return false;
            }
            if (EloCache.CachedEloData[shortPlayerName].CaElo != 0 && EloCache.CachedEloData[shortPlayerName].CtfElo != 0 &&
                EloCache.CachedEloData[shortPlayerName].DuelElo != 0 &&
                EloCache.CachedEloData[shortPlayerName].FfaElo != 0 && EloCache.CachedEloData[shortPlayerName].TdmElo != 0)
            {
                exists = true;
            }
            return exists;
        }

        /// <summary>
        ///     Checks whether the players the has invalid elo data.
        /// </summary>
        /// <param name="pinfo">The pinfo.</param>
        /// <returns><c>true</c> if the player has invalid elo data, otherwise <c>false</c>.</returns>
        public bool PlayerHasInvalidEloData(PlayerInfo pinfo)
        {
            bool invalid = false;
            if (pinfo.EloData == null) return true;
            if (pinfo.EloData.CaElo == 0 && pinfo.EloData.CtfElo == 0 && pinfo.EloData.DuelElo == 0 &&
                pinfo.EloData.FfaElo == 0 && pinfo.EloData.TdmElo == 0)
            {
                Debug.WriteLine(string.Format("Player: {0} has invalid elo data.", pinfo.ShortName));
                invalid = true;
            }
            return invalid;
        }

        /// <summary>
        /// Asynchronously retrieves the elo data from QLRanks API.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <param name="playersToUpdate">The players to update.</param>
        /// <returns></returns>
        public async Task<QlRanks> RetrieveEloDataFromApiAsync(Dictionary<string, PlayerInfo> currentPlayers, List<string> playersToUpdate)
        {
            if (playersToUpdate.Count == 0) return null;

            var retriever = new QlRanksEloRetriever();
            QlRanks d = await retriever.DoQlRanksRetrievalAsync(playersToUpdate);
            if (d != null)
            {
                SetQlRanksInfo(currentPlayers, d);
            }
            else
            {
                Debug.WriteLine("QLRANKS: Error: object was null which indicates a problem with retrieval...");
            }
            return d;
        }

        /// <summary>
        /// Asynchronously retrieves the elo data from QLRanks API.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <returns></returns>
        public async Task<QlRanks> RetrieveEloDataFromApiAsync(Dictionary<string, PlayerInfo> currentPlayers, string playerToUpdate)
        {
            if (string.IsNullOrEmpty(playerToUpdate)) return null;

            var retriever = new QlRanksEloRetriever();
            QlRanks d = await retriever.DoQlRanksRetrievalAsync(playerToUpdate);
            if (d != null)
            {
                SetQlRanksInfo(currentPlayers, playerToUpdate, d);
            }
            else
            {
                Debug.WriteLine("QLRANKS: Error: object was null which indicates a problem with retrieval...");
            }
            return d;
        }

        /// <summary>
        /// Sets the cached elo data.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <param name="shortPlayerName">Short name of the player (excluding clan tag).</param>
        public void SetCachedEloData(Dictionary<string, PlayerInfo> currentPlayers, string shortPlayerName)
        {
            if (!DoesCachedEloExist(shortPlayerName)) return;
            currentPlayers[shortPlayerName].EloData = EloCache.CachedEloData[shortPlayerName];
            //Debug.WriteLine(
            //    "Using the cached elo data that already exists for {0}. Data - CA: {1} - CTF: {2} - DUEL: {3} - FFA: {4} -- TDM: {5}",
            //    shortPlayerName, EloCache.CachedEloData[shortPlayerName].CaElo, EloCache.CachedEloData[shortPlayerName].CtfElo,
            //    EloCache.CachedEloData[shortPlayerName].DuelElo, EloCache.CachedEloData[shortPlayerName].FfaElo,
            //    EloCache.CachedEloData[shortPlayerName].TdmElo);
        }

        /// <summary>
        /// Indicates where an elo update should be skipped or not.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="currentPlayers">The current players.</param>
        /// <returns><c>true</c> if the elo update should be skipped, otherwise <c>false</c>.</returns>
        public bool ShouldSkipEloUpdate(string player, Dictionary<string, PlayerInfo> currentPlayers)
        {
            if ((Tools.KeyExists(player, currentPlayers) && (!PlayerHasInvalidEloData(currentPlayers[player]))))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the QLRanks ELo data for the group of players.
        /// </summary>
        /// <param name="currentPlayers">The current players on the server.</param>
        /// <param name="qlr">The QlRanks object.</param>
        private void SetQlRanksInfo(Dictionary<string, PlayerInfo> currentPlayers, QlRanks qlr)
        {
            foreach (var player in currentPlayers)
            {
                // closure
                KeyValuePair<string, PlayerInfo> player1 = player;
                foreach (
                    QlRanksPlayer qp in
                        qlr.players.Where(
                            qp => qp.nick.Equals(player1.Key, StringComparison.InvariantCultureIgnoreCase)))
                {
                    currentPlayers[player.Key].EloData.CaElo = qp.ca.elo;
                    currentPlayers[player.Key].EloData.CtfElo = qp.ctf.elo;
                    currentPlayers[player.Key].EloData.DuelElo = qp.duel.elo;
                    currentPlayers[player.Key].EloData.FfaElo = qp.ffa.elo;
                    currentPlayers[player.Key].EloData.TdmElo = qp.tdm.elo;
                    Debug.WriteLine(
                        "Set {0}'s elo data to: [CA]: {1} - [CTF]: {2} - [DUEL]: {3} - [FFA]: {4} - [TDM]: {5}",
                        player.Key, qp.ca.elo, qp.ctf.elo, qp.duel.elo, qp.ffa.elo, qp.tdm.elo);
                    if (!DoesCachedEloExist(player.Key))
                    {
                        EloCache.CachedEloData[player.Key].CaElo = qp.ca.elo;
                        EloCache.CachedEloData[player.Key].CtfElo = qp.ctf.elo;
                        EloCache.CachedEloData[player.Key].DuelElo = qp.duel.elo;
                        EloCache.CachedEloData[player.Key].FfaElo = qp.ffa.elo;
                        EloCache.CachedEloData[player.Key].TdmElo = qp.tdm.elo;
                    }
                }
            }
        }

        /// <summary>
        /// Sets QLRanks Elo data for a single player.
        /// </summary>
        /// <param name="currentPlayers">The current players on the server</param>
        /// <param name="currentPlayer">The current player to update</param>
        /// <param name="qlr">The QlRanks object.</param>
        private void SetQlRanksInfo(Dictionary<string, PlayerInfo> currentPlayers, string currentPlayer, QlRanks qlr)
        {
            foreach (
                    QlRanksPlayer qp in
                        qlr.players.Where(
                            qp => qp.nick.Equals(currentPlayer, StringComparison.InvariantCultureIgnoreCase)))
            {
                currentPlayers[currentPlayer].EloData.CaElo = qp.ca.elo;
                currentPlayers[currentPlayer].EloData.CtfElo = qp.ctf.elo;
                currentPlayers[currentPlayer].EloData.DuelElo = qp.duel.elo;
                currentPlayers[currentPlayer].EloData.FfaElo = qp.ffa.elo;
                currentPlayers[currentPlayer].EloData.TdmElo = qp.tdm.elo;
                Debug.WriteLine(
                    "Set {0}'s elo data to: [CA]: {1} - [CTF]: {2} - [DUEL]: {3} - [FFA]: {4} - [TDM]: {5}",
                    currentPlayer, qp.ca.elo, qp.ctf.elo, qp.duel.elo, qp.ffa.elo, qp.tdm.elo);
                if (!DoesCachedEloExist(currentPlayer))
                {
                    EloCache.CachedEloData[currentPlayer].CaElo = qp.ca.elo;
                    EloCache.CachedEloData[currentPlayer].CtfElo = qp.ctf.elo;
                    EloCache.CachedEloData[currentPlayer].DuelElo = qp.duel.elo;
                    EloCache.CachedEloData[currentPlayer].FfaElo = qp.ffa.elo;
                    EloCache.CachedEloData[currentPlayer].TdmElo = qp.tdm.elo;
                }
            }
        }
    }
}