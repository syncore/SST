using System.Collections.Generic;
using System.Linq;
using SSB.Enum;
using SSB.Model;

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
            CurrentPlayers = new Dictionary<string, PlayerInfo>();
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
        /// Gets the team.
        /// </summary>
        /// <param name="t">The Team enum.</param>
        /// <returns>A list of <see cref="PlayerInfo"/>objects for a given Team enum.</returns>
        public List<PlayerInfo> GetTeam(Team t)
        {
            return CurrentPlayers.Where(player => player.Value.Team.Equals(t)).Select(player => player.Value).ToList();
        }
    }
}