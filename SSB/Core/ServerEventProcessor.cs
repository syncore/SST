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
        ///     Gets the type of game running on this server.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The gametype as a <see cref="QlGameTypes" /> enum.</returns>
        public QlGameTypes GetGameType(string text)
        {
            var gametype = QlGameTypes.Unspecified;
            string gt = ConsoleTextProcessor.GetCvarValue(text);
            int gtnum;
            bool isNum = (int.TryParse(gt, out gtnum));
            if (isNum)
            {
                if (gtnum == 0)
                {
                    // Special case for FFA
                    gametype = (QlGameTypes)999;
                }
                else
                {
                    gametype = (QlGameTypes)gtnum;
                }
            }
            Debug.WriteLine("This server's gametype is: " + gametype);
            //Set
            _ssb.ServerInfo.CurrentGameType = gametype;
            return gametype;
        }

        /// <summary>
        ///     Retrieves a given player's player id (clientnum) from our internal list or
        ///     queries the server with the 'players' command and returns the id if the player is
        ///     not detected.
        /// </summary>
        /// <param name="player">The player whose id needs to be retrieved.</param>
        /// <returns>The player</returns>
        public string GetPlayerId(string player)
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
                _ssb.QlCommands.QlCmdPlayers();
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
                string player = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1);
                string id = text.Substring(0, 2).Trim();
                Debug.Write(string.Format("Found player {0} with client id {1} - setting info.\n", player, id));
                _ssb.ServerInfo.CurrentPlayers[player] = new PlayerInfo(player, id);
                if (qlranksHelper.DoesCachedEloExist(player))
                {
                    qlranksHelper.SetCachedEloData(_ssb.ServerInfo.CurrentPlayers, player);
                }
                else
                {
                    qlranksHelper.CreateNewPlayerEloData(_ssb.ServerInfo.CurrentPlayers, player);
                    eloNeedsUpdating.Add(player);
                }
            }
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();

            // Get the QLRanks info for these players
            if (eloNeedsUpdating.Any())
            {
                await
                    qlranksHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, eloNeedsUpdating);
                // Elo limiter kick, if active
                if (_ssb.CommandProcessor.Limiter.EloLimit.IsLimitActive)
                {
                    foreach (string player in eloNeedsUpdating)
                    {
                        _ssb.CommandProcessor.Limiter.EloLimit.CheckPlayerEloRequirement(player);
                    }
                }
            }
            // Account date kick, if active
            if (_ssb.CommandProcessor.Limiter.AccountDateLimit.IsLimitActive)
            {
                await _ssb.CommandProcessor.Limiter.AccountDateLimit.RunUserDateCheck(_ssb.ServerInfo.CurrentPlayers);
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
                _ssb.ServerInfo.CurrentPlayers[playerNameOnlyMatch.Value] =
                    new PlayerInfo(playerNameOnlyMatch.Value,
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
    }
}