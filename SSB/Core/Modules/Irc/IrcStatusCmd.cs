using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Model.QuakeLiveApi;
using SSB.Util;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: display the current server's status.
    /// </summary>
    public class IrcStatusCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly bool _isAsync = true;
        private readonly SynServerBot _ssb;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;
        private int _ircMinArgs = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcStatusCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcStatusCmd(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
            _irc = irc;
        }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync
        {
            get { return _isAsync; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _ircMinArgs; } }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public IrcUserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this command takes no arguments.
        /// </remarks>
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this is an async command.
        /// </remarks>
        public void Exec(CmdArgs c)
        {
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            var serverDetails = await GetServerDetails(_ssb.ServerInfo.CurrentServerId);
            if (serverDetails == null)
            {
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    "\u0003\u0002[ERROR]\u0002 Unable to retrieve server information.");
                return;
            }
            var gametype = (QlGameTypes) serverDetails.game_type;

            _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                Helpers.IsQuakeLiveTeamGame(gametype)
                    ? FormatTeamGameStatusMessage(serverDetails)
                    : FormatNonTeamGameStatusMessage(serverDetails));
        }

        /// <summary>
        ///     Formats the status message for non-team games.
        /// </summary>
        /// <param name="serverDetails">The server details.</param>
        /// <returns>The formatted status message for non-team games as a string.</returns>
        private string FormatNonTeamGameStatusMessage(Server serverDetails)
        {
            var gamestate = serverDetails.g_gamestate.Replace("_", "");
            var players = new StringBuilder();
            var spec = new StringBuilder();
            foreach (var player in serverDetails.players.Where(player => player.team == 0))
            {
                players.Append(string.Format("{0} ({1}), ", player.name, player.score));
            }
            foreach (var player in serverDetails.players.Where(player => player.team == 3))
            {
                spec.Append(string.Format("{0} ({1}), ", player.name, player.score));
            }

            var scoreAndPlayers = string.Format("\u0003PLAYERS: {0} \u000314SPEC: {1}",
                players.ToString().TrimEnd(',', ' '), spec.ToString().TrimEnd(',', ' '));

            return
                string.Format(
                    "\u000310\u0002[STATUS]\u0002\u0003 {0} players, \u0002{1}\u0002 {2} on map \u0002{3}\u0002 @ {4} - {5}",
                    serverDetails.num_players, gamestate, serverDetails.game_type_title,
                    serverDetails.map_title, serverDetails.host_address, scoreAndPlayers);
        }

        /// <summary>
        ///     Formats the status message for team games.
        /// </summary>
        /// <param name="serverDetails">The server details.</param>
        /// <returns>The formatted status message for team games as a string.</returns>
        private string FormatTeamGameStatusMessage(Server serverDetails)
        {
            var gamestate = serverDetails.g_gamestate.Replace("_", "");
            var scoreAndPlayers = string.Empty;
            var red = new StringBuilder();
            var blue = new StringBuilder();
            var spec = new StringBuilder();
            foreach (var player in serverDetails.players.Where(player => player.team == 1))
            {
                red.Append(string.Format("{0} ({1}), ", player.name, player.score));
            }
            foreach (var player in serverDetails.players.Where(player => player.team == 2))
            {
                blue.Append(string.Format("{0} ({1}), ", player.name, player.score));
            }
            foreach (
                var player in serverDetails.players.Where(player => (player.team == 0 || player.team == 3)))
            {
                spec.Append(string.Format("{0}, ", player.name));
            }
            if (serverDetails.g_redscore > serverDetails.g_bluescore ||
                serverDetails.g_redscore == serverDetails.g_bluescore)
            {
                scoreAndPlayers =
                    string.Format("\u000304RED ({0}): {1}\u0003 \u000311BLUE ({2}): {3}\u0003 SPEC: {4}",
                        serverDetails.g_redscore, red.ToString().TrimEnd(',', ' '),
                        serverDetails.g_bluescore, blue.ToString().TrimEnd(',', ' '),
                        spec.ToString().TrimEnd(',', ' '));
            }
            else if (serverDetails.g_redscore < serverDetails.g_bluescore)
            {
                scoreAndPlayers =
                    string.Format("\u000311BLUE ({0}): {1}\u0003 \u000304RED ({2}): {3}\u0003 SPEC: {4}",
                        serverDetails.g_bluescore, blue.ToString().TrimEnd(',', ' '),
                        serverDetails.g_redscore, red.ToString().TrimEnd(',', ' '),
                        spec.ToString().TrimEnd(',', ' '));
            }

            return
                string.Format(
                    "\u000310\u0002[STATUS]\u0002\u0003 {0} players, \u0002{1}\u0002 {2} on map \u0002{3}\u0002 @ {4} - {5}",
                    serverDetails.num_players, gamestate, serverDetails.game_type_title,
                    serverDetails.map_title, serverDetails.host_address, scoreAndPlayers);
        }

        /// <summary>
        ///     Gets the server details from the QL API.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>The server as a <see cref="Server" /> object.</returns>
        private async Task<Server> GetServerDetails(string serverId)
        {
            if (string.IsNullOrEmpty(serverId)) return null;
            var qlRetr = new QlRemoteInfoRetriever();
            var srv = await qlRetr.QueryQlServer(serverId);
            return srv;
        }
    }
}