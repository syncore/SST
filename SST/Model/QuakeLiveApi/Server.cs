using System.Collections.Generic;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming

namespace SST.Model.QuakeLiveApi
{
    /// <summary>
    ///     Model representing a Quake Live server. This is the format of an individual server returned
    ///     from http://www.quakelive.com/browser/details?ids={server_id(s)} - it's different from that
    ///     returned by /list?filter={base64_encoded_json_filter}
    ///     Note: browser/details?ids={server_id(s)} actually includes a list of players for a given
    ///     server, unlike /list?filter={base64_encoded_json_filter}
    /// </summary>
    public class Server
    {
        // port regexp: colon with at least 4 numbers
        private readonly Regex _port = new Regex(@"[\:]\d{4,}");

        private string _hostaddress;

        /// <summary>
        ///     Gets or sets the blue team's elo.
        /// </summary>
        /// <value>
        ///     The blue team's elo.
        /// </value>
        /// <remarks>This is a custom property.</remarks>
        public long blueteamelo { get; set; }

        /// <summary>
        ///     Gets or sets the capturelimit.
        /// </summary>
        /// <value>The capturelimit.</value>
        public int capturelimit { get; set; }

        /// <summary>
        ///     Gets the server's IP address with the port number removed.
        /// </summary>
        /// <value>
        ///     The clean IP address.
        /// </value>
        /// <remarks>This is a custom property.</remarks>
        public string cleanedip { get; set; }

        /// <summary>
        ///     Gets or sets the ecode.
        /// </summary>
        /// <value>The ecode.</value>
        public int ECODE { get; set; }

        /// <summary>
        ///     Gets or sets the fraglimit.
        /// </summary>
        /// <value>The fraglimit.</value>
        public int fraglimit { get; set; }

        /// <summary>
        ///     Gets or sets the g_bluescore.
        /// </summary>
        /// <value>The g_bluescore.</value>
        public int g_bluescore { get; set; }

        /// <summary>
        ///     Gets or sets the g_custom settings.
        /// </summary>
        /// <value>The g_custom settings.</value>
        public string g_customSettings { get; set; }

        /// <summary>
        ///     Gets or sets the g_gamestate.
        /// </summary>
        /// <value>The g_gamestate.</value>
        public string g_gamestate { get; set; }

        /// <summary>
        ///     Gets or sets the g_instagib.
        /// </summary>
        /// <value>The g_instagib.</value>
        public int g_instagib { get; set; }

        /// <summary>
        ///     Gets or sets the g_levelstarttime.
        /// </summary>
        /// <value>The g_levelstarttime.</value>
        public long g_levelstarttime { get; set; }

        /// <summary>
        ///     Gets or sets the g_needpass.
        /// </summary>
        /// <value>The g_needpass.</value>
        public int g_needpass { get; set; }

        /// <summary>
        ///     Gets or sets the g_redscore.
        /// </summary>
        /// <value>The g_redscore.</value>
        public int g_redscore { get; set; }

        /// <summary>
        ///     Gets or sets the game_type.
        /// </summary>
        /// <value>The game_type.</value>
        public int game_type { get; set; }

        /// <summary>
        ///     Gets or sets the game_type_title.
        /// </summary>
        /// <value>The game_type_title.</value>
        public string game_type_title { get; set; }

        /// <summary>
        ///     Gets or sets the host_address.
        /// </summary>
        /// <value>The host_address.</value>
        public string host_address
        {
            get
            {
                return _hostaddress;
            }
            set
            {
                _hostaddress = value;
                cleanedip = _port.Replace(value, string.Empty);
            }
        }

        /// <summary>
        ///     Gets or sets the host_name.
        /// </summary>
        /// <value>The host_name.</value>
        public string host_name { get; set; }

        /// <summary>
        ///     Gets or sets the location_id.
        /// </summary>
        /// <value>The location_id.</value>
        public long location_id { get; set; }

        /// <summary>
        ///     Gets or sets the map.
        /// </summary>
        /// <value>The map.</value>
        public string map { get; set; }

        /// <summary>
        ///     Gets or sets the map_title.
        /// </summary>
        /// <value>The map_title.</value>
        public string map_title { get; set; }

        /// <summary>
        ///     Gets or sets the max_clients.
        /// </summary>
        /// <value>The max_clients.</value>
        public int max_clients { get; set; }

        /// <summary>
        ///     Gets or sets the num_clients.
        /// </summary>
        /// <value>The num_clients.</value>
        public int num_clients { get; set; }

        /// <summary>
        ///     Gets or sets the num_players.
        /// </summary>
        /// <value>The num_players.</value>
        public int num_players { get; set; }

        /// <summary>
        ///     Gets or sets the owner.
        /// </summary>
        /// <value>The owner.</value>
        public string owner { get; set; }

        /// <summary>
        ///     Gets or sets the players.
        /// </summary>
        /// <value>The players.</value>
        public List<Player> players { get; set; }

        /// <summary>
        ///     Gets or sets the premium.
        /// </summary>
        /// <value>The premium.</value>
        public object premium { get; set; }

        /// <summary>
        ///     Gets or sets the public_id.
        /// </summary>
        /// <value>The public_id.</value>
        public int public_id { get; set; }

        /// <summary>
        ///     Gets or sets the ranked.
        /// </summary>
        /// <value>The ranked.</value>
        public int ranked { get; set; }

        /// <summary>
        ///     Gets or sets the red team's elo.
        /// </summary>
        /// <value>
        ///     The red team's elo.
        /// </value>
        /// <remarks>This is a custom property.</remarks>
        public long redteamelo { get; set; }

        /// <summary>
        ///     Gets or sets the roundlimit.
        /// </summary>
        /// <value>The roundlimit.</value>
        public int roundlimit { get; set; }

        /// <summary>
        ///     Gets or sets the roundtimelimit.
        /// </summary>
        /// <value>The roundtimelimit.</value>
        public int roundtimelimit { get; set; }

        /// <summary>
        ///     Gets or sets the ruleset.
        /// </summary>
        /// <value>The ruleset.</value>
        public string ruleset { get; set; }

        /// <summary>
        ///     Gets or sets the scorelimit.
        /// </summary>
        /// <value>The scorelimit.</value>
        public string scorelimit { get; set; }

        /// <summary>
        ///     Gets or sets the skill delta.
        /// </summary>
        /// <value>The skill delta.</value>
        public int skillDelta { get; set; }

        /// <summary>
        ///     Gets or sets the teamsize.
        /// </summary>
        /// <value>The teamsize.</value>
        public int teamsize { get; set; }

        /// <summary>
        ///     Gets or sets the timelimit.
        /// </summary>
        /// <value>The timelimit.</value>
        public int timelimit { get; set; }
    }
}