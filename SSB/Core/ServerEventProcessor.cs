using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Core.Commands.Limits;
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
        ///     Gets the name of the bot account.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The name of the account running the bot.</returns>
        public string GetBotAccountName(string text)
        {
            string name = ConsoleTextProcessor.GetCvarValue(text);
            Debug.WriteLine("The name of the account running the bot is: " + name);
            _ssb.BotName = name;
            return name;
        }

        /// <summary>
        ///     Retrieves a given player's player id (clientnum) from our internal list or
        ///     queries the server with the 'players' command and returns the id if the player is
        ///     not detected.
        /// </summary>
        /// <param name="player">The player whose id needs to be retrieved.</param>
        /// <returns>The player</returns>
        public async Task<string> GetPlayerId(string player)
        {
            PlayerInfo pinfo;
            string id = string.Empty;
            if (_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out pinfo))
            {
                Debug.WriteLine("Retrieved id {0} for player {1}", id, player);
                id = pinfo.Id;
            }
            else
            {
                // Player doesn't exist, request players from server
                await _ssb.QlCommands.QlCmdPlayers();
                // Try again
                if (!_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out pinfo)) return id;
                Debug.WriteLine("Retrieved id {0} for player {1}", id, player);
                id = pinfo.Id;
                // Only clear if we've had to use 'players' command
                _ssb.QlCommands.ClearBothQlConsoles();
            }

            return id;
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
                string playerNameOnly = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1);
                string playerAndClan = text.Substring(NthIndexOf(text, " ", 2)).Trim();
                string id = text.Substring(0, 2).Trim();
                Debug.Write(string.Format("Found player {0} with client id {1} - setting info.\n",
                    playerNameOnly, id));
                _ssb.ServerInfo.CurrentPlayers[playerNameOnly] = new PlayerInfo(playerNameOnly, playerAndClan,
                    id);

                if (qlranksHelper.DoesCachedEloExist(playerNameOnly))
                {
                    qlranksHelper.SetCachedEloData(_ssb.ServerInfo.CurrentPlayers, playerNameOnly);
                }
                else
                {
                    qlranksHelper.CreateNewPlayerEloData(_ssb.ServerInfo.CurrentPlayers, playerNameOnly);
                    eloNeedsUpdating.Add(playerNameOnly);
                }
            }
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();

            // Get the QLRanks info for these players
            if (eloNeedsUpdating.Any())
            {
                await
                    qlranksHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, eloNeedsUpdating);
            }
            // Elo limiter kick, if active
            if (EloLimit.IsLimitActive)
            {
                foreach (var player in _ssb.ServerInfo.CurrentPlayers.ToList())
                {
                    await _ssb.CommandProcessor.Limiter.EloLimit.CheckPlayerEloRequirement(player.Key);
                }
            }
            // Account date kick, if active
            if (AccountDateLimit.IsLimitActive)
            {
                await
                    _ssb.CommandProcessor.Limiter.AccountDateLimit.RunUserDateCheck(
                        _ssb.ServerInfo.CurrentPlayers);
            }
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
            _ssb.ServerInfo.CurrentServerId = serverId;
            Debug.WriteLine("Found server id: " + serverId);
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
            return serverId;
        }

        /// <summary>
        ///     Gets the players' team info from the 'configstrings' command.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
        public bool GetTeamInfoFromCfgString(string text)
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
                _ssb.ServerInfo.CurrentTeamInfo[playerNameOnlyMatch.Value] =
                    new TeamInfo(playerNameOnlyMatch.Value, team);
                Debug.WriteLine("Name, Team: {0}, {1}", playerNameOnlyMatch.Value, team);
                success = true;
            }
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
            return success;
        }

        /// <summary>
        ///     Handles the map load or change.
        /// </summary>
        /// <param name="text">The text.</param>
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
        ///     Find the n-th occurrence of a substring s.
        /// </summary>
        /// <param name="input">The input string</param>
        /// <param name="value">The value to find</param>
        /// <param name="n">The n-th occurrence.</param>
        /// <returns>The position of the n-th occurrence of the value.</returns>
        /// <remarks>
        ///     Taken from Alexander PRokofyev's answer at:
        ///     http://stackoverflow.com/a/187394
        /// </remarks>
        private int NthIndexOf(string input, string value, int n)
        {
            Match m = Regex.Match(input, "((" + value + ").*?){" + n + "}");

            if (m.Success)
            {
                return m.Groups[2].Captures[n - 1].Index;
            }
            return -1;
        }
    }
}