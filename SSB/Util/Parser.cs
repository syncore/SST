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
            CfgStringPlayerInfo = new cfgStringPlayerInfo();
            PlPlayerNameAndId = new plPlayerNameAndId();
            EvPlayerDisconnected = new evPlayerDisconnected();
            EvPlayerKicked = new evPlayerKicked();
            EvPlayerRageQuit = new evPlayerRageQuit();
            CvarBotAccountName = new cvarBotAccountName();
            CvarGameType = new cvarGameType();
            CvarGameState = new cvarGameState();
            CvarServerPublicId = new cvarServerPublicId();
            EvMapLoaded = new evMapLoaded();
            ScmdPlayerConfigString = new scmdPlayerConfigString();
            ScmdPlayerConnected = new scmdPlayerConnected();
            ScmdPlayerKicked = new scmdPlayerKicked();
            ScmdPlayerDisconnected = new scmdPlayerDisconnected();
            ScmdPlayerJoinedSpectators = new scmdPlayerJoinedSpectators();
            ScmdPlayerRageQuits = new scmdPlayerRagequits();
            ScmdVoteCalledDetails = new scmdVoteCalledDetails();
            ScmdVoteCalledTagAndPlayer = new scmdVoteCalledTagAndPlayer();
            ScmdVoteNumYesVotes = new scmdVoteNumYesVotes();
            ScmdVoteNumNoVotes = new scmdVoteNumNoVotes();
            ScmdIntermission = new scmdIntermission();
            ScmdChatMessage = new scmdChatMessage();
            ScmdVoteFinalResult = new scmdVoteFinalResult();
            ScmdGameStateChange = new scmdGameStateChange();
            UtilCaretColor = new utilCaretColor();
        }

        /// <summary>
        ///     Regex for matching the player info returned by the 'configstrings' command.
        /// </summary>
        /// <value>
        ///     Regex for matching the player info returned by the 'configstrings' command.
        /// </value>
        /// <remarks>
        ///     Named group 'id' returns the two digit number after the 5, i.e. for 533 it will return 33. Must subtract
        ///     29 from this number to get the equivalent player id that can be retrieved from 'players' command.
        ///     Named group 'playerinfo' returns the entire player info string, i.e.:
        ///     n\syncore\t\3\model\sarge\hmodel\sarge\c1\13\c2\16\hc\100\w\0\l\0\tt\0\tl\0\rp\0\p\3\so\0\pq\0\wp\hmg\ws\sg\cn\\su\0\xcn\\c\
        /// </remarks>
        public Regex CfgStringPlayerInfo { get; private set; }

        /// <summary>
        ///     Regex for matching the name cvar (name of the account running SSB).
        /// </summary>
        /// <value>
        ///     Regex for matching the name cvar (name of the account running SSB).
        /// </value>
        public Regex CvarBotAccountName { get; private set; }

        /// <summary>
        ///     Regex for finding the g_gameState cvar value after issuing 'serverinfo'
        /// </summary>
        /// <value>
        ///     Regex for cvar g_gameState after issuing 'serverinfo' command.
        /// </value>
        /// <remarks>
        ///     Contains a named group 'gamestate' that has the gamestate status.
        /// </remarks>
        public Regex CvarGameState { get; private set; }

        /// <summary>
        ///     Regex for finding the g_gametype cvar value after issuing 'serverinfo'
        /// </summary>
        /// <value>
        ///     Regex for cvar g_gametype after issuing 'serverinfo' command.
        /// </value>
        /// <remarks>
        ///     Contains a named group 'gametype' that has the gametype #.
        /// </remarks>
        public Regex CvarGameType { get; private set; }

        /// <summary>
        ///     Regex for finding sv_gtid cvar value after issuing 'serverinfo' command.
        /// </summary>
        /// <value>
        ///     Regex for cvar sv_gtid after issuing 'serverinfo' command.
        /// </value>
        /// <remarks>
        ///     Contains a named group 'serverid' that has the serverid.
        /// </remarks>
        public Regex CvarServerPublicId { get; private set; }

        /// <summary>
        ///     Regex for detecting when the map has loaded.
        /// </summary>
        /// <value>
        ///     Regex for detecting when the map has loaded.
        /// </value>
        public Regex EvMapLoaded { get; private set; }

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
        ///     Regex for detecting when a player has ragequit.
        /// </summary>
        /// <value>
        ///     Regex for player has ragequit event.
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
        ///     Regex for matching a player's chat message.
        /// </summary>
        /// <value>
        ///     Regex for matching a player's chat message.
        /// </value>
        /// <remarks>
        ///     Named group 'fullplayerandmsg' contains clan tag + playername + msg.
        ///     There is a unicode END OF MEDIUM character 19 between the end of the player name
        ///     and the colon.
        ///     serverCommand: 4 : chat "00 player: hello" - would match: player\u0019: hello
        /// </remarks>
        public Regex ScmdChatMessage { get; private set; }

        /// <summary>
        ///     Regex for detecting gamestate status changes.
        /// </summary>
        /// <value>
        ///     Regex for detecting gamestate status changes.
        /// </value>
        /// <remarks>
        ///     Named group 'time' returns a string: \time\# that indicates a gamestate status change.
        ///     If \time\-1 then game is in warm-up mode. If \time\large#, then game is leaving warmup mode
        ///     and entering the countdown. If \time\0 then game is in progress.
        /// </remarks>
        public Regex ScmdGameStateChange { get; private set; }

        /// <summary>
        /// Regex for detecting when a game enters intermission (game ends).
        /// </summary>
        /// <value>
        /// Regex for detecting when a game enters intermission (game ends).
        /// </value>
        /// <remarks>
        /// This contains a named group 'intermissionvalue', with a value of 1 indicating that the game has entered
        /// intermission (that is, the game has ended).
        /// </remarks>
        public Regex ScmdIntermission { get; private set; }

        /// <summary>
        ///     Regex for matching the player info presented as a server command.
        /// </summary>
        /// <value>
        ///     Regex for matching the player info presented as a server command.
        /// </value>
        /// <remarks>
        ///     This is slightly different from <see cref="CfgStringPlayerInfo" /> as this version
        ///     is the one that is automatically returned as a serverCommand. However, it returns the same info as the cfgString
        ///     version.
        ///     Named group 'id' returns the two digit number after the 5, i.e. for 533 it will return 33. Must subtract
        ///     29 from this number to get the equivalent player id that can be retrieved from 'players' command.
        ///     Named group 'playerinfo' returns the entire player info string, i.e.:
        ///     n\syncore\t\3\model\sarge\hmodel\sarge\c1\13\c2\16\hc\100\w\0\l\0\tt\0\tl\0\rp\0\p\3\so\0\pq\0\wp\hmg\ws\sg\cn\\su\0\xcn\\c\
        /// </remarks>
        public Regex ScmdPlayerConfigString { get; private set; }

        /// <summary>
        ///     Regex for finding a player who has connected as issued in a servercommand.
        /// </summary>
        /// <value>
        ///     Regex for finding a player who has connected as issued in a servercommand.
        /// </value>
        /// <remarks>
        ///     This contains a named group, 'player' that has the name of the player who has connected.
        /// </remarks>
        public Regex ScmdPlayerConnected { get; private set; }

        /// <summary>
        ///     Regex for finding a player who has disconnected as issued in a servercommand.
        /// </summary>
        /// <value>
        ///     Regex for finding a player who has disconnected as issued in a servercommand.
        /// </value>
        /// <remarks>
        ///     This contains a named group, 'player' that has the name of the player who disconnected.
        /// </remarks>
        public Regex ScmdPlayerDisconnected { get; private set; }

        /// <summary>
        /// Regex for finding a player who has joined the spectators as issued in a servercommand.
        /// </summary>
        /// <value>
        /// Regex for finding a player who has joined the spectators as issued in a servercommand.
        /// </value>
        /// <remarks>
        /// This contains a named group 'player' that has the name of the player who joined the spectators.
        /// </remarks>
        public Regex ScmdPlayerJoinedSpectators { get; private set; }

        /// <summary>
        ///     Regex for finding a player who was kicked as issued in a servercommand.
        /// </summary>
        /// <value>
        ///     Regex for finding a player who was kicked as issued in a servercommand.
        /// </value>
        /// <remarks>
        ///     This contains a named group, 'player' that has the name of the player being kicked.
        /// </remarks>
        public Regex ScmdPlayerKicked { get; private set; }

        /// <summary>
        ///     Regex for finding a player who has ragequit as issued in a servercommand.
        /// </summary>
        /// <value>
        ///     Regex for finding a player who has ragequit as issued in a servercommand.
        /// </value>
        /// <remarks>
        ///     This contains a named group, 'player' that has the name of the player who has ragequit.
        /// </remarks>
        public Regex ScmdPlayerRageQuits { get; private set; }

        /// <summary>
        ///     Regex for getting the details of the vote that was just called.
        /// </summary>
        /// <value>
        ///     Regex for getting the details of the vote that was just called.
        /// </value>
        /// <remarks>
        ///     This contains two named groups, 'votetype' and 'votearg'.
        ///     Votetype corresponds to the types of votes, which can be:
        ///     map, kick, shuffle, g_gametype, ruleset, nextmap, clientkick,
        ///     teamsize, timelimit, map_restart, cointoss, fraglimit; some of which
        ///     may be disabled (i.e. g_gametype, ruleset, etc) depending on the server.
        ///     Votearg corresponds to the argument passed to the vote type, i.e.
        ///     a vote of "map campgrounds" has votetype map and argument campgrounds,
        ///     whereas a votetype shuffle has no args.
        /// </remarks>
        public Regex ScmdVoteCalledDetails { get; private set; }

        /// <summary>
        ///     Regex for getting the full name (clan tag and player name) of the player calling a vote.
        /// </summary>
        /// <value>
        ///     Regex for getting the full name (clan tag and player name) of the player calling a vote.
        /// </value>
        /// <remarks>
        ///     This contains one named group 'clanandplayer' that includes the full name (clan tag & player)
        ///     of the player who called the vote.
        /// </remarks>
        public Regex ScmdVoteCalledTagAndPlayer { get; private set; }

        /// <summary>
        ///     Regex for matching the final result of a vote.
        /// </summary>
        /// <value>
        ///     Regex for matching the final result of a vote.
        /// </value>
        /// <remarks>
        ///     Contains a named group 'result' which is either passed or failed.
        /// </remarks>
        public Regex ScmdVoteFinalResult { get; private set; }

        /// <summary>
        ///     Regex for getting the number of players who have voted no to the current vote.
        /// </summary>
        /// <value>
        ///     Regex for getting the number of players who have voted no to the current vote.
        /// </value>
        /// <remarks>
        ///     This contains a named group called 'novotes' which simply contains the number of no votes
        ///     as a string with the quotation marks already removed.
        /// </remarks>
        public Regex ScmdVoteNumNoVotes { get; private set; }

        /// <summary>
        ///     Regex for getting the number of players who have voted yes to the current vote.
        /// </summary>
        /// <value>
        ///     Regex for getting the number of players who have voted yes to the current vote.
        /// </value>
        /// <remarks>
        ///     This contains a named group called 'yesvotes' which simply contains the number of yes votes
        ///     as a string with the quotation marks already removed.
        /// </remarks>
        public Regex ScmdVoteNumYesVotes { get; private set; }

        /// <summary>
        ///     Regex for matching the caret and color of a player name and/or chat message.
        /// </summary>
        /// <value>
        ///     Regex for matching the caret and color of a player name and/or chat message.
        /// </value>
        public Regex UtilCaretColor { get; private set; }
    }
}