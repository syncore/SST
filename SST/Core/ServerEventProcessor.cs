using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using SST.Config;
using SST.Database;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Core
{
    /// <summary>
    /// Class responsible for handling various server events.
    /// </summary>
    public class ServerEventProcessor
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CORE]";
        private readonly List<string> _qlranksToUpdateFromPlayers;
        private readonly DbSeenDates _seenDb;
        private readonly SynServerTool _sst;
        private Timer _disconnectScanTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEventProcessor"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public ServerEventProcessor(SynServerTool sst)
        {
            _sst = sst;
            _seenDb = new DbSeenDates();
            _qlranksToUpdateFromPlayers = new List<string>();
        }

        /// <summary>
        /// Automatically gives operator status using QL's internal op system to the specified
        /// player who is currently on the server and is an SST user with userlevel Admin or higher.
        /// </summary>
        public async Task AutoOpActiveAdmin(string player)
        {
            if (!_sst.IsMonitoringServer) return;

            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();

            if (!cfg.CoreOptions.autoOpAdmins) return;

            var userDb = new DbUsers();
            var userLevel = userDb.GetUserLevel(player);

            if (userLevel <= UserLevel.SuperUser) return;
            if (!_sst.ServerInfo.CurrentPlayers.ContainsKey(player)) return;

            var id = GetPlayerId(player);
            if (id == -1) return;

            await _sst.QlCommands.SendToQlAsync(string.Format("op {0}", id), false);

            Log.Write(string.Format("Auto-opping {0} (user level: {1})",
                    player, userLevel), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Automatically gives operator status using QL's internal op system to all of the server's
        /// current players who are SST users with userlevel Admin or higher.
        /// </summary>
        public async Task AutoOpActiveAdmins()
        {
            if (!_sst.IsMonitoringServer) return;
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();
            if (!cfg.CoreOptions.autoOpAdmins) return;

            var userDb = new DbUsers();
            var allUsers = userDb.GetAllUsers();

            foreach (var u in allUsers.Where(u => _sst.ServerInfo.CurrentPlayers.ContainsKey(u.Name)))
            {
                if (u.AccessLevel <= UserLevel.SuperUser) continue;
                var id = GetPlayerId(u.Name);
                if (id == -1) continue;
                await _sst.QlCommands.SendToQlAsync(string.Format("op {0}", id), false);
                Log.Write(string.Format("Auto-opping {0} (user level: {1})",
                    u.Name, u.AccessLevel), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Attempts to retrieve a given player's player id (clientnum) from our internal player list.
        /// </summary>
        /// <param name="player">The player whose id needs to be retrieved.</param>
        /// <returns>The player</returns>
        public int GetPlayerId(string player)
        {
            PlayerInfo pinfo;
            var id = -1;
            if (_sst.ServerInfo.CurrentPlayers.TryGetValue(player, out pinfo))
            {
                id = pinfo.Id;
            }
            return id;
        }

        /// <summary>
        /// Gets the player name from the player identifier.
        /// </summary>
        /// <param name="id">The player identifier.</param>
        /// <returns>
        /// The player name as a string if the id matches the id parameter, otherwise a blank string.
        /// </returns>
        public string GetPlayerNameFromId(int id)
        {
            var name = string.Empty;
            foreach (
                var player in
                    _sst.ServerInfo.CurrentPlayers.Where(
                        player => player.Value.Id.Equals(id)))
            {
                name = player.Value.ShortName;
            }
            return name;
        }

        /// <summary>
        /// Handles the scan to check to see if SST is still connected to a Quake Live server.
        /// </summary>
        public void HandleDisconnectionScan()
        {
            if (_sst.IsDisconnectionScanPending) return;
            if (!_sst.IsInitComplete) return;
            _disconnectScanTimer = new Timer(10000) { AutoReset = false, Enabled = true };
            _disconnectScanTimer.Elapsed += DisconnectScanElapsed;
            _sst.IsDisconnectionScanPending = true;
            Log.Write("Will check if server connection still exists in 10 seconds.",
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the map load or change.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandleMapLoad(string text)
        {
            _sst.QlCommands.ClearQlWinConsole();
        }

        /// <summary>
        /// Handles the player information from the 'players' command and performs various actions
        /// based on the data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="playersText">The players text.</param>
        /// <remarks>
        /// playersText is sent as an IEnumerable, with each element representing a line of the text
        /// containing the player information from the /players command.
        /// </remarks>
        public async Task HandlePlayersFromPlayersCmd<T>(
            IEnumerable<T> playersText)
        {
            _qlranksToUpdateFromPlayers.Clear();
            var qlranksHelper = new QlRanksHelper();

            foreach (var line in playersText)
            {
                var text = line.ToString();
                var playerNameOnly =
                    text.Substring(
                        text.LastIndexOf(" ", StringComparison.Ordinal) + 1)
                        .ToLowerInvariant();

                // Try to create the player info (name, clan, id)
                if (!CreatePlayerFromPlayersText(text)) return;
                // Set the cached Elo for a player or add to list to be updated
                HandleEloFromPlayersText(qlranksHelper, playerNameOnly);
                // Store user's last seen date
                _seenDb.UpdateLastSeenDate(playerNameOnly, DateTime.Now);
            }
            // Clear
            _sst.QlCommands.ClearBothQlConsoles();

            // Do any necessary Elo updates
            if (_qlranksToUpdateFromPlayers.Any())
            {
                await
                    qlranksHelper.RetrieveEloDataFromApiAsync(
                        _sst.ServerInfo.CurrentPlayers, _qlranksToUpdateFromPlayers);
            }
            // If any players should be kicked (i.e. due to a module), then do so, but wait a few
            // (10) seconds since we might have to hit network for retrieval (i.e. QLRanks)
            await Task.Delay(10000);
            await DoRequiredPlayerKicks();
        }

        /// <summary>
        /// Detects whether the Quake Live client is currently on a server.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the client is on a server, otherwise <c>false</c>.</returns>
        public bool QlServerConnectionExists(string text)
        {
            // ui_mainmenu "1", result of /cmd, or result of a command requiring connection.
            if ((text.Equals("1") || text.Equals("Not connected to a server.",
                StringComparison.InvariantCultureIgnoreCase)))
            {
                _sst.ServerInfo.IsQlConnectedToServer = false;

                Log.Write("Connection to a Quake Live server was not detected.",
                    _logClassType, _logPrefix);

                return false;
            }

            _sst.ServerInfo.IsQlConnectedToServer = true;
            Log.Write("Detected Quake Live server connection.", _logClassType, _logPrefix);

            return true;
        }

        /// <summary>
        /// Sets the current server address.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetCurrentServerAddress(string text)
        {
            _sst.ServerInfo.CurrentServerAddress = text;

            Log.Write(string.Format("Set address of current monitored server to: {0}",
                text), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sets the end of game (frag/time/roundlimit reached)
        /// </summary>
        public void SetEndOfGameLimitReached()
        {
            // Set the game to WARM_UP after an 'limit reached' event is detected.
            _sst.ServerInfo.CurrentServerGameState = QlGameStates.Warmup;
            // Large batch of text incoming
            _sst.QlCommands.ClearQlWinConsole();

            Log.Write("End of game (frag/time/roundlimit reached) detected: setting status back to warm-up.",
                _logClassType, _logPrefix);

            SetEndOfGameTeams();

            // Pickup module
            if (_sst.Mod.Pickup.Active)
            {
                _sst.Mod.Pickup.Manager.HandleScoreOrTimelimitHit();
            }
        }

        /// <summary>
        /// Handles the start of the game intermission (period between game's end and end of endgame map-voting)
        /// </summary>
        public void SetIntermissionStart()
        {
            // Set the game to WARM_UP after an intermission (typically map end vote) is detected.
            _sst.ServerInfo.CurrentServerGameState = QlGameStates.Warmup;
            // Large batch of text incoming
            _sst.QlCommands.ClearQlWinConsole();

            Log.Write(
                "Start of intermission (game end/map voting) detected: setting status back to warm-up.",
                _logClassType, _logPrefix);

            // Pickup module
            if (_sst.Mod.Pickup.Active)
            {
                _sst.Mod.Pickup.Manager.HandleIntermissionStart();
            }
        }

        /// <summary>
        /// Sets the current server's gamestate.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The current server's gamestate as a <see cref="QlGameStates"/> enum value.</returns>
        /// <remarks>
        /// This method sets the server's gamestate received through either the serverinfo command
        /// or a bcs0/cs configstring.
        /// </remarks>
        public QlGameStates SetServerGameState(string text)
        {
            // Large text: Clear, just as if this was a manually-issued command, which is very
            // important in the case of bcs0/cs
            _sst.QlCommands.ClearQlWinConsole();
            var stateText = text.Trim();
            var gameState = QlGameStates.Unspecified;
            switch (stateText)
            {
                case "COUNT_DOWN":
                    gameState = QlGameStates.Countdown;
                    break;

                case "PRE_GAME":
                    gameState = QlGameStates.Warmup;
                    break;

                case "IN_PROGRESS":
                    gameState = QlGameStates.InProgress;
                    break;
            }
            // Set
            _sst.ServerInfo.CurrentServerGameState = gameState;
            Log.Write(string.Format("Gamestate is {0} ", gameState), _logClassType, _logPrefix);
            // Large text: clear again
            _sst.QlCommands.ClearQlWinConsole();
            // Pickup module
            HandlePickupEvents(gameState);

            return gameState;
        }

        /// <summary>
        /// Sets the current server's gametype.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The current server's gametype as a <see cref="QlGameTypes"/> enum value.</returns>
        public QlGameTypes SetServerGameType(string text)
        {
            var gtText = text.Trim();
            int gt;
            var gameType = QlGameTypes.Unspecified;
            if (int.TryParse(gtText, out gt))
            {
                _sst.ServerInfo.CurrentServerGameType = (QlGameTypes)gt;
                gameType = (QlGameTypes)gt;
                Log.Write("Found server gametype: " + gameType, _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(
                    "Received a SetServerGameType event but was unable to convert to QlGameTypes value.",
                    _logClassType, _logPrefix);
            }

            return gameType;
        }

        /// <summary>
        /// Sets the server identifier (public_id)
        /// </summary>
        /// <param name="text">The text from which to receive the server id.</param>
        /// <returns>The server's id (public_id) as a string.</returns>
        public string SetServerId(string text)
        {
            var serverId = text.Trim();
            _sst.ServerInfo.CurrentServerId = serverId;
            Log.Write("Got server id: " + serverId, _logClassType, _logPrefix);
            // Clear
            _sst.QlCommands.ClearBothQlConsoles();
            return serverId;
        }

        /// <summary>
        /// Sets the team score.
        /// </summary>
        /// <param name="team">The team.</param>
        /// <param name="score">The score.</param>
        public void SetTeamScore(Team team, int score)
        {
            if (team == Team.Blue)
            {
                _sst.ServerInfo.ScoreBlueTeam = score;
            }
            else if (team == Team.Red)
            {
                _sst.ServerInfo.ScoreRedTeam = score;
            }
            Log.Write(string.Format("Setting {0} team's score to {1}",
                ((team == Team.Blue) ? "BLUE" : "RED"), score), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Checks the player's account registration date against date limit, if date limit is active.
        /// </summary>
        /// <param name="players">The players.</param>
        private async Task CheckAccountDateAgainstLimit(
            Dictionary<string, PlayerInfo> players)
        {
            if (_sst.Mod.AccountDateLimit.Active)
            {
                await
                    _sst.Mod.AccountDateLimit.RunUserDateCheck(
                        players);
            }
        }

        /// <summary>
        /// Checks the player Elo against Elo limit, if Elo limiter is active.
        /// </summary>
        /// <param name="players">The players.</param>
        private async Task CheckEloAgainstLimit(
            Dictionary<string, PlayerInfo> players)
        {
            if (_sst.Mod.EloLimit.Active)
            {
                foreach (var player in players.ToList())
                {
                    await
                        _sst.Mod.EloLimit.CheckPlayerEloRequirement(player.Key);
                }
            }
        }

        /// <summary>
        /// Creates the player from the players command text.
        /// </summary>
        /// <param name="playerText">The player text.</param>
        /// <returns><c>true</c> if the playerinfo could be created, otherwise <c>false</c></returns>
        private bool CreatePlayerFromPlayersText(string playerText)
        {
            var playerNameOnly =
                playerText.Substring(
                    playerText.LastIndexOf(" ", StringComparison.Ordinal) + 1)
                    .ToLowerInvariant();
            var sndspace = (Helpers.NthIndexOf(playerText, " ", 2));
            var clanLength =
                ((playerText.LastIndexOf(" ", StringComparison.Ordinal) - sndspace));
            var clan = playerText.Substring(sndspace, (clanLength)).Trim();
            var idText = playerText.Substring(0, 2).Trim();
            int id;
            if (!int.TryParse(idText, out id))
            {
                Log.Write("Unable to extract player ID from players.", _logClassType, _logPrefix);
                return false;
            }

            Log.Write(
                string.Format(
                    "Found player {0} with client id {1} - setting info.\n",
                    playerNameOnly, id), _logClassType, _logPrefix);

            _sst.ServerInfo.CurrentPlayers[playerNameOnly] = new PlayerInfo(playerNameOnly,
                clan, Team.None, id);
            return true;
        }

        /// <summary>
        /// Method that is executed when the disconnection scan timer elapses.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private async void DisconnectScanElapsed(object sender, ElapsedEventArgs e)
        {
            await _sst.CheckQlServerConnectionExists();

            // If connection no longer exists then we need to stop monitoring the non-existent
            // server and shutdown all console reading.
            if (!_sst.ServerInfo.IsQlConnectedToServer)
            {
                Log.Write("Determined that QL server connection no longer exists; Will stop all" +
                          " server monitoring.", _logClassType, _logPrefix);

                // Stop monitoring, stop reading console, set server as disconnected.
                _sst.StopMonitoring();
            }

            _sst.IsDisconnectionScanPending = false;
            _disconnectScanTimer.Enabled = false;
            _disconnectScanTimer = null;
        }

        /// <summary>
        /// Kicks any of the current players if an active, imposed module requires it.
        /// </summary>
        private async Task DoRequiredPlayerKicks()
        {
            // Elo limiter kick, if active
            await CheckEloAgainstLimit(_sst.ServerInfo.CurrentPlayers);
            // Account date kick, if active
            await CheckAccountDateAgainstLimit(_sst.ServerInfo.CurrentPlayers);
            // Check for time-bans
            var bManager = new BanManager(_sst);
            await bManager.CheckForBans(_sst.ServerInfo.CurrentPlayers);
        }

        /// <summary>
        /// Handles the setting or updating of Elo from the players command text.
        /// </summary>
        /// <param name="qlranksHelper">The QLRanks helper.</param>
        /// <param name="player">The player.</param>
        private void HandleEloFromPlayersText(QlRanksHelper qlranksHelper, string player)
        {
            // Set cached Elo or update
            if (qlranksHelper.DoesCachedEloExist(player))
            {
                if (!qlranksHelper.IsCachedEloDataOutdated(player))
                {
                    qlranksHelper.SetCachedEloData(
                        _sst.ServerInfo.CurrentPlayers, player);

                    Log.Write(string.Format(
                        "Setting non-expired cached Elo result for {0} from database",
                        player), _logClassType, _logPrefix);
                }
                else
                {
                    qlranksHelper.CreateNewPlayerEloData(
                        _sst.ServerInfo.CurrentPlayers, player);
                    _qlranksToUpdateFromPlayers.Add(player);

                    Log.Write(string.Format(
                        "Outdated cached elo data found in DB for {0}. Adding to queue to update.",
                        player), _logClassType, _logPrefix);
                }
            }
            else
            {
                qlranksHelper.CreateNewPlayerEloData(
                    _sst.ServerInfo.CurrentPlayers, player);
                _qlranksToUpdateFromPlayers.Add(player);
            }
        }

        /// <summary>
        /// Handles the pickup game events (game start, game end) for the pickup module, if active.
        /// </summary>
        /// <param name="gameState">State of the game.</param>
        private void HandlePickupEvents(QlGameStates gameState)
        {
            if (_sst.Mod.Pickup.Active)
            {
                if (gameState == QlGameStates.InProgress)
                {
                    _sst.Mod.Pickup.Manager.HandlePickupLaunch();
                }
                else if (gameState == QlGameStates.Warmup)
                {
                    _sst.Mod.Pickup.Manager.HandlePickupEnd();
                }
            }
        }

        /// <summary>
        /// Sets the end of game teams.
        /// </summary>
        private void SetEndOfGameTeams()
        {
            if (!_sst.ServerInfo.IsATeamGame()) return;
            _sst.ServerInfo.EndOfGameRedTeam.Clear();
            _sst.ServerInfo.EndOfGameBlueTeam.Clear();

            foreach (var player in _sst.ServerInfo.GetTeam(Team.Red))
            {
                _sst.ServerInfo.EndOfGameRedTeam.Add(player.ShortName);
            }
            foreach (var player in _sst.ServerInfo.GetTeam(Team.Blue))
            {
                _sst.ServerInfo.EndOfGameBlueTeam.Add(player.ShortName);
            }
        }
    }
}
