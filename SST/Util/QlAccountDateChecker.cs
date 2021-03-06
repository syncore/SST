﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SST.Database;

namespace SST.Util
{
    /// <summary>
    /// Class responsible for scraping the QL website for a user's account registration date.
    /// </summary>
    public class QlAccountDateChecker
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[ACCOUNTDATECHECKER]";
        private readonly DbRegistrationDates _regDateDb;

        private readonly string _userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1003.1 Safari/535.19 Awesomium/1.7.1";

        /// <summary>
        /// Initializes a new instance of the <see cref="QlAccountDateChecker"/> class.
        /// </summary>
        public QlAccountDateChecker()
        {
            _regDateDb = new DbRegistrationDates();
        }

        /// <summary>
        /// Gets the user's registration date.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's registration date as a DateTime object.</returns>
        public async Task<DateTime> GetUserRegistrationDate(string user)
        {
            // See if the user already exists in internal database
            var registeredDate = _regDateDb.GetRegistrationDate(user);
            if (registeredDate != default(DateTime)) return registeredDate;

            // User doesn't exist in our db, retrieve from QL.
            registeredDate = await GetUserRegistrationDateFromQl(user);
            if (registeredDate != default(DateTime))
            {
                _regDateDb.AddUserToDb(user, registeredDate);
            }
            return registeredDate;
        }

        /// <summary>
        /// Asynchrounously scrapes the Quakelive.com site to retrieve the player's registration date.
        /// </summary>
        /// <returns><c>true</c> if the player was found on QL site, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Unfortunately, this is the best way to do this, since there is no exposed QL API for this.
        /// </remarks>
        private async Task<DateTime> GetUserRegistrationDateFromQl(string user)
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler);
            var playerurl = "http://www.quakelive.com/profile/summary/" + user.ToLowerInvariant();
            var registeredDate = new DateTime();
            //var playerurl = "http://10.0.0.7/datetest.html";

            using (httpClient)
            {
                try
                {
                    if (httpClientHandler.SupportsAutomaticDecompression)
                    {
                        httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip |
                                                                   DecompressionMethods.Deflate;
                    }

                    httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
                    var response = await httpClient.GetAsync(playerurl);
                    response.EnsureSuccessStatusCode();

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            var result = sr.ReadToEnd();
                            var htmlDocument = new HtmlDocument();
                            htmlDocument.LoadHtml(result);
                            /* <div class="prf_vitals">
                            <img src="http://cdn.quakelive.com/web/2014091104/images/profile/titles/ttl_vitalstats_v2014091104.0.png"
                             * alt="Vital Stats" width="108" height="13" class="prf_title" />
                            <br />
                            <p>
                            <b>Member Since:</b> Sep. 19, 2014<br />
                            .
                            .
                            </div>
                            */

                            var elem =
                                htmlDocument.DocumentNode.SelectSingleNode(
                                    "//div[contains(@class,'prf_vitals')]");
                            if (elem != null)
                            {
                                var pg = elem.SelectSingleNode("p");

                                var regdateStr = pg.ChildNodes[2].InnerText.Trim();
                                DateTime.TryParse(regdateStr, out registeredDate);

                                Log.Write(string.Format("Got account date for {0} from remote site: {1}",
                                    user, regdateStr), _logClassType, _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.WriteCritical("Error accessing Quake Live website: " + e.Message,
                        _logClassType, _logPrefix);
                }
            }
            return registeredDate;
        }
    }
}
