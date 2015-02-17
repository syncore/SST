using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Model.QuakeLiveApi;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    /// Command: find the QL server location for a user-specified player.
    /// </summary>
    public class FindPlayerCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindPlayerCmd"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public FindPlayerCmd(SynServerBot ssb)
        {
            _ssb = ssb;
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
                "^1[ERROR]^3 Usage: {0}{1} <name> ^7- name is without the clan tag.",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!Helpers.IsValidQlUsernameFormat(c.Args[1], false))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^1[ERROR] {0}^7 contains invalid characters (only a-z,0-9,- allowed)",
                            c.Args[1]));
                return;
            }

            // Search all the public servers (private "0")
            var firstQuery = await DoServerQuery(c.Args[1], false);
            FilterObject secondQuery;
            if (firstQuery == null)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Problem querying servers, try again later.");
                return;
            }
            // Player was NOT found on public servers
            if (firstQuery.servers.Count == 0)
            {
                // ...so now proceed to now search the private servers (private "1")
                secondQuery = await DoServerQuery(c.Args[1], true);
            }
            // Player WAS found on the public servers
            else
            {
                await DisplaySearchResults(c.Args[1], firstQuery.servers.First());
                return;
            }
            if (secondQuery == null)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Problem querying servers, try again later.");
                return;
            }
            // Player was NOT found on the private servers either, thus player is not playing at all
            if (secondQuery.servers.Count == 0)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^6[PLAYERFINDER] ^3{0}^7 was not found on any Quake Live servers.", c.Args[1]));
            }
            // Player WAS found on a private server.
            else
            {
                await DisplaySearchResults(c.Args[1], secondQuery.servers.First());
            }
        }

        /// <summary>
        /// Displays the search results.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="server">The server.</param>
        private async Task DisplaySearchResults(string player, Server server)
        {
            var qlLoc = new QlLocations();
            string country = qlLoc.GetLocationNameFromId(server.location_id);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^6[PLAYERFINDER] ^7Found ^3{0}^7 on [^5{1}^7] {2} (^2{3}/{4}^7) @ ^4{5}",
                    player, country, server.map, server.num_clients, server.max_clients, server.host_address));
        }

        /// <summary>
        /// Does the server query.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="usePrivate">if set to <c>true</c> then query private (private 1) servers. otherwise
        /// query public (private 0) servers.</param>
        /// <returns>The results of the query as a <see cref="FilterObject"/> object.</returns>
        private async Task<FilterObject> DoServerQuery(string player, bool usePrivate)
        {
            var qlInfoRetriever = new QlRemoteInfoRetriever();
            var query = await qlInfoRetriever.GetServerDataFromFilter(MakeEncodedFilter(player, usePrivate));
            return query;
        }

        /// <summary>
        /// Makes the encoded filter.
        /// </summary>
        /// <param name="player">The player to find.</param>
        /// <param name="usePrivate">if set to <c>true</c> then query private (private 1) servers. otherwise
        /// query public (private 0) servers.</param>
        /// <returns>The base64-encoded JSON as a string.</returns>
        private string MakeEncodedFilter(string player, bool usePrivate)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(
                "{\"filters\":{\"group\":\"all\",\"game_type\":\"any\",\"arena\":\"any\",\"state\":\"POPULATED\"," +
                "\"difficulty\":\"any\",\"location\":\"ALL\",\"private\":\"" + (usePrivate ? "1" : "0") +
                "\",\"premium_only\":0,\"ranked\":\"any\",\"invitation_only\":0},\"arena_type\":\"\",\"players\":[\"" + player + "\"]," +
                "\"game_types\":[5,4,3,0,1,2,9,10,11,8,6,7],\"ig\":0}"));
        }
    }
}