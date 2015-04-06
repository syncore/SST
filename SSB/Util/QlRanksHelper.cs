using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Database;
using SSB.Model;
using SSB.Model.QlRanks;

namespace SSB.Util
{
    /// <summary>
    ///     Helper class for various QlRanks-related functions.
    /// </summary>
    public class QlRanksHelper
    {
        private readonly DbElo _eloDb;

        /// <summary>
        /// Initializes a new instance of the <see cref="QlRanksHelper"/> class.
        /// </summary>
        public QlRanksHelper()
        {
            _eloDb = new DbElo();
        }

        /// <summary>
        /// Creates the new player elo data.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <param name="player">The player.</param>
        public void CreateNewPlayerEloData(Dictionary<string, PlayerInfo> currentPlayers, string player)
        {
            if (!Helpers.KeyExists(player, currentPlayers)) return;
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
            return _eloDb.UserAlreadyExists(shortPlayerName) && IsValidEloDatabaseData(shortPlayerName);
        }

        /// <summary>
        /// Determines whether the cached elo data for the specified user is too old,
        /// based on a value set in the core configuration options.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the cached elo data is outdated, otherwise <c>false</c>.</returns>
        public bool IsCachedEloDataOutdated(string user)
        {
            if (!DoesCachedEloExist(user)) return true;
            var edata = _eloDb.GetEloData(user);
            return DateTime.Now > edata.LastUpdatedDate.AddMinutes(GetExpirationFromConfig());
        }

        /// <summary>
        ///     Checks whether the players the has invalid elo data.
        /// </summary>
        /// <param name="pinfo">The pinfo.</param>
        /// <returns><c>true</c> if the player has invalid elo data, otherwise <c>false</c>.</returns>
        public bool PlayerHasInvalidEloData(PlayerInfo pinfo)
        {
            if (pinfo.EloData == null) return true;
            if (pinfo.EloData.CaElo == 0 && pinfo.EloData.CtfElo == 0 && pinfo.EloData.DuelElo == 0 &&
                pinfo.EloData.FfaElo == 0 && pinfo.EloData.TdmElo == 0)
            {
                Debug.WriteLine(string.Format("Player: {0} has invalid elo data.", pinfo.ShortName));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Asynchronously retrieves the elo data from QLRanks API.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <param name="playersToUpdate">The players to update.</param>
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
            currentPlayers[shortPlayerName].EloData = _eloDb.GetEloData(shortPlayerName);
        }

        /// <summary>
        /// Indicates where an elo update should be skipped or not.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="currentPlayers">The current players.</param>
        /// <returns><c>true</c> if the elo update should be skipped, otherwise <c>false</c>.</returns>
        public bool ShouldSkipEloUpdate(string player, Dictionary<string, PlayerInfo> currentPlayers)
        {
            if ((Helpers.KeyExists(player, currentPlayers) && (!PlayerHasInvalidEloData(currentPlayers[player]))))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the elo expiration time from the configuration.
        /// </summary>
        private uint GetExpirationFromConfig()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.VerifyConfigLocation();
            cfgHandler.ReadConfiguration();
            
            return cfgHandler.Config.CoreOptions.eloCacheExpiration;
        }

        /// <summary>
        /// Determines whether valid elo data exists for the specified player from the elo database.
        /// </summary>
        /// <param name="shortPlayerName">Short name of the player.</param>
        /// <returns><c>true</c>if valid elo data exists, otherwise <c>false</c>.</returns>
        private bool IsValidEloDatabaseData(string shortPlayerName)
        {
            var edata = _eloDb.GetEloData(shortPlayerName);
            return edata != null && (edata.CaElo != 0 && edata.CtfElo != 0 && edata.DuelElo != 0 && edata.FfaElo != 0 && edata.TdmElo != 0);
        }

        /// <summary>
        /// Sets the QLRanks ELo data for the group of players.
        /// </summary>
        /// <param name="currentPlayers">The current players on the server.</param>
        /// <param name="qlr">The QlRanks object.</param>
        private void SetQlRanksInfo(Dictionary<string, PlayerInfo> currentPlayers, QlRanks qlr)
        {
            foreach (var p in qlr.players)
            {
                // closure; compiler compliance
                var player = p;
                foreach (var c in currentPlayers.Keys.Where(c => player.nick.Equals(c, StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (currentPlayers[c].EloData == null)
                    {
                        currentPlayers[c].EloData = new EloData();
                    }

                    currentPlayers[c].EloData.CaElo = p.ca.elo;
                    currentPlayers[c].EloData.CtfElo = p.ctf.elo;
                    currentPlayers[c].EloData.DuelElo = p.duel.elo;
                    currentPlayers[c].EloData.FfaElo = p.ffa.elo;
                    currentPlayers[c].EloData.TdmElo = p.tdm.elo;
                    Debug.WriteLine(
                        "Set {0}'s elo data to: [CA]: {1} - [CTF]: {2} - [DUEL]: {3} - [FFA]: {4} - [TDM]: {5}",
                        c, p.ca.elo, p.ctf.elo, p.duel.elo, p.ffa.elo, p.tdm.elo);

                    if (_eloDb.UserAlreadyExists(c))
                    {
                        _eloDb.UpdateEloData(c, DateTime.Now, p.ca.elo, p.ctf.elo, p.duel.elo, p.ffa.elo,
                            p.tdm.elo);
                    }
                    else
                    {
                        _eloDb.AddUserToDb(c, DateTime.Now, p.ca.elo, p.ctf.elo, p.duel.elo, p.ffa.elo,
                            p.tdm.elo);
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
                if (currentPlayers[currentPlayer].EloData == null)
                {
                    currentPlayers[currentPlayer].EloData = new EloData();
                }

                currentPlayers[currentPlayer].EloData.CaElo = qp.ca.elo;
                currentPlayers[currentPlayer].EloData.CtfElo = qp.ctf.elo;
                currentPlayers[currentPlayer].EloData.DuelElo = qp.duel.elo;
                currentPlayers[currentPlayer].EloData.FfaElo = qp.ffa.elo;
                currentPlayers[currentPlayer].EloData.TdmElo = qp.tdm.elo;
                Debug.WriteLine(
                    "Set {0}'s elo data to: [CA]: {1} - [CTF]: {2} - [DUEL]: {3} - [FFA]: {4} - [TDM]: {5}",
                    currentPlayer, qp.ca.elo, qp.ctf.elo, qp.duel.elo, qp.ffa.elo, qp.tdm.elo);

                if (_eloDb.UserAlreadyExists(currentPlayer))
                {
                    _eloDb.UpdateEloData(currentPlayer, DateTime.Now, qp.ca.elo, qp.ctf.elo, qp.duel.elo, qp.ffa.elo,
                        qp.tdm.elo);
                }
                else
                {
                    _eloDb.AddUserToDb(currentPlayer, DateTime.Now, qp.ca.elo, qp.ctf.elo, qp.duel.elo, qp.ffa.elo,
                        qp.tdm.elo);
                }
            }
        }
    }
}