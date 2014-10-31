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

            //configstring playerinfo:
            // cs 549 "n\JoeJoe\t\3\model\mynx\hmodel\mynx\c1\12\c2\6\hc\100\w\0\l\0\skill\ 5.00\tt\0\tl\0\rp\1\p\0\so\0\pq\0\wp\hmg\ws\sg\cn\\su\0\xcn\\c\"
            expr = new RegexCompilationInfo(@"cs 5(?<id>[2-5][0-9]) (?<playerinfo>.*)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerInfo", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            // command: configstrings - Find player and team number (n\Lucy\t\1, where n\Playername\t\team#) after issuing 'configstrings' command
            //###: n\Lucy\t\1\model\lucy\hmodel\lucy\c1\20\c2\15\hc\100\w\0\l\0\skill\5.00\tt\5\tl\0\rp\1\p\0\so\0\pq\0\wp\rl\ws\sg\cn\\su\0\xcn\\c\
            expr = new RegexCompilationInfo(@"(n\\[a-zA-Z]+.(t)\\\d)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerandTeam", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // command: configstrings - Find player name only after issuing 'configstrings' command
            expr = new RegexCompilationInfo(@"([a-zA-Z])\w+",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerNameOnly", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            // command: configstrings - Find player team only after issuing 'configstrings' command
            expr = new RegexCompilationInfo(@"\\t\\\d",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerTeamOnly", "SSB.External.Parser",
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

            // event: player connection ("player has connected")
            // This requires the multiline (RegexOptions.Multiline) option and ^ for proper parsing
            expr = new RegexCompilationInfo(@"print ""(\w+ connected)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "evPlayerConnected", "SSB.External.Parser",
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

            // servercommand: player connected
            expr = new RegexCompilationInfo(@"print ""(?<player>.+) connected",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerConnected", "SSB.External.Parser",
                true);
            compilationList.Add(expr);
            
            // servercommand: player was kicked
            expr = new RegexCompilationInfo(@"print ""(?<player>.+) was kicked",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerKicked", "SSB.External.Parser",
                true);
            compilationList.Add(expr);

            //servercommand: player disconnected
            expr = new RegexCompilationInfo(@"print ""(?<player>.+) disconnected",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerDisconnected", "SSB.External.Parser",
                true);
            compilationList.Add(expr);


            //servercommand: player ragequits
            expr = new RegexCompilationInfo(@"print ""(?<player>.+) ragequits",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "scmdPlayerRagequits", "SSB.External.Parser",
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
            expr = new RegexCompilationInfo(@"g_gametype (?<gametype>.+)",
               RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "cvarGameType",
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