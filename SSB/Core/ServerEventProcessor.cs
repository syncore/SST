using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
        private readonly DbSeenDates _seenDb;
        private readonly SynServerBot _ssb;
        private Timer _disconnectScanTimer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerEventProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ServerEventProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _seenDb = new DbSeenDates();
        }

        /// <summary>
        ///     Attempts to retrieve a given player's player id (clientnum) from our internal
        ///     player list.
        /// </summary>
        /// <param name="player">The player whose id needs to be retrieved.</param>
        /// <returns>The player</returns>
        public int GetPlayerId(string player)
        {
            PlayerInfo pinfo;
            var id = -1;
            if (_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out pinfo))
            {
                id = pinfo.Id;
                Debug.WriteLine("Retrieved id {0} for player {1}", id, player);
            }
            return id;
        }

        /// <summary>
        ///     Gets the player name from the player identifier.
        /// </summary>
        /// <param name="id">The player identifier.</param>
        /// <returns>
        ///     The player name as a string if the id matches the id parameter,
        ///     otherwise a blank string.
        /// </returns>
        public string GetPlayerNameFromId(int id)
        {
            var name = string.Empty;
            foreach (
                var player in
                    _ssb.ServerInfo.CurrentPlayers.Where(
                        player => player.Value.Id.Equals(id)))
            {
                name = player.Value.ShortName;
            }
            return name;
        }

        /// <summary>
        ///     Handles the scan to check to see if SSB is still connected to a Quake Live server.
        /// </summary>
        public void HandleDisconnectionScan()
        {
            if (_ssb.IsDisconnectionScanPending) return;
            if (!_ssb.IsInitComplete) return;
            _disconnectScanTimer = new Timer(10000) {AutoReset = false, Enabled = true};
            _disconnectScanTimer.Elapsed += DisconnectScanElapsed;
            _ssb.IsDisconnectionScanPending = true;
            Debug.WriteLine("Will check if server connection still exists in 10 seconds.");
        }

        /// <summary>
        ///     Handles the map load or change.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandleMapLoad(string text)
        {
            // Large text: clear
            _ssb.QlCommands.ClearQlWinConsole();
            Debug.WriteLine("Detected map load (pak info): " + text);
        }

        /// <summary>
        ///     Handles the player information from the 'players' command and performs various
        ///     actions based on the data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="playersText">The players text.</param>
        public async Task HandlePlayersAndIdsFromPlayersCmd<T>(
            IEnumerable<T> playersText)
        {
            var eloNeedsUpdating = new List<string>();
            var qlranksHelper = new QlRanksHelper();
            foreach (var p in playersText)
            {
                var text = p.ToString();
                var playerNameOnly =
                    text.Substring(
                        text.LastIndexOf(" ", StringComparison.Ordinal) + 1)
                        .ToLowerInvariant();
                var sndspace = (Helpers.NthIndexOf(text, " ", 2));
                var clanLength =
                    ((text.LastIndexOf(" ", StringComparison.Ordinal) - sndspace));
                var clan = text.Substring(sndspace, (clanLength)).Trim();
                var idText = text.Substring(0, 2).Trim();
                int id;
                if (!int.TryParse(idText, out id))
                {
                    Debug.WriteLine(
                        "Unable to extract player's ID from players.");
                    return;
                }
                Debug.Write(
                    string.Format(
                        "Found player {0} with client id {1} - setting info.\n",
                        playerNameOnly, id));
                _ssb.ServerInfo.CurrentPlayers[playerNameOnly] =
                    new PlayerInfo(playerNameOnly, clan,
                        Team.None,
                        id);

                if (qlranksHelper.DoesCachedEloExist(playerNameOnly))
                {
                    if (!qlranksHelper.IsCachedEloDataOutdated(playerNameOnly))
                    {
                        qlranksHelper.SetCachedEloData(
                            _ssb.ServerInfo.CurrentPlayers, playerNameOnly);
                        Debug.WriteLine(
                            string.Format(
                                "Setting non-expired cached elo result for {0} from database",
                                playerNameOnly));
                    }
                    else
                    {
                        qlranksHelper.CreateNewPlayerEloData(
                            _ssb.ServerInfo.CurrentPlayers, playerNameOnly);
                        eloNeedsUpdating.Add(playerNameOnly);
                        Debug.WriteLine(
                            string.Format(
                                "Outdated cached elo data found in DB for {0}. Adding to queue to update.",
                                playerNameOnly));
                    }
                }
                else
                {
                    qlranksHelper.CreateNewPlayerEloData(
                        _ssb.ServerInfo.CurrentPlayers, playerNameOnly);
                    eloNeedsUpdating.Add(playerNameOnly);
                }
                // Store user's last seen date
                _seenDb.UpdateLastSeenDate(playerNameOnly, DateTime.Now);
            }
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
            // Get the QLRanks info for these players if required
            if (eloNeedsUpdating.Any())
            {
                await
                    qlranksHelper.RetrieveEloDataFromApiAsync(
                        _ssb.ServerInfo.CurrentPlayers, eloNeedsUpdating);
            }
            // If any players should be kicked (i.e. due to a module setting), then do so
            await DoRequiredPlayerKicks();
        }

        /// <summary>
        ///     Detects whether the Quake Live client is currently on a server.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///     <c>true</c> if the client is on a server, otherwise <c>false</c>.
        /// </returns>
        public bool QlServerConnectionExists(string text)
        {
            // ui_mainmenu "1", result of /cmd, or result of a command requiring connection.
            if ((text.Equals("1") || text.Equals("Not connected to a server.",
                StringComparison.InvariantCultureIgnoreCase)))
            {
                _ssb.ServerInfo.IsQlConnectedToServer = false;
                Debug.WriteLine("*** SSB did NOT detect that Quake Live server connection exists! ***");
                return false;
            }

            _ssb.ServerInfo.IsQlConnectedToServer = true;
            Debug.WriteLine("*** SSB detected that Quake Live connection exists! ***");
            return true;
        }

        /// <summary>
        ///     Sets the end of game (frag/time/roundlimit reached)
        /// </summary>
        public void SetEndOfGameLimitReached()
        {
            // Set the game to WARM_UP after an 'limit reached' event is detected.
            var gameState = QlGameStates.Warmup;
            _ssb.ServerInfo.CurrentServerGameState = gameState;
            // Large batch of text incoming
            _ssb.QlCommands.ClearQlWinConsole();
            Debug.WriteLine(
                "End of game (frag/time/roundlimit reached) detected: setting status back to warm-up mode.");
            // Pickup module
            if (_ssb.Mod.Pickup.Active)
            {
                _ssb.Mod.Pickup.Manager.HandleScoreOrTimelimitHit();
            }
            //HandlePickupEvents(gameState);
        }

        /// <summary>
        ///     Handles the start of the game intermission (period between game's end and end of endgame map-voting)
        /// </summary>
        public void SetIntermissionStart()
        {
            // Set the game to WARM_UP after an intermission (typically map end vote) is detected.
            var gameState = QlGameStates.Warmup;
            _ssb.ServerInfo.CurrentServerGameState = gameState;
            // Large batch of text incoming
            _ssb.QlCommands.ClearQlWinConsole();
            Debug.WriteLine(
                "START of Intermission (game end/map voting) detected: setting status back to warm-up mode.");
            // Pickup module
            if (_ssb.Mod.Pickup.Active)
            {
                _ssb.Mod.Pickup.Manager.HandleIntermissionStart();
            }
            //HandlePickupEvents(gameState);
        }

        /// <summary>
        ///     Sets the current server's gamestate.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The current server's gamestate as a <see cref="QlGameStates" />enum value.</returns>
        /// <remarks>
        ///     This method sets the server's gamestate received through either the serverinfo command or a bcs0/cs
        ///     configstring.
        /// </remarks>
        public QlGameStates SetServerGameState(string text)
        {
            // Large text: Clear, just as if this was a manually-issued command, which is very important in the case of bcs0/cs
            _ssb.QlCommands.ClearQlWinConsole();
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
            _ssb.ServerInfo.CurrentServerGameState = gameState;
            Debug.WriteLine(
                "*** Setting server gamestate to {0} via either 'serverinfo' cmd or bcs0/cs",
                gameState);
            // Large text: clear again
            _ssb.QlCommands.ClearQlWinConsole();
            // Pickup module
            HandlePickupEvents(gameState);

            return gameState;
        }

        /// <summary>
        ///     Sets the current server's gametype.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The current server's gametype as a <see cref="QlGameTypes" />enum value.</returns>
        public QlGameTypes SetServerGameType(string text)
        {
            var gtText = text.Trim();
            int gt;
            var gameType = QlGameTypes.Unspecified;
            if (int.TryParse(gtText, out gt))
            {
                _ssb.ServerInfo.CurrentServerGameType = (QlGameTypes) gt;
                gameType = (QlGameTypes) gt;
                Debug.WriteLine("*** Found server gametype: " + gameType);
            }
            else
            {
                Debug.WriteLine(
                    "Received a SetServerGameType event but was unable to convert the returned string into a QlGameTypes value.");
            }

            return gameType;
        }

        /// <summary>
        ///     Sets the server identifier (public_id)
        /// </summary>
        /// <param name="text">The text from which to receive the server id.</param>
        /// <returns>The server's id (public_id) as a string.</returns>
        public string SetServerId(string text)
        {
            var serverId = text.Trim();
            _ssb.ServerInfo.CurrentServerId = serverId;
            Debug.WriteLine("**** Found server id: " + serverId);
            // Clear
            _ssb.QlCommands.ClearBothQlConsoles();
            return serverId;
        }

        /// <summary>
        ///     Sets the team score.
        /// </summary>
        /// <param name="team">The team.</param>
        /// <param name="score">The score.</param>
        public void SetTeamScore(Team team, int score)
        {
            if (team == Team.Blue)
            {
                _ssb.ServerInfo.ScoreBlueTeam = score;
            }
            else if (team == Team.Red)
            {
                _ssb.ServerInfo.ScoreRedTeam = score;
            }
            Debug.WriteLine("Setting {0} team's score to {1}",
                ((team == Team.Blue) ? "BLUE" : "RED"), score);
        }

        /// <summary>
        ///     Checks the player's account registration date against date limit, if date limit is active.
        /// </summary>
        /// <param name="players">The players.</param>
        private async Task CheckAccountDateAgainstLimit(
            Dictionary<string, PlayerInfo> players)
        {
            if (_ssb.Mod.AccountDateLimit.Active)
            {
                await
                    _ssb.Mod.AccountDateLimit.RunUserDateCheck(
                        players);
            }
        }

        /// <summary>
        ///     Checks the player Elo against Elo limit, if Elo limiter is active.
        /// </summary>
        /// <param name="players">The players.</param>
        private async Task CheckEloAgainstLimit(
            Dictionary<string, PlayerInfo> players)
        {
            if (_ssb.Mod.EloLimit.Active)
            {
                foreach (var player in players.ToList())
                {
                    await
                        _ssb.Mod.EloLimit.CheckPlayerEloRequirement(player.Key);
                }
            }
        }

        /// <summary>
        ///     Method that is executed when the disconnection scan timer elapses.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        ///     The <see cref="ElapsedEventArgs" /> instance containing the
        ///     event data.
        /// </param>
        private async void DisconnectScanElapsed(object sender, ElapsedEventArgs e)
        {
            await _ssb.CheckQlServerConnectionExists();

            // If connection no longer exists then we need to stop
            // monitoring the non-existent server and shutdown all console reading.
            if (!_ssb.ServerInfo.IsQlConnectedToServer)
            {
                Debug.WriteLine("Disconnection scan determined that server connection" +
                                " no-longer exists; stopping monitoring.");

                // Stop monitoring, stop reading console, set server as disconnected.
                _ssb.StopMonitoring();
            }

            _ssb.IsDisconnectionScanPending = false;
            _disconnectScanTimer.Enabled = false;
            _disconnectScanTimer = null;
        }

        /// <summary>
        ///     Kicks any of the current players if an active, imposed module requires it.
        /// </summary>
        private async Task DoRequiredPlayerKicks()
        {
            // Elo limiter kick, if active
            await CheckEloAgainstLimit(_ssb.ServerInfo.CurrentPlayers);
            // Account date kick, if active
            await CheckAccountDateAgainstLimit(_ssb.ServerInfo.CurrentPlayers);
            // Check for time-bans
            var autoBanner = new PlayerAutoBanner(_ssb);
            await autoBanner.CheckForBans(_ssb.ServerInfo.CurrentPlayers);
        }

        /// <summary>
        ///     Handles the pickup game events (game start, game end) for the pickup module, if active.
        /// </summary>
        /// <param name="gameState">State of the game.</param>
        private void HandlePickupEvents(QlGameStates gameState)
        {
            if (_ssb.Mod.Pickup.Active)
            {
                if (gameState == QlGameStates.InProgress)
                {
                    _ssb.Mod.Pickup.Manager.HandlePickupLaunch();
                }
                else if (gameState == QlGameStates.Warmup)
                {
                    _ssb.Mod.Pickup.Manager.HandlePickupEnd();
                }
            }
        }
    }
}