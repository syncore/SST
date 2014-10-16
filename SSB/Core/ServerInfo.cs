﻿using System.Collections.Generic;
using SSB.Enum;
using SSB.Model;

namespace SSB.Core
{
    /// <summary>
    ///     Class that contains important information about the server on which the bot is loaded.
    /// </summary>
    public class ServerInfo
    {
        public ServerInfo()
        {
            CurrentPlayers = new Dictionary<string, PlayerInfo>();
        }

        /// <summary>
        ///     Gets or sets the type of the current game.
        /// </summary>
        /// <value>
        ///     The type of the current game.
        /// </value>
        public QlGameTypes CurrentGameType { get; set; }

        /// <summary>
        ///     Gets the current players.
        /// </summary>
        /// <value>
        ///     The current players.
        /// </value>
        public Dictionary<string, PlayerInfo> CurrentPlayers { get; private set; }
    }
}