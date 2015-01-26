using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Model.QlRanks;
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
        private int _minArgs = 0;
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
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

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
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} [name1,name2,name3] ^7- Comma sep list of names with ^1NO^7 spaces checks up to 3 names",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <remarks>
        ///     c.Args[1] if specified: user to check
        /// </remarks>
        public async Task ExecAsync(CmdArgs c)
        {
            if (c.Args.Length > 2)
            {
                // List of names to check with spaces after commas would be treated as additional args
                await DisplayArgLengthError(c);
                return;
            }
            string user = c.Args.Length == 1 ? c.FromUser : c.Args[1];
            if (!Tools.IsValidQlUsernameFormat(user, true))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^1[ERROR] {0}^7 contains invalid characters (only a-z,0-9,- allowed)",
                            c.Args[1]));
                return;
            }
            if (!Tools.KeyExists(user, _ssb.ServerInfo.CurrentPlayers))
            {
                await GetAndSayElo(user);
            }
                // Use existing elo data if it exists and is not invalid
            else if ((Tools.KeyExists(user, _ssb.ServerInfo.CurrentPlayers)
                      &&
                      (!_qlrHelper.PlayerHasInvalidEloData(_ssb.ServerInfo.CurrentPlayers[user]))))
            {
                PlayerInfo player = _ssb.ServerInfo.CurrentPlayers[user];
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^4[ELO]^7 {0}'s Elo:^4 |CA|^7 {1} ^4|CTF|^7 {2} ^4|DUEL|^7 {3} ^4|FFA|^7 {4} ^4|TDM|^7 {5}",
                            user, player.EloData.CaElo, player.EloData.CtfElo,
                            player.EloData.DuelElo, player.EloData.FfaElo, player.EloData.TdmElo));
            }
            else
            {
                await GetAndSayElo(user);
            }
        }

        /// <summary>
        ///     Retrieve the user's QLRanks Elo from the QLRanks API and announce it.
        /// </summary>
        /// <param name="user">The user.</param>
        private async Task GetAndSayElo(string user)
        {
            // In the case of retrieving multiple users' elo data, we're only interested in the first 3 users given.
            var multiPlayers = new string[] {};
            bool hasMultiple = false;
            if (user.Contains(","))
            {
                hasMultiple = true;
                multiPlayers = user.Split(',');
            }
            List<string> playersToRetrive = multiPlayers.TakeWhile((t, i) => i != 3).ToList();
            QlRanks qlrdata =
                await
                    _qlrEloRetriever.DoQlRanksRetrievalAsync((hasMultiple
                        ? string.Join(",", playersToRetrive)
                        : user));
            if (qlrdata == null)
            {
                string retr = hasMultiple ? string.Join(",", playersToRetrive) : user;
                await ShowEloRetrievalError(retr);
                return;
            }
            for (int i = 0; i < qlrdata.players.Count; i++)
            {
                if (i == 3) break;
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^4[ELO]^7 {0}'s Elo:^4 |CA|^7 {1} ^4|CTF|^7 {2} ^4|DUEL|^7 {3} ^4|FFA|^7 {4} ^4|TDM|^7 {5}",
                            (user.Contains(",") ? multiPlayers[i] : user), qlrdata.players[i].ca.elo,
                            qlrdata.players[i].ctf.elo, qlrdata.players[i].duel.elo,
                            qlrdata.players[i].ffa.elo, qlrdata.players[i].tdm.elo));
            }
        }

        /// <summary>
        ///     Shows the elo retrieval error.
        /// </summary>
        /// <param name="failedUsers">The failed users.</param>
        private async Task ShowEloRetrievalError(string failedUsers)
        {
            await
                _ssb.QlCommands.QlCmdSay(string.Format("[ERROR] Unable to retrieve Elo data for {0}",
                    failedUsers));
        }
    }
}