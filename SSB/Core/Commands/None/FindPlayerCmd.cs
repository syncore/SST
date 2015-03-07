﻿using System;
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
    ///     Command: find the QL server location for a user-specified player.
    /// </summary>
    public class FindPlayerCmd : IBotCommand
    {
        private readonly int _qlMinArgs = 2;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;
        private bool _isIrcAccessAllowed = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FindPlayerCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public FindPlayerCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

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
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise
        /// <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (!Helpers.IsValidQlUsernameFormat(Helpers.GetArgVal(c, 1), false))
            {
                StatusMessage = string.Format("^1[ERROR] {0}^7 contains invalid characters (only a-z,0-9,- allowed)",
                            Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);
                return false;
            }

            // Search all the public servers (private "0")
            var firstQuery = await DoServerQuery(Helpers.GetArgVal(c, 1), false);
            FilterObject secondQuery;
            if (firstQuery == null)
            {
                StatusMessage = "^1[ERROR]^3 Problem querying servers, try again later.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            // Player was NOT found on public servers
            if (firstQuery.servers.Count == 0)
            {
                // ...so now proceed to now search the private servers (private "1")
                secondQuery = await DoServerQuery(Helpers.GetArgVal(c, 1), true);
            }
            // Player WAS found on the public servers
            else
            {
                await DisplaySearchResults(c, firstQuery.servers.First());
                return true;
            }
            if (secondQuery == null)
            {
                StatusMessage = "^1[ERROR]^3 Problem querying servers, try again later.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            // Player was NOT found on the private servers either, thus player is not playing at all
            if (secondQuery.servers.Count == 0)
            {
                StatusMessage = string.Format("^6[PLAYERFINDER] ^3{0}^7 was not" +
                                              " found on any Quake Live servers.", Helpers.GetArgVal(c, 1));
                await SendServerSay(c, StatusMessage);
                return true;
            }
            // Player WAS found on a private server.
            await DisplaySearchResults(c, secondQuery.servers.First());
            return true;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <name> ^7- name is without the clan tag.",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        /// Displays the search results.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="server">The server.</param>
        private async Task DisplaySearchResults(CmdArgs c, Server server)
        {
            var qlLoc = new QlLocations();
            var country = qlLoc.GetLocationNameFromId(server.location_id);
            StatusMessage = string.Format("^6[PLAYERFINDER] ^7Found ^3{0}^7 on [^5{1}^7] {2} (^2{3}/{4}^7) @ ^4{5}",
                        Helpers.GetArgVal(c, 1), country, server.map, server.num_clients, server.max_clients,
                        server.host_address);
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Does the server query.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="usePrivate">
        ///     if set to <c>true</c> then query private (private 1) servers. otherwise
        ///     query public (private 0) servers.
        /// </param>
        /// <returns>The results of the query as a <see cref="FilterObject" /> object.</returns>
        private async Task<FilterObject> DoServerQuery(string player, bool usePrivate)
        {
            var qlInfoRetriever = new QlRemoteInfoRetriever();
            var query = await qlInfoRetriever.GetServerDataFromFilter(MakeEncodedFilter(player, usePrivate));
            return query;
        }

        /// <summary>
        ///     Makes the encoded filter.
        /// </summary>
        /// <param name="player">The player to find.</param>
        /// <param name="usePrivate">
        ///     if set to <c>true</c> then query private (private 1) servers. otherwise
        ///     query public (private 0) servers.
        /// </param>
        /// <returns>The base64-encoded JSON as a string.</returns>
        private string MakeEncodedFilter(string player, bool usePrivate)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(
                "{\"filters\":{\"group\":\"all\",\"game_type\":\"any\",\"arena\":\"any\",\"state\":\"POPULATED\"," +
                "\"difficulty\":\"any\",\"location\":\"ALL\",\"private\":\"" + (usePrivate ? "1" : "0") +
                "\",\"premium_only\":0,\"ranked\":\"any\",\"invitation_only\":0},\"arena_type\":\"\",\"players\":[\"" +
                player + "\"]," +
                "\"game_types\":[5,4,3,0,1,2,9,10,11,8,6,7],\"ig\":0}"));
        }
    }
}