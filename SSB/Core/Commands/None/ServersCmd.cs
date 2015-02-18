using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    /// Command: display currently populated servers for a user-specified gametype and geographical region.
    /// </summary>
    public class ServersCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;

        private readonly string[] _validGameTypes =
        {
            "ca", "ctf", "duel", "ffa", "ft", "tdm", "race"
        };

        private readonly string[] _validRegions =
        {
            "africa", "asia", "eu", "na", "oceania", "sa"
        };

        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 3;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServersCmd"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ServersCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <gametype> <region>",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!_ssb.Mod.Servers.Active)
            {
                await
                     _ssb.QlCommands.QlCmdSay(
                         string.Format(
                             "^1[ERROR]^3 Servers module has not been loaded. An admin must first load it with:^7 {0}{1} {2}",
                             CommandProcessor.BotCommandPrefix, CommandProcessor.CmdModule,
                             ModuleCmd.ServersArg));
                return;
            }

            if (_ssb.Mod.Servers.LastQueryTime.AddSeconds(_ssb.Mod.Servers.TimeBetweenQueries) > DateTime.Now)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^1[ERROR]^3 Server request was already made in the past {0} seconds. Ignoring.", _ssb.Mod.Servers.TimeBetweenQueries));
                return;
            }

            bool validGametype = _validGameTypes.Contains(c.Args[1]);
            if (!validGametype)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^1[ERROR]^3 Valid gametypes are: {0}", string.Join(", ", _validGameTypes)));
                return;
            }
            bool validRegion = _validRegions.Contains(c.Args[2]);
            if (!validRegion)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^1[ERROR]^3 Valid regions are: {0}", string.Join(", ", _validRegions)));
                return;
            }
            await ListActiveServers(c);
            // Impose limit
            _ssb.Mod.Servers.LastQueryTime = DateTime.Now;
        }

        /// <summary>
        /// Gets the game types from user-specified gametype abreviation.
        /// </summary>
        /// <param name="gtAbreviation">The user-specified gametype abreviation.</param>
        /// <returns>A string array containing the gametype and the gametype array number needed for building
        /// the QL filter. </returns>
        private string[] GetGameTypesFromAbreviation(string gtAbreviation)
        {
            var gtArr = new string[2];
            switch (gtAbreviation)
            {
                case "ca":
                    gtArr[0] = "4";
                    gtArr[1] = "[4]";
                    break;

                case "ctf":
                    gtArr[0] = "3";
                    gtArr[1] = "[5]";
                    break;

                case "duel":
                    gtArr[0] = "7";
                    gtArr[1] = "[1]";
                    break;

                case "ffa":
                    gtArr[0] = "2";
                    gtArr[1] = "[0]";
                    break;

                case "ft":
                    gtArr[0] = "5";
                    gtArr[1] = "[9]";
                    break;

                case "tdm":
                    gtArr[0] = "6";
                    gtArr[1] = "[3]";
                    break;

                case "race":
                    gtArr[0] = "25";
                    gtArr[1] = "[2]";
                    break;
            }
            return gtArr;
        }

        /// <summary>
        /// Gets the location needed for filter building from the user's region input.
        /// </summary>
        /// <param name="region">The region that user has specified.</param>
        /// <returns>The appropriate location string for the QL filter.</returns>
        private string GetLocationFromRegion(string region)
        {
            string location = string.Empty;
            switch (region)
            {
                case "africa":
                    location = "Africa";
                    break;

                case "asia":
                    location = "Asia";
                    break;

                case "eu":
                    location = "Europe";
                    break;

                case "na":
                    location = "North America";
                    break;

                case "oceania":
                    location = "Oceania";
                    break;

                case "sa":
                    location = "South America";
                    break;
            }
            return location;
        }

        /// <summary>
        /// Lists the active servers.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task ListActiveServers(CmdArgs c)
        {
            string[] gameTypeInfo = GetGameTypesFromAbreviation(c.Args[1]);
            string location = GetLocationFromRegion(c.Args[2]);
            string encodedFilter =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes("{\"filters\":{\"group\":\"any\",\"game_type\":\"" +
                                           gameTypeInfo[0] +
                                           "\",\"arena\":\"any\",\"state\":\"POPULATED\",\"difficulty\":\"any\",\"location\":\"" +
                                           location +
                                           "\",\"private\":0,\"premium_only\":0,\"ranked\":\"any\",\"invitation_only\":0},\"arena_type\":\"\",\"players\":[],\"game_types\":" +
                                           gameTypeInfo[1] + ",\"ig\":0}"));

            var qlInfoRetriever = new QlRemoteInfoRetriever();
            var fObj = await qlInfoRetriever.GetServerDataFromFilter(encodedFilter);
            if (fObj == null)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Problem retrieving server list, try again later.");
                return;
            }
            if (fObj.servers.Count == 0)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^4[ACTIVESERVERS]^7 There are ^1NO^7 active ^2{0}^7 servers in ^2{1}",
                    c.Args[1], location));
                return;
            }

            await _ssb.QlCommands.QlCmdSay(string.Format("^4[ACTIVESERVERS]^7 Showing up to ^2{0}^7 active ^2{1}^7 servers in ^2{2}:",
                _ssb.Mod.Servers.MaxServersToDisplay, c.Args[1].ToUpper(), location));
            var qlLoc = new QlLocations();
            for (int i = 0; i < fObj.servers.Count; i++)
            {
                if (i == _ssb.Mod.Servers.MaxServersToDisplay) break;
                string country = qlLoc.GetLocationNameFromId(fObj.servers[i].location_id);
                await _ssb.QlCommands.QlCmdSay(string.Format("^7{0} [^5{1}^7] {2} (^2{3}/{4}^7) @ ^4{5}",
                    (fObj.servers[i].g_needpass == 1 ? "[PW]" : string.Empty), country,
                    fObj.servers[i].map, fObj.servers[i].num_clients, fObj.servers[i].max_clients, fObj.servers[i].host_address));
            }
        }
    }
}