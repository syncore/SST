using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SSB.Core;
using SSB.Database;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Modules
{
    /// <summary>
    /// Account date limiter module.
    /// </summary>
    public class AccountDate : ModuleManager, ISsbModule
    {
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1003.1 Safari/535.19 Awesomium/1.7.1";

        private readonly RegistrationDates _regDateDb;
        private readonly SynServerBot _ssb;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountDate"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccountDate(SynServerBot ssb)
            : base(ssb)
        {
            _ssb = ssb;
            _regDateDb = new RegistrationDates();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the minimum days that an account must be registered.
        /// </summary>
        /// <value>
        /// The minimum days that an account must be registered.
        /// </value>
        public int MinimumDaysRequired { get; set; }

        /// <summary>
        /// Asynchrounously scrapes the Quakelive.com site to retrieve the player's registration date.
        /// </summary>
        /// <returns><c>true</c> if the player was found on QL site, otherwise <c>false</c>.</returns>
        /// <remarks>Unfortunately, this is the best way to do this, since there is no exposed QL API for this.
        /// </remarks>
        public async Task<DateTime> GetUserRegistrationDateFromQl(string user)
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

                    httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
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
                            <img src="http://cdn.quakelive.com/web/2014091104/images/profile/titles/ttl_vitalstats_v2014091104.0.png" alt="Vital Stats" width="108" height="13" class="prf_title" />
                            <br />
                            <p>
                            <b>Member Since:</b> Sep. 19, 2014<br />
                            .
                            .
                            </div>
                            */

                            var elem = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class,'prf_vitals')]");
                            var pg = elem.SelectSingleNode("p");
                            string regdateStr = pg.ChildNodes[2].InnerText.Trim();
                            DateTime.TryParse(regdateStr, out registeredDate);
                            Debug.WriteLine("Got date text: " + regdateStr);
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

        /// <summary>
        /// Loads this instance.
        /// </summary>
        public void Load()
        {
            Load(GetType());
        }

        /// <summary>
        /// Runs the user date check on all current players.
        /// </summary>
        /// <param name="players">The players.</param>
        public async Task RunUserDateCheck(Dictionary<string, PlayerInfo> players)
        {
            foreach (var player in players)
            {
                await RunUserDateCheck(player.Key);
            }
        }

        /// <summary>
        /// Unloads this instance.
        /// </summary>
        public void Unload()
        {
            Unload(GetType());
        }

        /// <summary>
        /// Gets the user's registration date.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's registration date as a DateTime object.</returns>
        private async Task<DateTime> GetUserRegistrationDate(string user)
        {
            // See if the user already exists
            _regDateDb.RetrieveAllUsers();
            DateTime registeredDate;
            if (_regDateDb.UsersWithDates.TryGetValue(user, out registeredDate))
            {
                return registeredDate;
            }
            else
            {
                // User doesn't exist in our db, retrieve from QL.
                registeredDate = await GetUserRegistrationDateFromQl(user);
                _regDateDb.AddUserToDb(user, registeredDate);
            }
            return registeredDate;
        }

        /// <summary>
        /// Runs the user date check on a given player.
        /// </summary>
        /// <param name="user">The user.</param>
        private async Task RunUserDateCheck(string user)
        {
            DateTime date = await GetUserRegistrationDate(user);
            VerifyUserDate(user, date);
        }

        /// <summary>
        /// Verifies the user's registration date and kicks the user if the requirement is not met.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="regDate">The user's registration date.</param>
        private void VerifyUserDate(string user, DateTime regDate)
        {
            DateTime now = DateTime.Now;
            if ((now - regDate).TotalDays < MinimumDaysRequired)
            {
                Debug.WriteLine("User {0} has created account within the last {1} days. Date created: {2}. Kicking...",
                    user, MinimumDaysRequired, regDate);
                _ssb.QlCommands.QlCmdKickban(user);
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[=> KICK]: ^1{0}^7 (QL account date:^1 {1}^7)'s account is too new and does not meet the server limit of^2 {2}^7 days ^3*",
                        user, regDate.ToString("d"), MinimumDaysRequired));
            }
        }
    }
}