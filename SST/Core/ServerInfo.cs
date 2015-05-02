using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Core
{
    /// <summary>
    ///     Class that contains important information about the server on which the bot is loaded.
    /// </summary>
    public class ServerInfo
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CORE]";

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerInfo" /> class.
        /// </summary>
        public ServerInfo()
        {
            // Ignoring case is very important here
            CurrentPlayers =
                new Dictionary<string, PlayerInfo>(StringComparer.InvariantCultureIgnoreCase);
            EndOfGameRedTeam = new List<string>();
            EndOfGameBlueTeam = new List<string>();
        }

        /// <summary>
        ///     Gets the current players.
        /// </summary>
        /// <value>
        ///     The current players.
        /// </value>
        public Dictionary<string, PlayerInfo> CurrentPlayers { get; private set; }

        /// <summary>
        /// Gets or sets the current server address.
        /// </summary>
        /// <value>
        /// The current server address.
        /// </value>
        public string CurrentServerAddress { get; set; }

        /// <summary>
        ///     Gets or sets the server's current game state.
        /// </summary>
        /// <value>
        ///     The server's current gamestate.
        /// </value>
        public QlGameStates CurrentServerGameState { get; set; }

        /// <summary>
        ///     Gets or sets the type of game for the current server.
        /// </summary>
        /// <value>
        ///     The type of game for the current server.
        /// </value>
        public QlGameTypes CurrentServerGameType { get; set; }

        /// <summary>
        ///     Gets or sets the current server identifier.
        /// </summary>
        /// <value>
        ///     The current server identifier.
        /// </value>
        public string CurrentServerId { get; set; }

        /// <summary>
        /// Gets or sets the blue team at game's end.
        /// </summary>
        /// <value>
        /// The blue team at game's end.
        /// </value>
        public List<string> EndOfGameBlueTeam { get; set; }

        /// <summary>
        /// Gets or sets the red team at game's end.
        /// </summary>
        /// <value>
        /// The red team at game's end.
        /// </value>
        public List<string> EndOfGameRedTeam { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the Quake Live client is actually
        ///     connected to a server or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the Quake Live client is connected to server; otherwise, <c>false</c>.
        /// </value>
        public bool IsQlConnectedToServer { get; set; }

        /// <summary>
        ///     Gets or sets the player the bot is currently following (spectating), if any.
        /// </summary>
        /// <value>
        ///     The player the bot is currently following (spectating), if any.
        /// </value>
        public string PlayerCurrentlyFollowing { get; set; }

        /// <summary>
        ///     Gets or sets the blue team's score.
        /// </summary>
        /// <value>
        ///     The blue team's score.
        /// </value>
        public int ScoreBlueTeam { get; set; }

        /// <summary>
        ///     Gets or sets the red team's score.
        /// </summary>
        /// <value>
        ///     The red team's score.
        /// </value>
        public int ScoreRedTeam { get; set; }

        /// <summary>
        ///     Gets the team.
        /// </summary>
        /// <param name="t">The Team enum.</param>
        /// <returns>A list of <see cref="PlayerInfo" />objects for a given Team enum.</returns>
        public List<PlayerInfo> GetTeam(Team t)
        {
            return
                CurrentPlayers.Where(player => player.Value.Team.Equals(t))
                    .Select(player => player.Value)
                    .ToList();
        }

        /// <summary>
        ///     Determines whether the active player count on the server is an even number.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the server's active (team red or team blue) player count is
        ///     an even number; otherwise <c>false</c>.
        /// </returns>
        public bool HasEvenTeams()
        {
            var red = GetTeam(Team.Red);
            var blue = GetTeam(Team.Blue);
            return (red.Count + blue.Count) % 2 == 0;
        }

        /// <summary>
        ///     Determines whether the specified player is an active player (on red or blue team).
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns><c>true</c> if the specified player is an active player, otherwise <c>false</c></returns>
        public bool IsActivePlayer(string player)
        {
            if (!Helpers.KeyExists(player, CurrentPlayers))
            {
                return false;
            }
            return CurrentPlayers[player].Team == Team.Blue ||
                   CurrentPlayers[player].Team == Team.Red;
        }

        /// <summary>
        ///     Determines whether the current gametype is a team-based game.
        /// </summary>
        /// <returns><c>true</c> if the current gametype is a team-based game; otherwise <c>false</c></returns>
        public bool IsATeamGame()
        {
            return Helpers.IsQuakeLiveTeamGame(CurrentServerGameType);
        }

        /// <summary>
        ///     Resets the server information.
        /// </summary>
        public void Reset()
        {
            CurrentPlayers.Clear();
            CurrentServerGameState = QlGameStates.Unspecified;
            CurrentServerGameType = QlGameTypes.Unspecified;
            CurrentServerId = string.Empty;
            CurrentServerAddress = string.Empty;
            IsQlConnectedToServer = false;
            PlayerCurrentlyFollowing = string.Empty;
            ScoreBlueTeam = 0;
            ScoreRedTeam = 0;
            Log.Write("Reset server information.", _logClassType, _logPrefix);
        }
    }
}