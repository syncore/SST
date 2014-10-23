using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SSB.Database;

namespace SSB.Util
{
    /// <summary>
    ///     Class responsible for scraping the QL website for a user's account registration date.
    /// </summary>
    public class QlAccountDateChecker
    {
        private readonly RegistrationDates _regDateDb;

        private string _userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1003.1 Safari/535.19 Awesomium/1.7.1";

        /// <summary>
        ///     Initializes a new instance of the <see cref="QlAccountDateChecker" /> class.
        /// </summary>
        public QlAccountDateChecker()
        {
            _regDateDb = new RegistrationDates();
        }

        /// <summary>
        ///     Gets the user's registration date.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's registration date as a DateTime object.</returns>
        public async Task<DateTime> GetUserRegistrationDate(string user)
        {
            DateTime registeredDate;
            // See if the user already exists in internal database
            if (_regDateDb.GetRegistrationDate(user) != default(DateTime))
            {
                registeredDate = _regDateDb.GetRegistrationDate(user);
                return registeredDate;
            }
            // User doesn't exist in our db, retrieve from QL.
            registeredDate = await GetUserRegistrationDateFromQl(user);
            if (registeredDate != default(DateTime))
            {
                _regDateDb.AddUserToDb(user, registeredDate);
            }
            return registeredDate;
        }

        /// <summary>
        ///     Asynchrounously scrapes the Quakelive.com site to retrieve the player's registration date.
        /// </summary>
        /// <returns><c>true</c> if the player was found on QL site, otherwise <c>false</c>.</returns>
        /// <remarks>
        ///     Unfortunately, this is the best way to do this, since there is no exposed QL API for this.
        /// </remarks>
        private async Task<DateTime> GetUserRegistrationDateFromQl(string user)
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler);
            string playerurl = "http://www.quakelive.com/profile/summary/" + user.ToLowerInvariant();
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
                    HttpResponseMessage response = await httpClient.GetAsync(playerurl);
                    response.EnsureSuccessStatusCode();

                    using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            string result = sr.ReadToEnd();
                            var htmlDocument = new HtmlDocument();
                            htmlDocument.LoadHtml(result);
                            /* <div class="prf_vitals">
                            <img src="http://cdn.quakelive.com/web/2014091104/images/profile/titles/ttl_vitalstats_v2014091104.0.png" alt="Vital Stats" width="108" height="13" class="prf_title" />
                            <br />
                            <p>
                            <b>Member Since:</b> Sep. 19, 2014<br />
                            .
                            .
                            </div>
                            */

                            HtmlNode elem =
                                htmlDocument.DocumentNode.SelectSingleNode(
                                    "//div[contains(@class,'prf_vitals')]");
                            if (elem != null)
                            {
                                HtmlNode pg = elem.SelectSingleNode("p");

                                string regdateStr = pg.ChildNodes[2].InnerText.Trim();
                                DateTime.TryParse(regdateStr, out registeredDate);
                                Debug.WriteLine("Got date text: " + regdateStr);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error accessing Quake Live website: " + e.Message);
                }
            }
            return registeredDate;
        }
    }
}