using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SSB.Core.Commands.Admin;
using SSB.Database;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Limits
{
    /// <summary>
    ///     Class that handles account date limiting.
    /// </summary>
    public class AccountDateLimit : ILimit
    {
        private readonly RegistrationDates _regDateDb;
        private readonly SynServerBot _ssb;
        private int _minLimitArgs = 3;

        private string _userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1003.1 Safari/535.19 Awesomium/1.7.1";

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountDateLimit" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccountDateLimit(SynServerBot ssb)
        {
            _ssb = ssb;
            _regDateDb = new RegistrationDates();
        }

        /// <summary>
        ///     Gets or sets the minimum days that an account must be registered.
        /// </summary>
        /// <value>
        ///     The minimum days that an account must be registered.
        /// </value>
        public int MinimumDaysRequired { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the account date limit is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the account date limit is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsLimitActive { get; set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinLimitArgs
        {
            get { return _minLimitArgs; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public void DisplayArgLengthError(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <days> ^7 - days must be >0",
                CommandProcessor.BotCommandPrefix, c.CmdName, LimitCmd.AccountDateLimitArg));
        }

        /// <summary>
        ///     Evaluates the account date limit command.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task EvalLimitCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minLimitArgs)
            {
                DisplayArgLengthError(c);
                return;
            }

            // Disable account date limiter
            if (c.Args[2].Equals("off"))
            {
                DisableAccountDateLimiter();
                return;
            }
            int days;
            bool isValidNum = ((int.TryParse(c.Args[2], out days) && days > 0));
            if ((!isValidNum))
            {
                DisplayArgLengthError(c);
                return;
            }
            IsLimitActive = true;
            MinimumDaysRequired = days;
            _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]^7 Account date limit ^2ON.^7 Players with accounts registered in the last^1 {0}^7 days may not play.",
                    days));

            await RunUserDateCheck(_ssb.ServerInfo.CurrentPlayers);
        }

        /// <summary>
        ///     Asynchrounously scrapes the Quakelive.com site to retrieve the player's registration date.
        /// </summary>
        /// <returns><c>true</c> if the player was found on QL site, otherwise <c>false</c>.</returns>
        /// <remarks>
        ///     Unfortunately, this is the best way to do this, since there is no exposed QL API for this.
        /// </remarks>
        public async Task<DateTime> GetUserRegistrationDateFromQl(string user)
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
                            HtmlNode pg = elem.SelectSingleNode("p");
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
        ///     Runs the user date check on all current players.
        /// </summary>
        /// <param name="players">The players.</param>
        public async Task RunUserDateCheck(Dictionary<string, PlayerInfo> players)
        {
            // ToList() call on foreach to copy contents to separate list
            // because .NET modifies collection during enumeration under the hood and otherwise causes
            // "Collection was modified; enumeration operation may not execute" error, see:
            // http://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute
            foreach (var player in players.ToList())
            {
                await RunUserDateCheck(player.Key);
            }
        }

        /// <summary>
        ///     Disables the account date limiter.
        /// </summary>
        private void DisableAccountDateLimiter()
        {
            IsLimitActive = false;
            _ssb.QlCommands.QlCmdSay(
                "^2[SUCCESS]^7 Account date limit ^1OFF^7. Players who registered on any date can play.");
        }

        /// <summary>
        ///     Gets the user's registration date.
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
            // User doesn't exist in our db, retrieve from QL.
            registeredDate = await GetUserRegistrationDateFromQl(user);
            _regDateDb.AddUserToDb(user, registeredDate);
            return registeredDate;
        }

        /// <summary>
        ///     Runs the user date check on a given player.
        /// </summary>
        /// <param name="user">The user.</param>
        private async Task RunUserDateCheck(string user)
        {
            DateTime date = await GetUserRegistrationDate(user);
            VerifyUserDate(user, date);
        }

        /// <summary>
        ///     Verifies the user's registration date and kicks the user if the requirement is not met.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="regDate">The user's registration date.</param>
        private void VerifyUserDate(string user, DateTime regDate)
        {
            DateTime now = DateTime.Now;
            if ((now - regDate).TotalDays < MinimumDaysRequired)
            {
                Debug.WriteLine(
                    "User {0} has created account within the last {1} days. Date created: {2}. Kicking...",
                    user, MinimumDaysRequired, regDate);
                _ssb.QlCommands.CustCmdKickban(user);
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[=> KICK]: ^1{0}^7 (QL account date:^1 {1}^7)'s account is too new and does not meet the limit of^2 {2}^7 days",
                        user, regDate.ToString("d"), MinimumDaysRequired));
            }
        }
    }
}