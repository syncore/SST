using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SST.Model;
using SST.Util;

namespace SST.Core
{
    /// <summary>
    ///     Class responsible for checking for the latest version of SST.
    /// </summary>
    public class VersionChecker
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CORE]";
        private readonly string _updateUrl = "http://sst.syncore.org/download/sstversion.json";

        /// <summary>
        ///     Checks for updates.
        /// </summary>
        /// <param name="isAutoCheck">
        ///     if set to <c>true</c> then the update check
        ///     was launched automatically without user intervention (program start).
        /// </param>
        /// <returns></returns>
        public async Task CheckForUpdates(bool isAutoCheck)
        {
            var versionInfo = await GetLatestVersionInfoFromServer();
            if (versionInfo != null)
            {
                if (IsNewerVersionAvailable(versionInfo))
                {
                    ShowUpdateMessage(versionInfo);
                }
                else
                {
                    if (!isAutoCheck)
                    {
                        MessageBox.Show(@"No SST updates are available at this time.",
                            @"No updates available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the latest version information from server.
        /// </summary>
        /// <returns>
        ///     The version informat as a <see cref="SstVersion" /> object.
        /// </returns>
        private async Task<SstVersion> GetLatestVersionInfoFromServer()
        {
            Log.Write("Checking for latest version.", _logClassType, _logPrefix);
            var httpClientHandler = new HttpClientHandler();
            HttpClient httpClient;
            using (httpClient = new HttpClient(httpClientHandler))
            {
                try
                {
                    if (httpClientHandler.SupportsAutomaticDecompression)
                    {
                        httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip
                                                                   | DecompressionMethods.Deflate;
                    }

                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await httpClient.GetAsync(_updateUrl);
                    response.EnsureSuccessStatusCode();

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            var json = sr.ReadToEnd();
                            return JsonConvert.DeserializeObject<SstVersion>(json);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format("Problem when querying SST version info from server: {0}",
                        ex.Message), _logClassType, _logPrefix);
                    return null;
                }
            }
        }

        /// <summary>
        ///     Determines whether a newer version of SST is available.
        /// </summary>
        /// <param name="versionInfo">The version information retrieved from the server.</param>
        /// <returns><c>true</c> if a newer version of SST is available, otherwise <c>false</c>.</returns>
        private bool IsNewerVersionAvailable(SstVersion versionInfo)
        {
            if (versionInfo.latestVersion <= 0.0) return false;

            var ver = Helpers.GetVersion();
            double ourVersion;
            double.TryParse(ver, out ourVersion);

            if (versionInfo.latestVersion > ourVersion)
            {
                Log.Write(string.Format(
                    "Newer version of SST exists (new version: {0}, released on {1}; user's version: {2})",
                    versionInfo.latestVersion, versionInfo.releaseDateShort, ver), _logClassType, _logPrefix);
            }
            else
            {
                Log.Write("SST is the latest version. No updates are available.",
                    _logClassType, _logPrefix);
            }

            return (versionInfo.latestVersion > ourVersion);
        }

        /// <summary>
        ///     Shows the software update message.
        /// </summary>
        /// <param name="versionInfo">The version information.</param>
        private void ShowUpdateMessage(SstVersion versionInfo)
        {
            var result = MessageBox.Show(
                string.Format(
                    "A newer version of SST is available (new version: {0}, released on {1}. your " +
                    "version: {2}) Would you like to visit the SST download page?",
                    versionInfo.latestVersion, versionInfo.releaseDate, Helpers.GetVersion()),
                @"Update is available", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;
            Helpers.LaunchUrlInBrowser("http://sst.syncore.org/download");
        }
    }
}