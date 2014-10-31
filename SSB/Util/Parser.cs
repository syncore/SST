using System.Text.RegularExpressions;
using SSB.External.Parser;

namespace SSB.Util
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
            CsPlayerInfo = new csPlayerInfo();
            CsPlayerAndTeam = new csPlayerandTeam();
            CsPlayerNameOnly = new csPlayerNameOnly();
            CsPlayerTeamOnly = new csPlayerTeamOnly();
            PlPlayerNameAndId = new plPlayerNameAndId();
            EvPlayerConnected = new evPlayerConnected();
            EvPlayerDisconnected = new evPlayerDisconnected();
            EvPlayerKicked = new evPlayerKicked();
            EvPlayerRageQuit = new evPlayerRageQuit();
            CvarBotAccountName = new cvarBotAccountName();
            CvarGameType = new cvarGameType();
            CvarServerPublicId = new cvarServerPublicId();
            EvMapLoaded = new evMapLoaded();
            ScmdPlayerConnected = new scmdPlayerConnected();
            ScmdPlayerKicked = new scmdPlayerKicked();
            ScmdPlayerDisconnected = new scmdPlayerDisconnected();
            ScmdPlayerRageQuits = new scmdPlayerRagequits();
        }

        /// <summary>
        ///     Regex for finding a player's name and team number after issuing 'configstrings' command.
        /// </summary>
        /// <value>
        ///     Regex for player's name and team number after issuing 'configstrings' command.
        /// </value>
        public Regex CsPlayerAndTeam { get; private set; }

        /// <summary>
        /// Regex for finding the player info configstring.
        /// </summary>
        /// <value>
        /// Regex for finding the player inf configstring.
        /// </value>
        /// <remarks>This contains the following named groups:
        /// 'id' is the two digit number after the 5 in the configstring from which 29 is to be subtracted.
        /// 'playerinfo' is the rest of the string after the cs 5##
        /// </remarks>
        public Regex CsPlayerInfo { get; private set; }

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
        /// Regex for finding name cvar value after issuing 'name'
        /// </summary>
        /// <value>
        /// Regex for cvar name after issuing 'name'
        /// </value>
        public Regex CvarBotAccountName { get; private set; }

        /// <summary>
        /// Regex for finding the g_gametype cvar value after issuing 'serverinfo'
        /// </summary>
        /// <value>
        /// Regex for cvar g_gametype after issuing 'serverinfo' command.
        /// </value>
        /// <remarks>
        /// Contains a named group 'gametype' that has the gametype #.
        /// </remarks>
        public Regex CvarGameType { get; private set; }

        /// <summary>
        ///     Regex for finding sv_gtid cvar value after issuing 'serverinfo' command.
        /// </summary>
        /// <value>
        ///     Regex for cvar sv_gtid after issuing 'serverinfo' command.
        /// </value>
        /// <remarks>
        /// Contains a named group 'serverid' that has the serverid.
        /// </remarks>
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
        /// Regex for detecting when a player has ragequit.
        /// </summary>
        /// <value>
        /// Regex for player has ragequit event.
        /// </value>
        public Regex EvPlayerRageQuit { get; private set; }

        /// <summary>
        ///     Regex for finding player's name and id after issuing 'players' command.
        /// </summary>
        /// <value>
        ///     Regex for player's name and id after issuing 'players' command.
        /// </value>
        public Regex PlPlayerNameAndId { get; private set; }

        /// <summary>
        /// Regex for finding a player who has connected as issued in a servercommand.
        /// </summary>
        /// <value>
        /// Regex for finding a player who has connected as issued in a servercommand.
        /// </value>
        /// <remarks>
        /// This contains a named group, 'player' that has the name of the player who has connected.
        /// </remarks>
        public Regex ScmdPlayerConnected { get; private set; }

        /// <summary>
        /// Regex for finding a player who has disconnected as issued in a servercommand.
        /// </summary>
        /// <value>
        /// Regex for finding a player who has disconnected as issued in a servercommand.
        /// </value>
        /// <remarks>
        /// This contains a named group, 'player' that has the name of the player who disconnected.
        /// </remarks>
        public Regex ScmdPlayerDisconnected { get; private set; }

        /// <summary>
        /// Regex for finding a player who was kicked as issued in a servercommand.
        /// </summary>
        /// <value>
        /// Regex for finding a player who was kicked as issued in a servercommand.
        /// </value>
        /// <remarks>
        /// This contains a named group, 'player' that has the name of the player being kicked.
        /// </remarks>
        public Regex ScmdPlayerKicked { get; private set; }

        /// <summary>
        /// Regex for finding a player who has ragequit as issued in a servercommand.
        /// </summary>
        /// <value>
        /// Regex for finding a player who has ragequit as issued in a servercommand.
        /// </value>
        /// <remarks>
        /// This contains a named group, 'player' that has the name of the player who has ragequit.
        /// </remarks>
        public Regex ScmdPlayerRageQuits { get; private set; }
    }
}