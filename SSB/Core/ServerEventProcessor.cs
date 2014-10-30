using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Core.Commands.Limits;
using SSB.Database;
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
        private readonly SeenDates _seenDb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerEventProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ServerEventProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _seenDb = new SeenDates();
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
        public async Task<int> GetPlayerId(string player)
        {
            PlayerInfo pinfo;
            int id = -1;
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
        ///     Handles the player information from the 'players' command and performs various actions based on the data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="playersText">The players text.</param>
        public async Task HandlePlayersAndIdsFromPlayersCmd<T>(IEnumerable<T> playersText)
        {
            var eloNeedsUpdating = new List<string>();
            var qlranksHelper = new QlRanksHelper();
            foreach (T p in playersText)
            {
                string text = p.ToString();
                string playerNameOnly = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1);
                //string playerAndClan = text.Substring(Tools.NthIndexOf(text, " ", 2)).Trim();
                int sndspace = (Tools.NthIndexOf(text, " ", 2));
                int clanLength = ((text.LastIndexOf(" ", StringComparison.Ordinal) - sndspace));
                string clan = text.Substring(sndspace, (clanLength)).Trim();
                string idText = text.Substring(0, 2).Trim();
                int id;
                if (!int.TryParse(idText, out id))
                {
                    Debug.WriteLine("Unable to extract player's ID from players.");
                    return;
                }
                Debug.Write(string.Format("Found player {0} with client id {1} - setting info.\n",
                    playerNameOnly, id));
                _ssb.ServerInfo.CurrentPlayers[playerNameOnly] = new PlayerInfo(playerNameOnly, clan, Team.None, 
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
                // Store user's last seen date
                _seenDb.UpdateLastSeenDate(playerNameOnly, DateTime.Now);
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
            await CheckEloAgainstLimit(_ssb.ServerInfo.CurrentPlayers);
            // Account date kick, if active
            await CheckAccountDateAgainstLimit(_ssb.ServerInfo.CurrentPlayers);
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
        /// Checks the player's account registration date against date limit, if date limit is active.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <returns></returns>
        private async Task CheckAccountDateAgainstLimit(Dictionary<string, PlayerInfo> players)
        {
            if (AccountDateLimit.IsLimitActive)
            {
                await
                    _ssb.CommandProcessor.Limiter.AccountDateLimit.RunUserDateCheck(
                        players);
            }
        }

        /// <summary>
        /// Checks the player Elo against Elo limit, if Elo limiter is active.
        /// </summary>
        /// <param name="players">The players.</param>
        private async Task CheckEloAgainstLimit(Dictionary<string, PlayerInfo> players)
        {
            if (EloLimit.IsLimitActive)
            {
                foreach (var player in players.ToList())
                {
                    await _ssb.CommandProcessor.Limiter.EloLimit.CheckPlayerEloRequirement(player.Key);
                }
            }
        }

        

        
    }
}