using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ParserDllGenerator
{
    public class ParserDllGenerator
    {
        /// <summary>
        ///     Defines the entry point of the application.
        /// </summary>
        /// <remarks>
        ///     When this runs, it generates an assembly (.dll) containing the regexes that SSB will use.
        ///     The SSB Visual Studio solution is currently set to silently execute this Main method to generate the .dll
        ///     Every time the ParserDllGenerator solution successfully builds (before SSB itself builds) to keep the
        ///     referenced assembly up to date.
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
                "cfgStringPlayerInfo", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            

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
                "plPlayerNameAndId", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // event: player disconnection ("player has disconnected")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing when it's not a servercommand
            expr = new RegexCompilationInfo(@"^\w+\s+(disconnected)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "evPlayerDisconnected",
                "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // event: player kicked ("player was kicked")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing when it's not a servercommand
            expr = new RegexCompilationInfo(@"^\w+\s+(was kicked)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "evPlayerKicked", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // event: player ragequits ("player ragequits")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^\w+\s+(ragequits)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "evPlayerRageQuit", "SSB.External.Parser",
                true);
            compilationList.Add(expr);


            // event: map loaded
            expr = new RegexCompilationInfo(@"(\d+ files in pk3 files)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "evMapLoaded", "SSB.External.Parser", true);
            compilationList.Add(expr);

            //servercommand: player's configstring
            // serverCommand: ## : cs 549 "n\JoeJoe\t\3\model\mynx\hmodel\mynx\c1\12\c2\6\hc\100\w\0\l\0\skill\ 5.00\tt\0\tl\0\rp\1\p\0\so\0\pq\0\wp\hmg\ws\sg\cn\\su\0\xcn\\c\"
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 5(?<id>[2-5][0-9]) (?<playerinfo>.*)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerConfigString", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // servercommand: chat message - named group 'fullplayerandmsg' contains clan tag + playername + msg
            // note: there is a unicode character 19 between the end of the player name and the colon
            // serverCommand: 4 : chat "00 player: hello" - would match: player\u0019: hello
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : chat ""\d+ (?<fullplayerandmsg>.+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdChatMessage", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            // servercommand: player connected
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) connected",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerConnected", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            // servercommand: player was kicked
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) was kicked",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerKicked", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player disconnected
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) disconnected",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerDisconnected", "SSB.External.Parser",
                true);
            compilationList.Add(expr);


            //servercommand: player ragequits
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<player>.+) ragequits",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerRagequits", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - player who called vote (includes clantag and player name)
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""(?<clanandplayer>.+) called a vote",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteCalledTagAndPlayer", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            //servercommand: vote called - details
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 9 ""(?<votetype>.+) ""*(?<votearg>.*?)""*""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteCalledDetails", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - number of yes votes
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 10 ""(?<yesvotes>\d+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteNumYesVotes", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            //servercommand: vote called - number of no votes
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 11 ""(?<novotes>\d+)""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteNumNoVotes", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: vote called - vote result
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : print ""Vote (?<result>passed|failed)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdVoteFinalResult", "SSB.External.Parser",
                true);
            compilationList.Add(expr);


            //servercommand: game is starting/ending
            expr = new RegexCompilationInfo(@"serverCommand: \d+ : cs 5 ""(?<time>.+)""",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdGameStateChange", "SSB.External.Parser",
               true);
            compilationList.Add(expr);

            // Specific cvar values:

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
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "cvarServerPublicId",
                "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // name - Find name after issuing 'name'
            expr = new RegexCompilationInfo(@"(""name""\sis:""\w+"")",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "cvarBotAccountName",
               "SSB.External.Parser",
               true);
            compilationList.Add(expr);

            // g_gametype - extract gametype from serverinfo command
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^g_gametype (?<gametype>.+)",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "cvarGameType",
               "SSB.External.Parser",
               true);
            compilationList.Add(expr);

            // g_gameState - extract gamestate from serverinfo command
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"^g_gameState (?<gamestate>.+)",
               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant, "cvarGameState",
               "SSB.External.Parser",
               true);
            compilationList.Add(expr);

            // Misc regex (util, etc)
            expr = new RegexCompilationInfo(@"\^[0-9]",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "utilCaretColor",
               "SSB.External.Parser",
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