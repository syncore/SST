using System.Linq;
using System.Threading.Tasks;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Retrieve a player's elo information.
    /// </summary>
    public class EloCmd : IBotCommand
    {
        private readonly QlRanksEloRetriever _qlrEloRetriever;
        private readonly QlRanksHelper _qlrHelper;
        private readonly SynServerBot _ssb;
        private bool _isIrcAccessAllowed = true;
        private int _qlMinArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EloCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public EloCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _qlrEloRetriever = new QlRanksEloRetriever();
            _qlrHelper = new QlRanksHelper();
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Helpers.GetArgVal(c, 1) if specified: user to check
        /// </remarks>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (c.Args.Length > (c.FromIrc ? 3 : 2))
            {
                // List of names to check with spaces after commas would be treated as additional args
                await DisplayArgLengthError(c);
                return false;
            }
            string user = c.Args.Length == (c.FromIrc ? 2 : 1) ? c.FromUser : Helpers.GetArgVal(c, 1);
            if (!Helpers.IsValidQlUsernameFormat(user, true))
            {
                StatusMessage = string.Format("^1[ERROR] {0}^7 contains invalid characters (only a-z,0-9,- allowed)",
                            Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            // We will need to retrieve the Elo from the API
            if (!Helpers.KeyExists(user, _ssb.ServerInfo.CurrentPlayers))
            {
                await GetAndSayElo(c, user);
                return true;
            }
            // Use existing Elo data if it exists and is not invalid
            if ((Helpers.KeyExists(user, _ssb.ServerInfo.CurrentPlayers)
                      &&
                      (!_qlrHelper.PlayerHasInvalidEloData(_ssb.ServerInfo.CurrentPlayers[user]))))
            {
                var player = _ssb.ServerInfo.CurrentPlayers[user];
                StatusMessage = string.Format(
                            "^4[ELO]^7 {0}'s Elo:^4 |CA|^7 {1} ^4|CTF|^7 {2} ^4|DUEL|^7 {3} ^4|FFA|^7 {4} ^4|TDM|^7 {5}",
                            user, player.EloData.CaElo, player.EloData.CtfElo,
                            player.EloData.DuelElo, player.EloData.FfaElo, player.EloData.TdmElo);
                await SendServerSay(c, StatusMessage);
                return true;
            }
            await GetAndSayElo(c, user);
            return true;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} [name1,name2,name3] ^7- Comma sep list of" +
                " names with ^1NO^7 spaces checks up to 3 names",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        /// Retrieve the user's QLRanks Elo from the QLRanks API and announce it.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="user">The user.</param>
        private async Task GetAndSayElo(CmdArgs c, string user)
        {
            // In the case of retrieving multiple users' elo data, we're only interested in the first 3 users given.
            var multiPlayers = new string[] { };
            bool hasMultiple = false;
            if (user.Contains(","))
            {
                hasMultiple = true;
                multiPlayers = user.Split(',');
            }
            var playersToRetrive = multiPlayers.TakeWhile((t, i) => i != 3).ToList();
            var qlrdata =
                await
                    _qlrEloRetriever.DoQlRanksRetrievalAsync((hasMultiple
                        ? string.Join(",", playersToRetrive)
                        : user));
            if (qlrdata == null)
            {
                string retr = hasMultiple ? string.Join(",", playersToRetrive) : user;
                await ShowEloRetrievalError(c, retr);
                return;
            }
            for (int i = 0; i < qlrdata.players.Count; i++)
            {
                if (i == 3) break;

                StatusMessage = string.Format(
                    "^4[ELO]^7 {0}'s Elo:^4 |CA|^7 {1} ^4|CTF|^7 {2} ^4|DUEL|^7 {3} ^4|FFA|^7 {4} ^4|TDM|^7 {5}",
                    (user.Contains(",") ? multiPlayers[i] : user), qlrdata.players[i].ca.elo,
                    qlrdata.players[i].ctf.elo, qlrdata.players[i].duel.elo,
                    qlrdata.players[i].ffa.elo, qlrdata.players[i].tdm.elo);
                await SendServerSay(c, StatusMessage);
            }
        }

        /// <summary>
        /// Shows the elo retrieval error.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="failedUsers">The failed users.</param>
        private async Task ShowEloRetrievalError(CmdArgs c, string failedUsers)
        {
            StatusMessage = string.Format("^1[ERROR]^3 Unable to retrieve Elo data for ^1{0}",
                    failedUsers);
            await SendServerTell(c, StatusMessage);
        }
    }
}