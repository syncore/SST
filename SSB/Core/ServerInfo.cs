using System.Collections.Generic;
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
        /// Gets or sets the current server identifier.
        /// </summary>
        /// <value>
        /// The current server identifier.
        /// </value>
        public string CurrentServerId { get; set; }
    }
}