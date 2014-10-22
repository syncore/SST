using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model.QuakeLiveApi;

namespace SSB.Util
{
    /// <summary>
    /// Class that remotely retrieves various details from the Quake Live API when such details are not represented by a console command, cvar, etc.
    /// </summary>
    public class QlRemoteInfoRetriever
    {
        /// <summary>
        /// Gets the server's gamestate from the QL API.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>A <see cref="QlGameStates"/> enum containing the server's gametype.</returns>
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
            Debug.WriteLine("The server's gamestate is: " + gamestate);
            return gamestate;
        }

        /// <summary>
        /// Gets the server's gametype from the QL API.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>A <see cref="QlGameTypes"/> enum containing the server's gametype.</returns>
        public async Task<QlGameTypes> GetGameType(string serverId)
        {
            var server = await QueryQlServer(serverId);
            QlGameTypes gametype;
            int gtNum = server.game_type;
            if (gtNum == 0)
            {
                // Special case for FFA
                gametype = (QlGameTypes)999;
            }
            else
            {
                gametype = (QlGameTypes)gtNum;
            }

            Debug.WriteLine("This server's gametype is: " + gametype);
            return gametype;
        }

        private async Task<Server> QueryQlServer(string serverId)
        {
            if (string.IsNullOrEmpty(serverId))
            {
                Debug.WriteLine("QLAPI: No server ID specified!");
                return null;
            }
            try
            {
                var query = new RestApiQuery();
                var url = "http://www.quakelive.com/browser/details?ids=" + serverId;
                IList<Server> serverList = await (query.QueryRestApiAsync<IList<Server>>(url));

                return serverList.First();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("QLAPI: Unable to return server information: " + ex.Message);
                return null;
            }
        }
    }
}