using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model;
using SSB.Model.QlRanks;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for handling various server events.
    /// </summary>
    public class ServerEventProcessor
    {
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerEventProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ServerEventProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets the players and ids from players command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="playersText">The players text.</param>
        /// <returns></returns>
        public async Task GetPlayersAndIdsFromPlayersCmd<T>(IEnumerable<T> playersText)
        {
            var eloNeedsUpdating = new List<string>();
            foreach (T p in playersText)
            {
                string text = p.ToString();
                string player = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1);
                string id = text.Substring(0, 2).Trim();
                Debug.Write(string.Format("Found player {0} with client id {1} - setting info.\n", player, id));
                _ssb.CurrentPlayers[player] = new PlayerInfo(player, id);
                if (DoesCachedEloExist(player))
                {
                    _ssb.CurrentPlayers[player].EloData = EloCache.CachedEloData[player];
                    Debug.WriteLine(
                        "Using the cached elo data that already exists for {0}. Data - CA: {1} - CTF: {2} - DUEL: {3} - FFA: {4} -- TDM: {5}",
                        player, EloCache.CachedEloData[player].CaElo, EloCache.CachedEloData[player].CtfElo,
                        EloCache.CachedEloData[player].DuelElo, EloCache.CachedEloData[player].FfaElo,
                        EloCache.CachedEloData[player].TdmElo);
                }
                else
                {
                    _ssb.CurrentPlayers[player].EloData = new EloData();
                    eloNeedsUpdating.Add(player);
                }
            }
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();

            // Get the QLRanks info for these players
            if (eloNeedsUpdating.Any())
            {
                var retriever = new QlRanksEloRetriever();
                QlRanks d = await retriever.DoQlRanksRetrievalAsync(eloNeedsUpdating);
                SetQlRanksInfo(d);
            }
        }

        /// <summary>
        ///     Gets the players and teams from the 'configstrings' command.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public bool GetPlayersAndTeamsFromCfgString(string text)
        {
            int teamNum = 0;
            bool success = false;
            Match playerNameOnlyMatch = _ssb.Parser.CsPlayerNameOnly.Match(text);
            Match teamOnlyMatch = _ssb.Parser.CsPlayerTeamOnly.Match(text);
            if (playerNameOnlyMatch.Success && teamOnlyMatch.Success)
            {
                try
                {
                    teamNum = Convert.ToInt32(teamOnlyMatch.Value.Replace("\\t\\", ""));
                }
                catch (FormatException e)
                {
                    Debug.WriteLine(e.Message);
                }
                Team team = DetermineTeam(teamNum);
                _ssb.CurrentPlayers[playerNameOnlyMatch.Value] = new PlayerInfo(playerNameOnlyMatch.Value,
                    team,
                    string.Empty);
                Debug.WriteLine("Name, Team: {0}, {1}", playerNameOnlyMatch.Value, team);
                success = true;
            }
            else
            {
                success = false;
            }

            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
            return success;
        }

        /// <summary>
        ///     Gets the server identifier (public_id)
        /// </summary>
        /// <param name="text">The text from which to receive the server id.</param>
        /// <returns>The server's id (public_id) as a string.</returns>
        public string GetServerId(string text)
        {
            string serverId = ConsoleTextProcessor.Strip(text.Replace("sv_gtid", ""));
            //string serverId = ConsoleTextProcessor.Strip(text.Replace("sv_adXmitDelay", ""));
            Debug.WriteLine("Found server id: " + serverId);
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
            return serverId;
        }

        public void HandleMapLoad(string text)
        {
            Debug.WriteLine("Detected map load (pak info): " + text);
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
        }

        /// <summary>
        ///     Determines the team enum value from the team number.
        /// </summary>
        /// <param name="teamNum">The team number.</param>
        /// <returns>An enum value representing the team name from the team number.</returns>
        private Team DetermineTeam(int teamNum)
        {
            switch (teamNum)
            {
                case 1:
                    return Team.Red;

                case 2:
                    return Team.Blue;

                case 3:
                    return Team.Spec;

                default:
                    return Team.None;
            }
        }

        /// <summary>
        ///     Evaluates whether cached Elo information already exists for a player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns><c>true</c> if cached Elo info already exists for a player, otherwise <c>false</c>.</returns>
        private bool DoesCachedEloExist(string player)
        {
            EloData cache;
            bool exists = false;
            if (!EloCache.CachedEloData.TryGetValue(player, out cache))
            {
                EloCache.CachedEloData.Add(player, new EloData());
                return false;
            }
            if (EloCache.CachedEloData[player].CaElo != 0 && EloCache.CachedEloData[player].CtfElo != 0 &&
                EloCache.CachedEloData[player].DuelElo != 0 &&
                EloCache.CachedEloData[player].FfaElo != 0 && EloCache.CachedEloData[player].TdmElo != 0)
            {
                exists = true;
            }
            return exists;
        }

        /// <summary>
        ///     Sets the QLRanks ELo data
        /// </summary>
        /// <param name="qlr">The QlRanks object.</param>
        private void SetQlRanksInfo(QlRanks qlr)
        {
            foreach (var player in _ssb.CurrentPlayers)
            {
                // closure
                KeyValuePair<string, PlayerInfo> player1 = player;
                foreach (
                    QlRanksPlayer qp in
                        qlr.players.Where(
                            qp => qp.nick.Equals(player1.Key, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _ssb.CurrentPlayers[player.Key].EloData.CaElo = qp.ca.elo;
                    _ssb.CurrentPlayers[player.Key].EloData.CtfElo = qp.ctf.elo;
                    _ssb.CurrentPlayers[player.Key].EloData.DuelElo = qp.duel.elo;
                    _ssb.CurrentPlayers[player.Key].EloData.FfaElo = qp.ffa.elo;
                    _ssb.CurrentPlayers[player.Key].EloData.TdmElo = qp.tdm.elo;
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
    }
}