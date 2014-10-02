using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SSB
{
    /// <summary>
    ///     This class generates an assembly containing the compiled regular expressions that the application will use.
    /// </summary>
    public class CompileRegex
    {
        /// <summary>
        /// Generates the regex assembly (.dll).
        /// </summary>
        public void GenerateAssembly()
        {
            RegexCompilationInfo expr;
            var compilationList = new List<RegexCompilationInfo>();

            // command: configstrings - Find player and team number (n\Lucy\t\1, where n\Playername\t\team#) after issuing 'configstrings' command
            //###: n\Lucy\t\1\model\lucy\hmodel\lucy\c1\20\c2\15\hc\100\w\0\l\0\skill\5.00\tt\5\tl\0\rp\1\p\0\so\0\pq\0\wp\rl\ws\sg\cn\\su\0\xcn\\c\
            expr = new RegexCompilationInfo(@"(n\\[a-zA-Z]+.(t)\\\d)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerandTeam", "Utils.Parser",
                true);
            compilationList.Add(expr);

            // command: configstrings - Find player name only after issuing 'configstrings' command
            expr = new RegexCompilationInfo(@"([a-zA-Z])\w+",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerNameOnly", "Utils.Parser",
                true);
            compilationList.Add(expr);

            // command: configstrings - Find player team only after issuing 'configstrings' command
            expr = new RegexCompilationInfo(@"\\t\\\d",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "csPlayerTeamOnly", "Utils.Parser",
                true);
            compilationList.Add(expr);

            // command: players - Find name and player id after issuing 'players' command
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
            expr = new RegexCompilationInfo(@"((\d+\s\D\W+\w..+))",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "plPlayerNameAndId", "Utils.Parser",
                true);
            compilationList.Add(expr);

            // event: player connection ("player has connected")
            expr = new RegexCompilationInfo(@"\w+\s+(connected)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "evPlayerConnected", "Utils.Parser",
                true);
            compilationList.Add(expr);

            // event: player disconnection ("player has disconnected")
            expr = new RegexCompilationInfo(@"\w+\s+(disconnected)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "evPlayerDisconnected",
                "Utils.Parser",
                true);
            compilationList.Add(expr);

            // event: player kicked ("player was kicked")
            expr = new RegexCompilationInfo(@"\w+\s+(was kicked)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "evPlayerKicked", "Utils.Parser",
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
            expr = new RegexCompilationInfo(@"\S\w.gtid\s+\d+",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, "cvarServerPublicId",
                "SSB.Utils.Parser",
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