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
            ScmdVoteCalledDetails = new scmdVoteCalledDetails();
            ScmdVoteCalledTagAndPlayer = new scmdVoteCalledTagAndPlayer();
            ScmdVoteNumYesVotes = new scmdVoteNumYesVotes();
            ScmdVoteNumNoVotes = new scmdVoteNumNoVotes();
            ScmdChatMessage = new scmdChatMessage();
            ScmdVoteFinalResult = new scmdVoteFinalResult();
        }

        /// <summary>
        ///     Regex for finding a player's name and team number after issuing 'configstrings' command.
        /// </summary>
        /// <value>
        ///     Regex for player's name and team number after issuing 'configstrings' command.
        /// </value>
        public Regex CsPlayerAndTeam { get; private set; }

        /// <summary>
        ///     Regex for finding the player info configstring.
        /// </summary>
        /// <value>
        ///     Regex for finding the player inf configstring.
        /// </value>
        /// <remarks>
        ///     This contains the following named groups:
        ///     'id' is the two digit number after the 5 in the configstring from which 29 is to be subtracted.
        ///     'playerinfo' is the rest of the string after the cs 5##
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
        ///     Regex for finding name cvar value after issuing 'name'
        /// </summary>
        /// <value>
        ///     Regex for cvar name after issuing 'name'
        /// </value>
        public Regex CvarBotAccountName { get; private set; }

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
        /// Regex for matching the final result of a vote.
        /// </summary>
        /// <value>
        /// Regex for matching the final result of a vote.
        /// </value>
        /// <remarks>
        /// Contains a named group 'result' which is either passed or failed.
        /// </remarks>
        public Regex ScmdVoteFinalResult { get; private set; }
    }
}