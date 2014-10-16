using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SSB.Core;
using SSB.Database;
using SSB.Interfaces;

namespace SSB.Modules
{
    /// <summary>
    /// Account date limiter module.
    /// </summary>
    public class AccountDate : ModuleManager, ISsbModule
    {
        private readonly SynServerBot _ssb;
        private readonly RegistrationDates _regDateDb;
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1003.1 Safari/535.19 Awesomium/1.7.1";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountDate"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccountDate(SynServerBot ssb) : base(ssb)
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
        /// Loads this instance.
        /// </summary>
        public void Load()
        {
            Load(GetType());
        }

        /// <summary>
        /// Unloads this instance.
        /// </summary>
        public void Unload()
        {
            Unload(GetType());
        }

        /// <summary>
        /// Asynchrounously scrapes the Quakelive.com site to retrieve the player's registration date.
        /// </summary>
        /// <returns><c>true</c> if the player was found on QL site, otherwise <c>false</c>.</returns>
        /// <remarks>Unfortunately, this is the best way to do this, since there is no exposed QL API for this.
        /// </remarks>
        private async Task<string> GetUserRegistrationDateFromQl(string user)
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler);
            var playerurl = "http://www.quakelive.com/profile/summary/" + user.ToLowerInvariant();
            var regdate = string.Empty;
            //var playerurl = "http://10.0.0.7/parsetest.html";

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
                            regdate = pg.ChildNodes[1].InnerText.Trim();
                            Debug.WriteLine("Got date text: " + regdate);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error accessing Quake Live website: " + e.Message);
                }
            }
            return regdate;
        }
        
        public async Task CheckUserRegistrationDate(string user)
        {
            // See if the user already exists
            string dateStr = string.Empty;
            DateTime registeredDate;
            if (_regDateDb.UsersWithDates.TryGetValue(user, out dateStr))
            {
                if (DateTime.TryParse(dateStr, out registeredDate))
                {
                    
                } 
            }
            else
            {
                dateStr = await GetUserRegistrationDateFromQl(user);
                
                if (!string.IsNullOrEmpty(dateStr))
                {
                    _regDateDb.AddUserToDb(user, dateStr);
                }
            }
         }
        
    }
}
