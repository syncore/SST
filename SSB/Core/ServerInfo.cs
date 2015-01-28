using System;
using System.Collections.Generic;
using System.Linq;
using SSB.Enum;
using SSB.Model;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class that contains important information about the server on which the bot is loaded.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInfo"/> class.
        /// </summary>
        public ServerInfo()
        {
            // Ignoring case is very important here
            CurrentPlayers =
                new Dictionary<string, PlayerInfo>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///     Gets the current players.
        /// </summary>
        /// <value>
        ///     The current players.
        /// </value>
        public Dictionary<string, PlayerInfo> CurrentPlayers { get; private set; }

        /// <summary>
        /// Gets or sets the server's current game state.
        /// </summary>
        /// <value>
        /// The server's current gamestate.
        /// </value>
        public QlGameStates CurrentServerGameState { get; set; }

        /// <summary>
        /// Gets or sets the type of game for the current server.
        /// </summary>
        /// <value>
        /// The type of game for the current server.
        /// </value>
        public QlGameTypes CurrentServerGameType { get; set; }

        /// <summary>
        /// Gets or sets the current server identifier.
        /// </summary>
        /// <value>
        /// The current server identifier.
        /// </value>
        public string CurrentServerId { get; set; }

        /// <summary>
        /// Gets or sets the player the bot is currently following (spectating), if any.
        /// </summary>
        /// <value>
        /// The player the bot is currently following (spectating), if any.
        /// </value>
        public string PlayerCurrentlyFollowing { get; set; }

        /// <summary>
        /// Gets the team.
        /// </summary>
        /// <param name="t">The Team enum.</param>
        /// <returns>A list of <see cref="PlayerInfo"/>objects for a given Team enum.</returns>
        public List<PlayerInfo> GetTeam(Team t)
        {
            return CurrentPlayers.Where(player => player.Value.Team.Equals(t)).Select(player => player.Value).ToList();
        }

        /// <summary>
        /// Determines whether the active player count on the server is an even number.
        /// </summary>
        /// <returns><c>true</c> if the server's active (team red or team blue) player count is
        /// an even number; otherwise <c>false</c>.</returns>
        public bool HasEvenTeams()
        {
            var red = GetTeam(Team.Red);
            var blue = GetTeam(Team.Blue);
            return (red.Count + blue.Count) % 2 == 0;
        }

        /// <summary>
        /// Determines whether the specified player is an active player (on red or blue team).
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns><c>true</c> if the specified player is an active player, otherwise <c>false</c></returns>
        public bool IsActivePlayer(string player)
        {
            if (!Tools.KeyExists(player, CurrentPlayers))
            {
                return false;
            }
            return CurrentPlayers[player].Team == Team.Blue ||
                   CurrentPlayers[player].Team == Team.Red;
        }

        /// <summary>
        /// Determines whether the current gametype is a team-based game.
        /// </summary>
        /// <returns><c>true</c> if the current gametype is a team-based game; otherwise <c>false</c></returns>
        public bool IsATeamGame()
        {
            switch (CurrentServerGameType)
            {
                case QlGameTypes.Unspecified:
                case QlGameTypes.Ffa:
                case QlGameTypes.Duel:
                case QlGameTypes.Race:
                    return false;

                case QlGameTypes.Tdm:
                case QlGameTypes.Ca:
                case QlGameTypes.Ctf:
                case QlGameTypes.OneFlagCtf:
                case QlGameTypes.Harvester:
                case QlGameTypes.FreezeTag:
                case QlGameTypes.Domination:
                case QlGameTypes.AttackDefend:
                case QlGameTypes.RedRover:
                    return true;
            }
            return false;
        }
    }
}