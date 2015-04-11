using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ParserDllGenerator
{
    /// <summary>
    /// This class generates an assembly (.dll file) that contains the regular expressions
    /// that SST uses to interpret QL events. The separate assembly method is preferred according to
    /// MSDN's "Best Practices for Regular Expressions in the .NET Framework", particularly the section titled:
    /// "Regular Expressions: Compiled to an Assembly"
    /// (https://msdn.microsoft.com/en-us/library/gg578045%28v=vs.110%29.aspx)
    /// </summary>
    public class ParserDllGenerator
    {
        /// <summary>
        ///     Defines the entry point of the application.
        /// </summary>
        /// <remarks>
        ///     When this runs, it generates an assembly (.dll) containing the regexes that SST will use.
        ///     The SST Visual Studio solution is currently set to silently execute ParserDllGenerator.exe
        ///     to generate the .dll every time the ParserDllGenerator solution successfully builds
        ///     (which is set to build before SST itself builds) to keep the referenced assembly up to date.
        /// </remarks>
        public static void Main()
        {
            RegexCompilationInfo expr;
            var compilationList = new List<RegexCompilationInfo>();

            // command: configstrings - Find player id and player info (team, clantag, full clan, subscriber, etc.)
            // named group 'id' returns the two digit number after the 5, i.e. for 533 it will return 33. Must subtract
            // 29 from this number to get the equivalent player id that can be retrieved from 'players' command.
            // named group 'playerinfo' returns the entire player info string, i.e.:
            // n\syncore\t\3\model\sarge\hmodel\sarge\c1\13\c2\16\hc\100\w\0\l\0\tt\0\tl\0\rp\0\p\3\so\0\pq\0\wp\hmg\ws\sg\cn\\su\0\xcn\\c\
            expr = new RegexCompilationInfo(@"5(?<id>[2-5][0-9]): (?<playerinfo>.*)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
                "cfgStringPlayerInfo", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // command: configstrings - find the gametype information from the configstrings command
            // named group 'gametype' contains the gametype number (i.e. 4 for ca, 5 for ctf, etc.)
            expr = new RegexCompilationInfo(@"\b0:.+g_gametype\\(?<gametype>\d+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                "cfgStringGameType", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //\b0:.+g_gametype\\(\d+)


            // command: players - Find name and player id after issuing 'players' command
            // This requires the multiline (RegexOptions.Multiline) option
            /*
            ]\players
             0   [CLAN] Klesk
             1   Lucy
             2   Keel
             3   Anarki
             4   Doom
             5   team. Sarge
             6   Orbb
             7   Mynx
             8 * syncore
             */
            expr = new RegexCompilationInfo(@"(^\s+\d+\s\D\W+\w..+)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
                "plPlayerNameAndId", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // event: player disconnection ("player has disconnected")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing when it's not a servercommand
            expr = new RegexCompilationInfo(@"^\w+\s+(disconnected)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "evPlayerDisconnected",
                "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // event: player kicked ("player was kicked")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing when it's not a servercommand
            expr = new RegexCompilationInfo(@"^\w+\s+(was kicked)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "evPlayerKicked", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // event: player ragequits ("player ragequits")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^\w+\s+(ragequits)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "evPlayerRageQuit", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // event: map loaded
            expr = new RegexCompilationInfo(@"(\d+ files in pk3 files)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "evMapLoaded", "SST.External.Parser", true);
            compilationList.Add(expr);

            //servercommand: tinfo
            // this message is constantly sent when the owner plays on the same account as the bot
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : tinfo",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdTinfo", "SST.External.Parser",
               true);
            compilationList.Add(expr);

            //servercommand: player's accuracy data
            // example: serverCommand: 579 : acc  0 0 0 21 17 0 0 0 0 0 0 0 0 0 0
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : acc  (?<accdata>.+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdAccuracy", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player's configstring
            // serverCommand: ## : cs 549 "n\JoeJoe\t\3\model\mynx\hmodel\mynx\c1\12\c2\6\hc\100\w\0\l\0\skill\ 5.00\tt\0\tl\0\rp\1\p\0\so\0\pq\0\wp\hmg\ws\sg\cn\\su\0\xcn\\c\"
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 5(?<id>[2-5][0-9]) (?<playerinfo>.*)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerConfigString", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // servercommand: chat message - named group 'fullplayerandmsg' contains clan tag + playername + msg
            // note: there is a unicode character 19 between the end of the player name and the colon
            // serverCommand: 4 : chat "00 player: hello" - would match: player\u0019: hello
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : chat ""\d+ (?<fullplayerandmsg>.+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdChatMessage", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // servercommand: player connected
            // note: connections include the clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) connected",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerConnected", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // servercommand: player was kicked
            // note: kicks do not include clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) was kicked",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerKicked", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player disconnected
            // note: disconnections do not include the clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) disconnected",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerDisconnected", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player timed out
            // note: time outs, like disconnections, do not include the clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) timed out",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerTimedOut", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player was disconnected due to invalid password
            // note: invalid password disconnects do NOT include the clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) Invalid password",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerInvalidPasswordDisconnect", "SST.External.Parser",
                true);
            compilationList.Add(expr);



            //servercommand: player joined spectators
            // note: spectating includes the clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cp ""(?<player>.+) joined the spectators",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerJoinedSpectators", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player ragequits
            // note: ragequit does not include the clan tag
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) ragequits",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerRagequits", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: match was aborted
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : pcp "".* has aborted the match",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdMatchAborted", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: match endeded due to timelimit being reached
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""Timelimit hit",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdTimelimitHit", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: match ended due to fraglimit, capturelimit, or roundlimit being reached
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print "".* hit the (fraglimit|capturelimit|roundlimit)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdScorelimitHit", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - player who called vote (includes clantag and player name)
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<clanandplayer>.+) called a vote",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteCalledTagAndPlayer", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - details
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 9 ""(?<votetype>.+) ""*(?<votearg>.*?)""*""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteCalledDetails", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - number of yes votes
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 10 ""(?<yesvotes>\d+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteNumYesVotes", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - number of no votes
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 11 ""(?<novotes>\d+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteNumNoVotes", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: intermission (game over) detected
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 14 ""(?<intermissionvalue>.+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdIntermission", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - vote result
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""Vote (?<result>passed|failed)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteFinalResult", "SST.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: gamestate change (more accurate method)
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : (bcs0|cs) 0 (.+g_gameState\\(?<gamestatus>\w+))",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdGameStateChange", "SST.External.Parser",
               true);
            compilationList.Add(expr);

            //servercommand: unncessary pak info (bcs0 1, bcs1 1, bcs2 1...)
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : (bcs\d+) 1.*",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPakInfo", "SST.External.Parser",
               true);
            compilationList.Add(expr);

            //servercommand: gamestate change (\time\# less accurate method)
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 5 ""(?<time>.+)""",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdGameStateTimeChange", "SST.External.Parser",
               true);
            compilationList.Add(expr);

            //servercommand: red team score
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 6 ""(?<redscore>\d+)""",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdRedScore", "SST.External.Parser",
               true);
            compilationList.Add(expr);

            //servercommand: blue team score
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 7 ""(?<bluescore>\d+)""",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdBlueScore", "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // serverinfo cmd - Find sv_gtid after issuing 'serverinfo' command
            /*
            ]/serverinfo
            Server info settings:
            .......
            .......
            .......
            .......
            sv_gtid             710833
            .......
            .......
            */
            // offline test: @"\S\w.adXmitDelay\s+\d+";
            expr = new RegexCompilationInfo(@"sv_gtid (?<serverid>.+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "svInfoServerPublicId",
                "SST.External.Parser",
                true);
            compilationList.Add(expr);

            // standard cvar and its value
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo("^\"(?<cvarname>.+)\" is:\"(?<cvarvalue>.+)\" default:.*",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "cvarNameAndValue",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // g_gametype - extract gametype from serverinfo command
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^g_gametype (?<gametype>.+)",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "svInfoGameType",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // g_gameState - extract gamestate from serverinfo command
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^g_gameState (?<gamestate>.+)",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "svInfoGameState",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // clientinfo - extract the player's state (see whether player is connected to server)
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^state: (?<state>\d+)",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "clInfoConnectionState",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // Misc regex (util, etc)
            expr = new RegexCompilationInfo(@"\^[0-9]",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "utilCaretColor",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // SST disconnection: Cvar_Set2: com_errorMessage (Disconnected from server) in developer mode
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo("^Cvar_Set2: com_errorMessage (Disconnected from server)",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "cvarSetQlDisconnected",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // SST disconnection: "ERROR: Disconnected from server"
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo("^ERROR: Disconnected from server",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "msgErrorDisconnected",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);


            // "Not connected to a server." message
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^Not connected to a server.",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "msgNotConnected",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // "----- R_Init -----" message
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^----- R_Init -----",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "msgRInit",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // "----- finished R_Init -----" message
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^----- finished R_Init -----",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "msgFinishedRInit",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);

            // Z_Malloc crash messages
            //^recursive error after: Z_Malloc
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^recursive error after: Z_Malloc|^\*\* Z_Malloc: failed",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "msgZmallocCrash",
               "SST.External.Parser",
               true);
            compilationList.Add(expr);


            // Generate the assembly
            var compilationArray = new RegexCompilationInfo[compilationList.Count];
            var assemName =
                new AssemblyName("Parser, Version=1.0.0.1000, Culture=neutral, PublicKeyToken=null");
            compilationList.CopyTo(compilationArray);
            Regex.CompileToAssembly(compilationArray, assemName);
        }
    }
}