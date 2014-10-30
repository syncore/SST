using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Model.QlRanks;

namespace SSB.Util
{
    /// <summary>
    ///     This class is responsible for retrieving player elo data from the QLRanks API.
    /// </summary>
    public class QlRanksEloRetriever
    {
        /// <summary>
        ///     Asynchronously performs the QLRanks data retrieval for multiple players.
        /// </summary>
        /// <returns>QlRanks object</returns>
        public async Task<QlRanks> DoQlRanksRetrievalAsync<T>(IEnumerable<T> players)
        {
            QlRanks q = await GetQlRanksObjectAsync(players);
            return q;
        }

        /// <summary>
        ///     Asynchronously performs the QLRanks data retrieval for a single player.
        /// </summary>
        /// <returns>QlRanks object</returns>
        /// <remarks>This can also handle comma-separated lists of players if need be.</remarks>
        public async Task<QlRanks> DoQlRanksRetrievalAsync(string player)
        {
            QlRanks q;
            // So this can handle lists as well...
            if (player.Contains(","))
            {
                List<string> pList = player.Trim().Split(',').ToList();
                q = await GetQlRanksObjectAsync(pList);
            }
            else
            {
                q = await GetEloDataFromQlRanksApiAsync(player);
            }
            return q;
        }

        /// <summary>
        ///     Asynchronously retrieves the player Elo information from the QLRanks API via HTTP GET request(s).
        /// </summary>
        /// <param name="players">The players.</param>
        /// <returns>QLRanks object</returns>
        private async Task<QlRanks> GetEloDataFromQlRanksApiAsync(string players)
        {
            string url = "http://www.qlranks.com/api.aspx?nick=" + players;
            //string url = "http://10.0.0.7/api.aspx?nick=" + players;

            try
            {
                var query = new RestApiQuery();
                QlRanks qlr = await (query.QueryRestApiAsync<QlRanks>(url));

                return qlr;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to retrieve QlRanks data: " + e.Message);
                return null;
            }
        }

        /// <summary>
        ///     Asynchronously sends the list of players that need elo updates to the QLRanks API then sets the elo data once that
        ///     information is retrieved.
        /// </summary>
        private async Task<QlRanks> GetQlRanksObjectAsync<T>(IEnumerable<T> playersToUpdate)
        {
            IList<T> toUpdate = playersToUpdate as IList<T> ?? playersToUpdate.ToList();
            QlRanks qlr = await GetEloDataFromQlRanksApiAsync(string.Join("+", toUpdate));
            Debug.WriteLine(string.Format("QLRANKS: URL: http://www.qlranks.com/api.aspx?nick={0}",
                string.Join("+", toUpdate)));
            return qlr;
        }
    }
}