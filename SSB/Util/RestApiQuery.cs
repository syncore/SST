using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SSB.Util
{
    /// <summary>
    ///     This class is responsible for querying Quake Live and QLRanks REST APIs for various information.
    /// </summary>
    public class RestApiQuery
    {
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1003.1 Safari/535.19 Awesomium/1.7.1";

        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;

        /// <summary>
        ///     Asynchronously makes a call to a given REST API.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The API's URL.</param>
        /// <remarks>The API of interest is either the QuakeLive API or the QLRanks API.</remarks>
        /// <returns></returns>
        public async Task<T> QueryRestApiAsync<T>(string url)
        {
            _httpClientHandler = new HttpClientHandler();
            using (_httpClient = new HttpClient(_httpClientHandler))
            {
                try
                {
                    if (_httpClientHandler.SupportsAutomaticDecompression)
                    {
                        _httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip |
                                                                    DecompressionMethods.Deflate;
                    }

                    // QLRanks
                    if (!IsQuakeLiveUrl(url))
                    {
                        _httpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                    }

                    _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            string json = sr.ReadToEnd();
                            // QL site actually doesn't send "application/json", but "text/html"
                            // even though it is actually JSON HtmlDecode replaces &gt;, &lt; same
                            // as quakelive.js's EscapeHTML function
                            json = IsQuakeLiveUrl(url) ? WebUtility.HtmlDecode(json) : json;

                            return JsonConvert.DeserializeObject<T>(json);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return default(T);
                }
            }
        }

        /// <summary>
        ///     Determines whether the given URL is a quakelive.com URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if the URL is a quakelive.com URL, otherwise <c>false</c>.</returns>
        private bool IsQuakeLiveUrl(string url)
        {
            return (url.Contains("quakelive.com"));
        }
    }
}