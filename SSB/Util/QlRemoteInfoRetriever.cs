using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Enums;
using SSB.Model.QuakeLiveApi;

namespace SSB.Util
{
    /// <summary>
    ///     Class that remotely retrieves various details from the Quake Live API when such details are not represented by a
    ///     console command, cvar, etc.
    /// </summary>
    public class QlRemoteInfoRetriever
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[API:QUAKELIVE]";

        /// <summary>
        ///     Gets the server's gamestate from the QL API.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>A <see cref="QlGameStates" /> enum containing the server's gametype.</returns>
        public async Task<QlGameStates> GetGameState(string serverId)
        {
            var server = await QueryQlServer(serverId);
            var gamestate = QlGameStates.Unspecified;
            var state = server.g_gamestate;
            switch (state)
            {
                case "PRE_GAME":
                    gamestate = QlGameStates.Warmup;
                    break;

                case "COUNT_DOWN":
                    gamestate = QlGameStates.Countdown;
                    break;

                case "IN_PROGRESS":
                    gamestate = QlGameStates.InProgress;
                    break;
            }
            Log.Write("The server's gamestate is: " + gamestate, _logClassType, _logPrefix);
            return gamestate;
        }

        /// <summary>
        ///     Gets the server's gametype from the QL API.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>A <see cref="QlGameTypes" /> enum containing the server's gametype.</returns>
        public async Task<QlGameTypes> GetGameType(string serverId)
        {
            var server = await QueryQlServer(serverId);
            var gtNum = server.game_type;
            var gametype = (QlGameTypes) gtNum;

            Log.Write("This server's gametype is: " + gametype, _logClassType, _logPrefix);
            return gametype;
        }

        /// <summary>
        ///     Retrieves Quake Live server data from a public filter URL.
        /// </summary>
        /// <param name="filter">The base64 encoded json filter.</param>
        /// <returns>The server data as a root <see cref="FilterObject" /></returns>
        public async Task<FilterObject> GetServerDataFromFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                Log.Write("No encoded filter was specified!", _logClassType, _logPrefix);
                return null;
            }
            try
            {
                var query = new RestApiQuery();
                var url = string.Format("http://www.quakelive.com/browser/list?filter={0}&_={1}", filter,
                    Math.Truncate((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds));

                var filterObj = await (query.QueryRestApiAsync<FilterObject>(url));

                if (filterObj != null)
                {
                    Log.Write("Got filter information for base64 encoded filter.", _logClassType, _logPrefix);
                }

                return filterObj;
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem retrieving filter object from encoded filter: " + ex.Message,
                    _logClassType, _logPrefix);
                return null;
            }
        }

        /// <summary>
        ///     Queries a Quake Live server.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>The server's details as a <see cref="Server" /> object.</returns>
        public async Task<Server> QueryQlServer(string serverId)
        {
            if (string.IsNullOrEmpty(serverId))
            {
                Log.Write("No server ID was specified!", _logClassType, _logPrefix);
                return null;
            }
            try
            {
                var query = new RestApiQuery();
                var url = "http://www.quakelive.com/browser/details?ids=" + serverId;
                var serverList = await (query.QueryRestApiAsync<IList<Server>>(url));

                // QL always returns a collection no matter what. We're only querying one server so get first.
                if (serverList.Count != 0)
                {
                    Log.Write(string.Format("Got server information for server with id {0}",
                        serverId), _logClassType, _logPrefix);
                }

                return serverList.First();
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem returning server information: " + ex.Message,
                    _logClassType, _logPrefix);
                return null;
            }
        }
    }
}