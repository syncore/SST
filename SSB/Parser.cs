using System.Text.RegularExpressions;
using SSB.Utils.Parser;
using Utils.Parser;

namespace SSB
{
    /// <summary>
    ///     Class responsible for parsing text with regexes
    /// </summary>
    public class Parser
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Parser" /> class.
        /// </summary>
        public Parser()
        {
            CsPlayerAndTeam = new csPlayerandTeam();
            CsPlayerNameOnly = new csPlayerNameOnly();
            CsPlayerTeamOnly = new csPlayerTeamOnly();
            PlPlayerNameAndId = new plPlayerNameAndId();
            EvPlayerConnected = new evPlayerConnected();
            EvPlayerDisconnected = new evPlayerDisconnected();
            EvPlayerKicked = new evPlayerKicked();
            CvarServerPublicId = new cvarServerPublicId();
            EvMapLoaded = new evMapLoaded();
        }

        /// <summary>
        ///     Regex for finding a player's name and team number after issuing 'configstrings' command.
        /// </summary>
        /// <value>
        ///     Regex for player's name and team number after issuing 'configstrings' command.
        /// </value>
        public Regex CsPlayerAndTeam { get; private set; }

        /// <summary>
        ///     Regex for finding only a player's name after issuing 'configstrings' command.
        /// </summary>
        /// <value>
        ///     Regex for player's name after issuing 'configstrings' command.
        /// </value>
        public Regex CsPlayerNameOnly { get; private set; }

        /// <summary>
        ///     Regex for finding a player's team number after issuing 'configstrings' command.
        /// </summary>
        /// <value>
        ///     Regex for player's team number after issuing 'configstrings' command.
        /// </value>
        public Regex CsPlayerTeamOnly { get; private set; }

        /// <summary>
        ///     Regex for finding sv_gtid cvar value after issuing 'serverinfo' command.
        /// </summary>
        /// <value>
        ///     Regex for cvar sv_gtid after issuing 'serverinfo' command.
        /// </value>
        public Regex CvarServerPublicId { get; private set; }

        /// <summary>
        /// Regex for detecting when the map has loaded.
        /// </summary>
        /// <value>
        /// Regex for detecting when the map has loaded.
        /// </value>
        public Regex EvMapLoaded { get; private set; }

        /// <summary>
        ///     Regex for detecting when a player has connected.
        /// </summary>
        /// <value>
        ///     Regex for player has connected event.
        /// </value>
        public Regex EvPlayerConnected { get; private set; }

        /// <summary>
        ///     Regex for detecting when a player has disconnected.
        /// </summary>
        /// <value>
        ///     Regex for player has disconnected event.
        /// </value>
        public Regex EvPlayerDisconnected { get; private set; }

        /// <summary>
        ///     Regex for detecting when a player has been kicked.
        /// </summary>
        /// <value>
        ///     Regex for player has been kicked event.
        /// </value>
        public Regex EvPlayerKicked { get; private set; }

        /// <summary>
        ///     Regex for finding player's name and id after issuing 'players' command.
        /// </summary>
        /// <value>
        ///     Regex for player's name and id after issuing 'players' command.
        /// </value>
        public Regex PlPlayerNameAndId { get; private set; }
    }
}