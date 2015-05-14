using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SST.Config;
using SST.Core.Modules;
using SST.Database;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Core
{
    /// <summary>
    /// Class responsible for handling player events.
    /// </summary>
    public class PlayerEventProcessor
    {
        private readonly ConfigHandler _cfgHandler;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CORE]";
        private readonly QlRanksHelper _qlRanksHelper;
        private readonly DbSeenDates _seenDb;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerEventProcessor"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public PlayerEventProcessor(SynServerTool sst)
        {
            _sst = sst;
            _qlRanksHelper = new QlRanksHelper();
            _seenDb = new DbSeenDates();
            _cfgHandler = new ConfigHandler();
        }

        /// <summary>
        /// Handles the player connection.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandleIncomingPlayerConnection(string player)
        {
            // "/?" command, regex would otherwise match, so ignore
            if (player.Equals("players show currently", StringComparison.InvariantCultureIgnoreCase)) return;

            Log.Write("Detected incoming connection for " + player, _logClassType, _logPrefix);

            // Player connections include the clan tag, so we need to remove it
            player = Helpers.GetStrippedName(player);

            _seenDb.UpdateLastSeenDate(player, DateTime.Now);

            if ((Helpers.KeyExists(player, _sst.ServerInfo.CurrentPlayers) &&
                 (!_qlRanksHelper.ShouldSkipEloUpdate(player, _sst.ServerInfo.CurrentPlayers))))
            {
                await HandleEloUpdate(player);
            }

            // If IRC module is active, send the message to the IRC channel
            if (_sst.Mod.Irc.Active && _sst.Mod.Irc.IsConnectedToIrc)
            {
                _sst.Mod.Irc.IrcManager.SendIrcMessage(_sst.Mod.Irc.IrcManager.IrcSettings.ircChannel,
                    string.Format("{0} has connected to my QL server.", player));
            }

            // Auto-op if applicable
            await _sst.ServerEventProcessor.AutoOpActiveAdmin(player);

            // Elo limiter kick, if active
            if (_sst.Mod.EloLimit.Active)
            {
                await _sst.Mod.EloLimit.CheckPlayerEloRequirement(player);
            }
            // Account date kick, if active
            if (_sst.Mod.AccountDateLimit.Active)
            {
                await _sst.Mod.AccountDateLimit.RunUserDateCheck(player);
            }
            // Send general info delayed message
            await SendConnectionInfoMessage(player);

            // Pickup module: notify connecting player of the game signup process
            if (_sst.Mod.Pickup.Active)
            {
                await _sst.Mod.Pickup.Manager.NotifyConnectingUser(player);
            }

            // Check for time-bans
            var bManager = new BanManager(_sst);
            await bManager.CheckForBans(player);
        }

        /// <summary>
        /// Handles the outgoing player connection, either by disconnect or kick.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandleOutgoingPlayerConnection(string player)
        {
            Log.Write("Detected outgoing connection for " + player, _logClassType, _logPrefix);

            player = player.ToLowerInvariant();

            // The outgoing player was actually in the game, and not a spectator
            var outgoingWasActive = _sst.ServerInfo.IsActivePlayer(player);
            // Get the outgoing player's team before he disconnected
            var outgoingTeam = _sst.ServerInfo.CurrentPlayers[player].Team;

            // Evaluate the player's no-show/sub status for pickup module, if active
            if (_sst.Mod.Pickup.Active)
            {
                await _sst.Mod.Pickup.Manager.EvalOutgoingPlayer(player, outgoingWasActive, outgoingTeam);
                _sst.Mod.Pickup.Manager.RemoveActivePickupPlayer(player);
                _sst.Mod.Pickup.Manager.RemoveEligibility(player);
            }

            // Remove player from our internal list
            RemovePlayer(player);

            // If player was/is currently being spectated, clear the internal tracker
            if (!string.IsNullOrEmpty(_sst.ServerInfo.PlayerCurrentlyFollowing)
                && _sst.ServerInfo.PlayerCurrentlyFollowing.Equals(player))
            {
                _sst.ServerInfo.PlayerCurrentlyFollowing = string.Empty;
            }

            // If IRC module is active, send the message to the IRC channel
            if (_sst.Mod.Irc.Active && _sst.Mod.Irc.IsConnectedToIrc)
            {
                _sst.Mod.Irc.IrcManager.SendIrcMessage(_sst.Mod.Irc.IrcManager.IrcSettings.ircChannel,
                    string.Format("{0} has left my QL server.", player));
            }

            // Evaluate player's early quit situation if that module is active
            if (!_sst.Mod.EarlyQuit.Active) return;
            if (!outgoingWasActive) return;

            // Do not increase player's early quit count if already banned.
            var banDb = new DbBans();
            if (banDb.UserAlreadyBanned(player)) return;

            var eqh = new EarlyQuitHandler(_sst);
            await eqh.EvalCountdownQuitter(player);
            await eqh.EvalInProgressQuitter(player);
        }

        /// <summary>
        /// Handles the player accuracy data.
        /// </summary>
        /// <param name="m">The match.</param>
        public void HandlePlayerAccuracyData(Match m)
        {
            var player = _sst.ServerInfo.PlayerCurrentlyFollowing;
            if (string.IsNullOrEmpty(player))
            {
                Log.Write(
                    "Accuracy data detected but is of no use because no player is currently being followed. Ignoring.",
                    _logClassType, _logPrefix);

                return;
            }
            if (!Helpers.KeyExists(player, _sst.ServerInfo.CurrentPlayers))
            {
                Log.Write(
                    "Player currently being followed is no longer on the server/doesn't exist in internal list. Ignoring.",
                    _logClassType, _logPrefix);

                return;
            }
            var extracted = m.Groups["accdata"].Value;
            var accstrings = extracted.Split(' ');
            if (accstrings.Length < 15)
            {
                Log.Write("Got accuracy data length of invalid size. Ignoring.", _logClassType, _logPrefix);
                return;
            }
            var accs = new int[accstrings.Length];
            for (var k = 0; k < accstrings.Length; k++)
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
            _sst.ServerInfo.CurrentPlayers[player].Acc = playerAccInfo;
            Log.Write(string.Format(
                "Set accuracy data for currently followed player: {0}. Data: (MG) {1} | (SG) {2}" +
                " | (GL) {3} | (RL) {4} | (LG) {5} | (RG) {6} | (PG) {7} | (BFG) {8} | (GH) {9} |" +
                " (NG) {10} | (PRX) {11} | (CG) {12} | (HMG) {13}", player, playerAccInfo.MachineGun,
                playerAccInfo.ShotGun, playerAccInfo.GrenadeLauncher, playerAccInfo.RocketLauncher,
                playerAccInfo.LightningGun, playerAccInfo.RailGun, playerAccInfo.PlasmaGun, playerAccInfo.Bfg,
                playerAccInfo.GrapplingHook, playerAccInfo.NailGun, playerAccInfo.ProximityMineLauncher,
                playerAccInfo.ChainGun, playerAccInfo.HeavyMachineGun), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the player chat message.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandlePlayerChatMessage(string text)
        {
            // Typically we'd normalize using ToUpperInvariant() but QL doesn't allow accented
            // characters, so it doesn't matter
            var msgContent =
                ConsoleTextProcessor.Strip(text.Substring(text.IndexOf(": ", StringComparison.Ordinal) + 1))
                    .ToLowerInvariant();

            var name = text.Substring(0, text.LastIndexOf('\u0019'));

            // teamchat is already ignored, so also ignore 'tell' messages which would crash bot
            if (name.StartsWith("\u0019[")) return;

            var msgFrom = Helpers.GetStrippedName(name);

            if (!_sst.AccountName.Equals(msgFrom, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Write(string.Format("Detected chat message {0} from {1}",
                    msgContent, msgFrom), _logClassType, _logPrefix);
            }

            // If IRC module is active, send the message to the IRC channel
            if (_sst.Mod.Irc.Active && _sst.Mod.Irc.IsConnectedToIrc)
            {
                // Don't show
                if (msgContent.StartsWith("[irc]", StringComparison.InvariantCultureIgnoreCase))
                    return;

                _sst.Mod.Irc.IrcManager.SendIrcMessage(_sst.Mod.Irc.IrcManager.IrcSettings.ircChannel,
                    string.Format("[{0} @ QL]: {1}", msgFrom, msgContent));
            }

            // Check to see if chat message is a valid command
            if (msgContent.StartsWith(CommandList.GameCommandPrefix))
            {
                // ReSharper disable once UnusedVariable (synchronous)
                var s = _sst.CommandProcessor.ProcessBotCommand(msgFrom, msgContent);
            }
        }

        /// <summary>
        /// Handles the player's configuration string.
        /// </summary>
        /// <param name="m">The match.</param>
        public void HandlePlayerConfigString(Match m)
        {
            if (m.Groups["playerinfo"].Value.Equals("\"\"", StringComparison.InvariantCultureIgnoreCase))
            {
                // Player has been kicked or otherwise leaves; the playerinfo which is normally
                // n\name\t#\model... will just be ""; which would be treated as a new player
                // connecting, ignore.
                return;
            }

            var idMatchText = m.Groups["id"].Value;
            var playerInfoMatchText = m.Groups["playerinfo"].Value.Replace("\"", "");

            var pi = playerInfoMatchText.Split('\\');
            if (pi.Length == 0)
            {
                Log.Write("Received invalid player info array length.", _logClassType, _logPrefix);
                return;
            }
            var playername = GetCsValue("n", pi);
            int status;
            int tm;
            int.TryParse(GetCsValue("t", pi), out tm);
            int.TryParse(GetCsValue("rp", pi), out status);
            var ready = (ReadyStatus)status;
            var team = (Team)tm;
            PlayerInfo p;
            // Player already exists... Update if necessary.
            if (_sst.ServerInfo.CurrentPlayers.TryGetValue(playername, out p))
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
        /// Handles the situation when a player joins the spectators.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandlePlayerWentToSpec(string player)
        {
            // Spectator event includes the full clan tag, so need to strip
            player = Helpers.GetStrippedName(player);

            // The outgoing player was actually in the game & not a spectator.
            //TODO: investigate this, it might always be false for sub being moved out in pickup
            var outgoingWasActive = _sst.ServerInfo.IsActivePlayer(player);
            // Get the outgoing player's team before he disconnected
            var outgoingTeam = _sst.ServerInfo.CurrentPlayers[player].Team;

            // Evaluate the player's no-show/sub status for pickup module, if active
            if (_sst.Mod.Pickup.Active)
            {
                await _sst.Mod.Pickup.Manager.EvalOutgoingPlayer(player, outgoingWasActive, outgoingTeam);
                _sst.Mod.Pickup.Manager.RemoveActivePickupPlayer(player);
                _sst.Mod.Pickup.Manager.RemoveEligibility(player);
            }

            // If IRC module is active, send the message to the IRC channel
            if (_sst.Mod.Irc.Active && _sst.Mod.Irc.IsConnectedToIrc)
            {
                _sst.Mod.Irc.IrcManager.SendIrcMessage(_sst.Mod.Irc.IrcManager.IrcSettings.ircChannel,
                    string.Format("{0} has joined the spectators.", player));
            }

            // Evaluate player's early quit situation if that module is active
            if (!_sst.Mod.EarlyQuit.Active) return;
            if (!outgoingWasActive) return;
            var eqh = new EarlyQuitHandler(_sst);
            await eqh.EvalCountdownQuitter(player);
            await eqh.EvalInProgressQuitter(player);
        }

        /// <summary>
        /// Gets the corresponding value associated with a player's configstring.
        /// </summary>
        /// <param name="term">The term to find.</param>
        /// <param name="arr">The array containing the playerinfo.</param>
        /// <returns>
        /// The corresponding value associated with a player's config string. i.e. Using 'n' as the
        /// term will return the player's name, using 'cn' as the term will return the clan tag, if
        /// any. If not found, then an empty string will be returned.
        /// </returns>
        private static string GetCsValue(string term, string[] arr)
        {
            var foundVal = string.Empty;
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i].Equals(term, StringComparison.InvariantCultureIgnoreCase))
                {
                    foundVal = arr[i + 1];
                }
            }
            return foundVal;
        }

        /// <summary>
        /// Creates the new player from the configuration string.
        /// </summary>
        /// <param name="idText">The player id as a string.</param>
        /// <param name="pi">The player info array.</param>
        private void CreateNewPlayerFromConfigString(string idText, string[] pi)
        {
            if (pi.Length <= 1) return;
            int id;
            if (!int.TryParse(idText, out id))
            {
                Log.Write("Unable to determine player's id from cstr.",
                    _logClassType, _logPrefix);

                return;
            }
            id = (id - 29);
            int tm;
            var playername = GetCsValue("n", pi).ToLowerInvariant();
            if (!int.TryParse(GetCsValue("t", pi), out tm))
            {
                Log.Write(string.Format("Unable to determine team info for player {0} from cstr.",
                    playername), _logClassType, _logPrefix);
                return;
            }

            var clantag = GetCsValue("cn", pi);
            var subscriber = GetCsValue("su", pi);
            var fullclanname = GetCsValue("xcn", pi);
            var country = GetCsValue("c", pi);

            // Create player. Also set misc details like full clan name, country code, subscription status.
            _sst.ServerInfo.CurrentPlayers[playername] = new PlayerInfo(playername, clantag, (Team)tm,
                id) { Subscriber = subscriber, FullClanName = fullclanname, CountryCode = country };

            Log.Write(
                string.Format(
                    "Detected player: {0} - Country: {1} - Tag: {2} - Clan Name: {3} - Pro Acct: {4}",
                    playername, country, clantag, fullclanname, (subscriber.Equals("1") ? "yes" : "no")),
                _logClassType, _logPrefix);

            // Keep team information up to date
            UpdatePlayerTeam(playername, (Team)tm);
        }

        /// <summary>
        /// Handles the QLRanks Elo update if necessary.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task HandleEloUpdate(string player)
        {
            PlayerInfo p;
            if (!_sst.ServerInfo.CurrentPlayers.TryGetValue(player, out p)) return;
            if (_qlRanksHelper.DoesCachedEloExist(player))
            {
                if (!_qlRanksHelper.IsCachedEloDataOutdated(player))
                {
                    _qlRanksHelper.SetCachedEloData(_sst.ServerInfo.CurrentPlayers, player);
                }
                else
                {
                    Log.Write(
                        string.Format("Outdated cached Elo data found in database for {0}. Will update.",
                            player), _logClassType, _logPrefix);

                    _qlRanksHelper.CreateNewPlayerEloData(_sst.ServerInfo.CurrentPlayers, player);
                    await
                        _qlRanksHelper.RetrieveEloDataFromApiAsync(_sst.ServerInfo.CurrentPlayers, player);
                }
            }
            else
            {
                _qlRanksHelper.CreateNewPlayerEloData(_sst.ServerInfo.CurrentPlayers, player);
                await
                    _qlRanksHelper.RetrieveEloDataFromApiAsync(_sst.ServerInfo.CurrentPlayers, player);
            }
        }

        /// <summary>
        /// Removes the player from the current in-game players.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        private void RemovePlayer(string player)
        {
            if (Helpers.KeyExists(player, _sst.ServerInfo.CurrentPlayers))
            {
                _sst.ServerInfo.CurrentPlayers.Remove(player);
                Log.Write(string.Format("Removed {0} from the current in-game players.",
                    player), _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(
                    string.Format(
                        "Unable to remove {0} from the current in-game players. Player was not in list of current in-game players.",
                        player), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Sends a general delayed information message to connecting players.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task SendConnectionInfoMessage(string player)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            await
                _sst.QlCommands.QlCmdDelayedTell(
                    string.Format(
                        "^7This server is running SST v^5{0}^7. Use ^3{1}{2}^7 for command list. One command is allowed every ^3{3}^7 seconds.",
                        Helpers.GetVersion(), CommandList.GameCommandPrefix, CommandList.CmdHelp,
                        cfg.CoreOptions.requiredTimeBetweenCommands),
                    player, 25);
        }

        /// <summary>
        /// Updates the player's ready status.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="status">The status.</param>
        private void UpdatePlayerReadyStatus(string player, ReadyStatus status)
        {
            _sst.ServerInfo.CurrentPlayers[player].Ready = status;
            Log.Write(string.Format("Updated {0}'s player status to: {1}",
                player, status), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Updates the player's team.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        private void UpdatePlayerTeam(string player, Team team)
        {
            _sst.ServerInfo.CurrentPlayers[player].Team = team;

            Log.Write(string.Format("Updated {0}'s team to: {1}",
                player, team), _logClassType, _logPrefix);

            // Pickup module
            if (_sst.Mod == null) return;
            if (_sst.Mod.Pickup.Active && (team == Team.Red || team == Team.Blue))
            {
                if (!_sst.Mod.Pickup.Manager.IsPickupPreGame &&
                    !_sst.Mod.Pickup.Manager.IsPickupInProgress) return;

                _sst.Mod.Pickup.Manager.AddActivePickupPlayer(player.ToLowerInvariant());
            }
        }
    }
}
