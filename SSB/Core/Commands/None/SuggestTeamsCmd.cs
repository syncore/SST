using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: Suggest balanced teams based on player Elo data.
    /// </summary>
    public class SuggestTeamsCmd : IBotCommand
    {
        private readonly QlRanksHelper _qlrHelper;
        private readonly SynServerBot _ssb;
        private readonly TeamBalancer _teamBalancer;
        private List<PlayerInfo> _balancedBlueTeam;
        private List<PlayerInfo> _balancedRedTeam;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SuggestTeamsCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public SuggestTeamsCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _qlrHelper = new QlRanksHelper();
            _teamBalancer = new TeamBalancer();
            _balancedRedTeam = new List<PlayerInfo>();
            _balancedBlueTeam = new List<PlayerInfo>();
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
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
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
            if (_ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Ca &&
                _ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Ctf &&
                _ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Tdm)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Team balancing is not available on this server!");
                return;
            }

            var blueTeam = _ssb.ServerInfo.GetTeam(Team.Blue);
            var redTeam = _ssb.ServerInfo.GetTeam(Team.Red);
            int redAndBlueTotalPlayers = (blueTeam.Count + redTeam.Count);

            if ((redAndBlueTotalPlayers) % 2 != 0)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Teams can only be suggested if there are a total even number of red and blue players!");
                return;
            }
            if ((redAndBlueTotalPlayers) < 4)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 There must be at least 4 total players for the team suggestion!");
                return;
            }
            try
            {
                // Verify all player elo data prior to making any suggestions.
                await VerifyElo(_ssb.ServerInfo.CurrentPlayers);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not verify all players' Elo data. Will skip team suggestion." + ex.Message);
            }

            var allPlayers = redTeam;
            allPlayers.AddRange(blueTeam);
            _balancedRedTeam = _teamBalancer.DoBalance(allPlayers, _ssb.ServerInfo.CurrentServerGameType, Team.Red);
            _balancedBlueTeam = _teamBalancer.DoBalance(allPlayers, _ssb.ServerInfo.CurrentServerGameType, Team.Blue);
            await DisplayBalanceResults(_balancedRedTeam, _balancedBlueTeam, _ssb.ServerInfo.CurrentServerGameType);
        }

        /// <summary>
        /// Gets and displays the results.
        /// </summary>
        /// <param name="teamRed">The red team.</param>
        /// <param name="teamBlue">The blue team.</param>
        /// <param name="gametype">The gametype.</param>
        private async Task DisplayBalanceResults(IList<PlayerInfo> teamRed, IList<PlayerInfo> teamBlue, QlGameTypes gametype)
        {
            long redTeamElo = teamRed.Sum(player => player.EloData.GetEloFromGameType(gametype));
            //long redTeamAvgElo = (redTeamElo / teamRed.Count);
            long blueTeamElo = teamBlue.Sum(player => player.EloData.GetEloFromGameType(gametype));
            //long blueTeamAvgElo = (blueTeamElo / teamBlue.Count);
            var red = new StringBuilder();
            var blue = new StringBuilder();

            foreach (var player in teamRed)
            {
                red.Append(string.Format("^1{0} [{1}]^7, ", player.ShortName,
                    player.EloData.GetEloFromGameType(gametype)));
            }
            foreach (var player in teamBlue)
            {
                blue.Append(string.Format("^5{0} [{1}]^7, ", player.ShortName,
                    player.EloData.GetEloFromGameType(gametype)));
            }

            await _ssb.QlCommands.QlCmdSay("^2[TEAMBALANCE]^7 Suggested ^2balanced^7 teams are:");
            await
                _ssb.QlCommands.QlCmdSay(string.Format(
                    "^1[RED]: {0}", red.ToString().TrimEnd(',', ' ')));

            await
                _ssb.QlCommands.QlCmdSay(string.Format(
                    "^5[BLUE]: {0}", blue.ToString().TrimEnd(',', ' ')));
            //await
            //    _ssb.QlCommands.QlCmdSay(string.Format(
            //        "^1[RED]: {0} | Total Elo: ^2{1}^7 | Avg Elo Per Player: ^2{2}", red.ToString().TrimEnd(',', ' '),
            //        redTeamElo, redTeamAvgElo));

            //await
            //    _ssb.QlCommands.QlCmdSay(string.Format(
            //        "^5[BLUE]: {0} | Total Elo: ^2{1}^7 | Avg Elo Per Player: ^2{2}", blue.ToString().TrimEnd(',', ' '),
            //        blueTeamElo, blueTeamAvgElo));
        }

        /// <summary>
        /// Verify all player elo data.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unable to verify player Elo data</exception>
        private async Task VerifyElo(Dictionary<string, PlayerInfo> players)
        {
            var update = (from player in players where _qlrHelper.PlayerHasInvalidEloData(player.Value) select player.Key).ToList();
            if (update.Any())
            {
                var qlrData = await _qlrHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, update);
                if (qlrData == null)
                {
                    await _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Unable to verify player data. Try again in a few seconds.");
                    throw new Exception("Unable to verify player Elo data");
                }
            }
        }
    }
}