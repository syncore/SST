using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model;
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
            var qlranksHelper = new QlRanksHelper();
            foreach (T p in playersText)
            {
                string text = p.ToString();
                string player = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1);
                string id = text.Substring(0, 2).Trim();
                Debug.Write(string.Format("Found player {0} with client id {1} - setting info.\n", player, id));
                _ssb.CurrentPlayers[player] = new PlayerInfo(player, id);
                if (qlranksHelper.DoesCachedEloExist(player))
                {
                    qlranksHelper.SetCachedEloData(_ssb.CurrentPlayers, player);
                }
                else
                {
                    qlranksHelper.CreateNewPlayerEloData(_ssb.CurrentPlayers, player);
                    eloNeedsUpdating.Add(player);
                }
            }
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();

            // Get the QLRanks info for these players
            if (eloNeedsUpdating.Any())
            {
                await qlranksHelper.RetrieveEloDataFromApiAsync(_ssb.CurrentPlayers, eloNeedsUpdating);
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
    }
}