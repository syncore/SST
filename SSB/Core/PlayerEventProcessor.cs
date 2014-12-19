using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Model;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for handling player events.
    /// </summary>
    public class PlayerEventProcessor
    {
        private readonly QlRanksHelper _qlRanksHelper;
        private readonly SeenDates _seenDb;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerEventProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PlayerEventProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _qlRanksHelper = new QlRanksHelper();
            _seenDb = new SeenDates();
        }

        /// <summary>
        ///     Handles the player connection.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandleIncomingPlayerConnection(string player)
        {
            _seenDb.UpdateLastSeenDate(player, DateTime.Now);

            if ((Tools.KeyExists(player, _ssb.ServerInfo.CurrentPlayers) &&
                 (!_qlRanksHelper.ShouldSkipEloUpdate(player, _ssb.ServerInfo.CurrentPlayers))))
            {
                await HandleEloUpdate(player);
            }
            // Elo limiter kick, if active
            if (_ssb.Mod.EloLimit.Active)
            {
                await _ssb.Mod.EloLimit.CheckPlayerEloRequirement(player);
            }
            // Account date kick, if active
            if (_ssb.Mod.AccountDateLimit.Active)
            {
                await _ssb.Mod.AccountDateLimit.RunUserDateCheck(player);
            }
            // Check for time-bans
            var autoBanner = new PlayerAutoBanner(_ssb);
            await autoBanner.CheckForBans(player);

            Debug.WriteLine("Detected incoming connection for " + player);
        }

        /// <summary>
        ///     Handles the outgoing player connection, either by disconnect or kick.
        /// </summary>
        /// <param name="player">The player.</param>
        public void HandleOutgoingPlayerConnection(string player)
        {
            // Remove player from our internal list
            RemovePlayer(player);
            Debug.WriteLine("Detected outgoing connection for " + player);
        }

        /// <summary>
        ///     Handles the player chat message.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandlePlayerChatMessage(string text)
        {
            string msgContent =
                ConsoleTextProcessor.Strip(text.Substring(text.IndexOf(": ", StringComparison.Ordinal) + 1))
                    .ToLowerInvariant();

            string name = text.Substring(0, text.LastIndexOf('\u0019'));
            string msgFrom;

            if (name.LastIndexOf(" ", StringComparison.Ordinal) != -1)
            {
                // Has clan tag; get name only
                msgFrom = name.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1,
                    text.LastIndexOf('\u0019')).ToLowerInvariant();
            }
            else
            {
                // No clan tag; get name only
                msgFrom = name;
            }

            //string msgFrom = text.Substring(0, text.LastIndexOf('\u0019'));
            //string msgFrom = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1,
            //    text.LastIndexOf('\u0019')).ToLowerInvariant();
            Debug.WriteLine("** Detected chat message {0} from {1} **", msgContent, msgFrom);
            // Check to see if chat message is a valid command
            if (msgContent.StartsWith(CommandProcessor.BotCommandPrefix))
            {
                // Synchronous
                // ReSharper disable once UnusedVariable
                Task s = _ssb.CommandProcessor.ProcessBotCommand(msgFrom, msgContent);
            }
        }

        /// <summary>
        ///     Handles the player's configuration string.
        /// </summary>
        /// <param name="m">The match.</param>
        public void HandlePlayerConfigString(Match m)
        {
            string idMatchText = m.Groups["id"].Value;
            string playerInfoMatchText = m.Groups["playerinfo"].Value.Replace("\"", "");

            string[] pi = playerInfoMatchText.Split('\\');
            if (pi.Length == 0)
            {
                Debug.WriteLine("Invalid player info array length.");
                return;
            }
            string playername = GetCsValue("n", pi);
            int status;
            int tm;
            int.TryParse(GetCsValue("t", pi), out tm);
            int.TryParse(GetCsValue("rp", pi), out status);
            var ready = (ReadyStatus) status;
            var team = (Team) tm;
            PlayerInfo p;
            // Player already exists... Update if necessary.
            if (_ssb.ServerInfo.CurrentPlayers.TryGetValue(playername, out p))
            {
                //Debug.WriteLine(string.Format("{0} already exists, updating player info if necessary.", playername));
                if (p.Ready != ready)
                {
                    UpdatePlayerReadyStatus(playername, ready);
                }
                if (p.Team != team)
                {
                    UpdatePlayerTeam(playername, team);
                }
            }
            else
            {
                CreateNewPlayerFromConfigString(idMatchText, pi);
            }
        }

        /// <summary>
        ///     Gets the corresponding value associated with a player's configstring.
        /// </summary>
        /// <param name="term">The term to find.</param>
        /// <param name="arr">The array containing the playerinfo.</param>
        /// <returns>
        ///     The corresponding value associated with a player's config string. i.e. Using 'n' as the term
        ///     will return the player's name, using 'cn' as the term will return the clan tag, if any. If not found,
        ///     then an empty string will be returned.
        /// </returns>
        private static string GetCsValue(string term, string[] arr)
        {
            string foundVal = string.Empty;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Equals(term, StringComparison.InvariantCultureIgnoreCase))
                {
                    foundVal = arr[i + 1];
                }
            }
            return foundVal;
        }

        /// <summary>
        ///     Creates the new player from the configuration string.
        /// </summary>
        /// <param name="idText">The player id as a string.</param>
        /// <param name="pi">The player info array.</param>
        private void CreateNewPlayerFromConfigString(string idText, string[] pi)
        {
            if (pi.Length <= 1) return;
            int id;
            if (!int.TryParse(idText, out id))
            {
                Debug.WriteLine("Unable to determine player's id from cs info.");
                return;
            }
            id = (id - 29);
            int tm;
            if (!int.TryParse(GetCsValue("t", pi), out tm))
            {
                Debug.WriteLine(string.Format("Unable to determine team info for player {0} from cs info.",
                    GetCsValue("n", pi)));
                return;
            }
            string playername = GetCsValue("n", pi);
            string clantag = GetCsValue("cn", pi);
            string subscriber = GetCsValue("su", pi);
            string fullclanname = GetCsValue("xcn", pi);
            string country = GetCsValue("c", pi);

            // Create player. Also Set misc details like full clan name, country code, subscription status.
            _ssb.ServerInfo.CurrentPlayers[playername] = new PlayerInfo(playername, clantag, (Team) tm,
                id) {Subscriber = subscriber, FullClanName = fullclanname, CountryCode = country};
            Debug.Write(
                string.Format(
                    "[NEWPLAYER(CS)]: Detected player {0} - Country: {1} - Tag: {2} - (Clan: {3}) - Pro: {4} - \n",
                    playername, country, clantag, fullclanname, subscriber));
        }

        /// <summary>
        ///     Handles the QLRanks elo update if necessary.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task HandleEloUpdate(string player)
        {
            PlayerInfo p;
            if (!_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out p)) return;
            if (_qlRanksHelper.DoesCachedEloExist(player))
            {
                _qlRanksHelper.SetCachedEloData(_ssb.ServerInfo.CurrentPlayers, player);
            }
            else
            {
                _qlRanksHelper.CreateNewPlayerEloData(_ssb.ServerInfo.CurrentPlayers, player);
                await
                    _qlRanksHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, player);
            }
        }

        /// <summary>
        ///     Removes the player from the current in-game players.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        private void RemovePlayer(string player)
        {
            // Remove from players
            if (_ssb.ServerInfo.CurrentPlayers.Remove(player))
            {
                Debug.WriteLine(string.Format("Removed {0} from the current in-game players.", player));
            }
            else
            {
                Debug.WriteLine(
                    string.Format(
                        "Unable to remove {0} from the current in-game players. Player was not in list of current in-game players.",
                        player));
            }
        }

        /// <summary>
        ///     Updates the player's ready status.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="status">The status.</param>
        private void UpdatePlayerReadyStatus(string player, ReadyStatus status)
        {
            _ssb.ServerInfo.CurrentPlayers[player].Ready = status;
            Debug.WriteLine("Updated {0}'s player status to: {1}", player, status);
        }

        /// <summary>
        ///     Updates the player's team.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        private void UpdatePlayerTeam(string player, Team team)
        {
            _ssb.ServerInfo.CurrentPlayers[player].Team = team;
            Debug.WriteLine("Updated {0}'s team to: {1}", player, team);
        }
    }
}