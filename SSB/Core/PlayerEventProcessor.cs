using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Core.Modules;
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
        private readonly DbSeenDates _seenDb;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerEventProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PlayerEventProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _qlRanksHelper = new QlRanksHelper();
            _seenDb = new DbSeenDates();
        }

        /// <summary>
        ///     Handles the player connection.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandleIncomingPlayerConnection(string player)
        {
            // "/?" command, regex would otherwise match, so ignore
            if (player.Equals("players show currently", StringComparison.InvariantCultureIgnoreCase)) return;

            // Player connections include the clan tag, so we need to remove it
            player = Helpers.GetStrippedName(player);

            _seenDb.UpdateLastSeenDate(player, DateTime.Now);

            if ((Helpers.KeyExists(player, _ssb.ServerInfo.CurrentPlayers) &&
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
            // Pickup module: notify connecting player of the game signup process
            if (_ssb.Mod.Pickup.Active)
            {
                await _ssb.Mod.Pickup.Manager.NotifyConnectingUser(player);
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
        public async Task HandleOutgoingPlayerConnection(string player)
        {
            player = player.ToLowerInvariant();
            
            // The outgoing player was actually in the game, and not a spectator
            bool outgoingWasActive = _ssb.ServerInfo.IsActivePlayer(player);
            // Get the outgoing player's team before he disconnected
            var outgoingTeam = _ssb.ServerInfo.CurrentPlayers[player].Team;

            // Evaluate the player's no-show/sub status for pickup module, if active
            if (_ssb.Mod.Pickup.Active)
            {
                await _ssb.Mod.Pickup.Manager.EvalOutgoingPlayer(player, outgoingWasActive, outgoingTeam);
                _ssb.Mod.Pickup.Manager.RemoveActivePickupPlayer(player);
                _ssb.Mod.Pickup.Manager.RemoveEligibility(player);
            }

            // Remove player from our internal list
            RemovePlayer(player);

            // If player was/is currently being spectated, clear the internal tracker
            if (!string.IsNullOrEmpty(_ssb.ServerInfo.PlayerCurrentlyFollowing)
                && _ssb.ServerInfo.PlayerCurrentlyFollowing.Equals(player))
            {
                _ssb.ServerInfo.PlayerCurrentlyFollowing = string.Empty;
            }

            Debug.WriteLine("Detected outgoing connection for " + player);

            // Evaluate player's early quit situation if that module is active
            if (!_ssb.Mod.EarlyQuit.Active) return;
            if (!outgoingWasActive) return;
            var eqh = new EarlyQuitHandler(_ssb);
            await eqh.EvalCountdownQuitter(player);
            await eqh.EvalInProgressQuitter(player);
        }

        /// <summary>
        ///     Handles the player accuracy data.
        /// </summary>
        /// <param name="m">The match.</param>
        public void HandlePlayerAccuracyData(Match m)
        {
            string player = _ssb.ServerInfo.PlayerCurrentlyFollowing;
            if (string.IsNullOrEmpty(player))
            {
                Debug.WriteLine(
                    "Accuracy data detected but is of no use because no player is currently being followed. Ignoring.");
                return;
            }
            if (!Helpers.KeyExists(player, _ssb.ServerInfo.CurrentPlayers))
            {
                Debug.WriteLine(
                    "Player currently being followed is no longer on the server/doesn't exist in internal list. Ignoring.");
                return;
            }
            string extracted = m.Groups["accdata"].Value;
            string[] accstrings = extracted.Split(' ');
            if (accstrings.Length < 15)
            {
                Debug.WriteLine("Invalid accuracy data array length.");
                return;
            }
            var accs = new int[accstrings.Length];
            for (int k = 0; k < accstrings.Length; k++)
            {
                int n;
                int.TryParse(accstrings[k], out n);
                accs[k] = n;
            }
            var playerAccInfo = new AccuracyInfo
            {
                MachineGun = accs[2],
                ShotGun = accs[3],
                GrenadeLauncher = accs[4],
                RocketLauncher = accs[5],
                LightningGun = accs[6],
                RailGun = accs[7],
                PlasmaGun = accs[8],
                Bfg = accs[9],
                GrapplingHook = accs[10],
                NailGun = accs[11],
                ProximityMineLauncher = accs[12],
                ChainGun = accs[13],
                HeavyMachineGun = accs[14]
            };
            // Set
            _ssb.ServerInfo.CurrentPlayers[player].Acc = playerAccInfo;
            Debug.WriteLine(
                "Set accuracy data for currently followed player: {0}. Data: [MG] {1} | [SG] {2}" +
                " | [GL] {3} | [RL] {4} | [LG] {5} | [RG] {6} | [PG] {7} | [BFG] {8} | [GH] {9} |" +
                " [NG] {10} | [PRX] {11} | [CG] {12} | [HMG] {13}", player, playerAccInfo.MachineGun,
                playerAccInfo.ShotGun, playerAccInfo.GrenadeLauncher, playerAccInfo.RocketLauncher,
                playerAccInfo.LightningGun, playerAccInfo.RailGun, playerAccInfo.PlasmaGun, playerAccInfo.Bfg,
                playerAccInfo.GrapplingHook, playerAccInfo.NailGun, playerAccInfo.ProximityMineLauncher,
                playerAccInfo.ChainGun, playerAccInfo.HeavyMachineGun);
        }

        /// <summary>
        ///     Handles the player chat message.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandlePlayerChatMessage(string text)
        {
            // Typically we'd normalize using ToUpperInvariant() but QL doesn't allow accented characters,
            // so it doesn't matter
            string msgContent =
                ConsoleTextProcessor.Strip(text.Substring(text.IndexOf(": ", StringComparison.Ordinal) + 1))
                    .ToLowerInvariant();

            string name = text.Substring(0, text.LastIndexOf('\u0019'));

            // teamchat is already ignored, so also ignore 'tell' messages which would crash bot
            if (name.StartsWith("\u0019[")) return;

            string msgFrom = Helpers.GetStrippedName(name);

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
            if (m.Groups["playerinfo"].Value.Equals("\"\"", StringComparison.InvariantCultureIgnoreCase))
            {
                // Player has been kicked or otherwise leaves; the playerinfo which is normally
                // n\name\t#\model... will just be ""; which would be treated as a new player connecting, ignore.
                return;
            }

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
            var ready = (ReadyStatus)status;
            var team = (Team)tm;
            PlayerInfo p;
            // Player already exists... Update if necessary.
            if (_ssb.ServerInfo.CurrentPlayers.TryGetValue(playername, out p))
            {
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
        ///     Handles the situation when a player joins the spectators.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandlePlayerWentToSpec(string player)
        {
            // Spectator event includes the full clan tag, so need to strip
            player = Helpers.GetStrippedName(player);

            // The outgoing player was actually in the game & not a spectator.
            //TODO: investigate this, it might always be false for sub being moved out in pickup
            bool outgoingWasActive = _ssb.ServerInfo.IsActivePlayer(player);
            // Get the outgoing player's team before he disconnected
            var outgoingTeam = _ssb.ServerInfo.CurrentPlayers[player].Team;

            // Evaluate the player's no-show/sub status for pickup module, if active
            if (_ssb.Mod.Pickup.Active)
            {
                await _ssb.Mod.Pickup.Manager.EvalOutgoingPlayer(player, outgoingWasActive, outgoingTeam);
                _ssb.Mod.Pickup.Manager.RemoveActivePickupPlayer(player);
                _ssb.Mod.Pickup.Manager.RemoveEligibility(player);
            }

            // Evaluate player's early quit situation if that module is active
            if (!_ssb.Mod.EarlyQuit.Active) return;
            if (!outgoingWasActive) return;
            var eqh = new EarlyQuitHandler(_ssb);
            await eqh.EvalCountdownQuitter(player);
            await eqh.EvalInProgressQuitter(player);
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
            string playername = GetCsValue("n", pi).ToLowerInvariant();
            if (!int.TryParse(GetCsValue("t", pi), out tm))
            {
                Debug.WriteLine(string.Format("Unable to determine team info for player {0} from cs info.",
                    playername));
                return;
            }

            string clantag = GetCsValue("cn", pi);
            string subscriber = GetCsValue("su", pi);
            string fullclanname = GetCsValue("xcn", pi);
            string country = GetCsValue("c", pi);

            // Create player. Also set misc details like full clan name, country code, subscription status.
            _ssb.ServerInfo.CurrentPlayers[playername] = new PlayerInfo(playername, clantag, (Team)tm,
                id) { Subscriber = subscriber, FullClanName = fullclanname, CountryCode = country };
            Debug.Write(
                string.Format(
                    "[NEWPLAYER(CS)]: Detected player {0} - Country: {1} - Tag: {2} - (Clan: {3}) - Pro: {4} - \n",
                    playername, country, clantag, fullclanname, subscriber));

            // Keep team information up to date
            UpdatePlayerTeam(playername, (Team)tm);
        }

        /// <summary>
        ///     Handles the QLRanks Elo update if necessary.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task HandleEloUpdate(string player)
        {
            PlayerInfo p;
            if (!_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out p)) return;
            if (_qlRanksHelper.DoesCachedEloExist(player))
            {
                if (!_qlRanksHelper.IsCachedEloDataOutdated(player))
                {
                    _qlRanksHelper.SetCachedEloData(_ssb.ServerInfo.CurrentPlayers, player);
                    Debug.WriteLine(
                        string.Format("Setting non-expired cached elo result for {0} from database", player));
                }
                else
                {
                    _qlRanksHelper.CreateNewPlayerEloData(_ssb.ServerInfo.CurrentPlayers, player);
                    await
                        _qlRanksHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, player);
                    Debug.WriteLine(
                        string.Format("Outdated cached elo data found in DB for {0}. Will update.", player));
                }
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
            if (Helpers.KeyExists(player, _ssb.ServerInfo.CurrentPlayers))
            {
                _ssb.ServerInfo.CurrentPlayers.Remove(player);
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
            Debug.WriteLine("****** Updated {0}'s team to: {1} ******", player, team);
            // Pickup module
            if (_ssb.Mod == null) return;
            if (_ssb.Mod.Pickup.Active && (team == Team.Red || team == Team.Blue))
            {
                if (!_ssb.Mod.Pickup.Manager.IsPickupPreGame &&
                    !_ssb.Mod.Pickup.Manager.IsPickupInProgress) return;
                
                _ssb.Mod.Pickup.Manager.AddActivePickupPlayer(player.ToLowerInvariant());
            }
        }
    }
}