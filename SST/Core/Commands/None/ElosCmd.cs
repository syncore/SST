namespace SST.Core.Commands.None
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using SST.Enums;
    using SST.Interfaces;
    using SST.Model;
    using SST.Util;

    /// <summary>
    /// Command: Displays the server's QLRanks Elo information for all team or duel gametypes.
    /// </summary>
    public class ElosCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:ELOS]";
        private readonly int _qlMinArgs = 0;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElosCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public ElosCmd(SynServerTool sst)
        {
            _sst = sst;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs
        {
            get { return _qlMinArgs + 1; }
        }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        public async Task<bool> ExecAsync(Cmd c)
        {
            if (!_sst.ServerInfo.IsQlRanksGameType())
            {
                StatusMessage = "^1[ERROR]^3 Server is not running QLRanks-supported gametype!";
                await SendServerTell(c, StatusMessage);
                return false;
            }

            var retrResult = await GetMissingEloVals();
            if (!retrResult)
            {
                StatusMessage = "^1[ERROR]^7 Unable to retrieve QLRanks data.";
                await SendServerSay(c, StatusMessage);
                return false;
            }

            if (_sst.ServerInfo.IsATeamGame())
            {
                await ShowEloTeams(c);
            }
            if (_sst.ServerInfo.CurrentServerGameType == QlGameTypes.Duel)
            {
                await ShowEloDuel(c);
            }
            return true;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return string.Empty;
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
            {
                await _sst.QlCommands.QlCmdSay(message, false);
            }
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
            {
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
            }
        }

        /// <summary>
        /// Gets the missing player Elo values if applicable.
        /// </summary>
        /// <returns><c>true</c> if the ELo values could be retried, otherwise <c>false</c>.</returns>
        private async Task<bool> GetMissingEloVals()
        {
            var qlrHelper = new QlRanksHelper();
            var playersToUpdate = (from player in _sst.ServerInfo.CurrentPlayers
                where qlrHelper.PlayerHasInvalidEloData(player.Value)
                select player.Key).ToList();
            if (playersToUpdate.Any())
            {
                var qlr =
                    await
                        qlrHelper.RetrieveEloDataFromApiAsync(_sst.ServerInfo.CurrentPlayers, playersToUpdate);
                if (qlr == null)
                {
                    Log.Write("QLRanks Elo data could not be retrieved when listing server Elos.",
                        _logClassType, _logPrefix);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Shows the server's Elo data for team-based game types.
        /// </summary>
        /// <param name="c">The command information.</param>
        private async Task ShowEloTeams(Cmd c)
        {
            var bl = new StringBuilder();
            var rd = new StringBuilder();
            var sp = new StringBuilder();
            var elos = new StringBuilder();
            foreach (var player in _sst.ServerInfo.CurrentPlayers)
            {
                if (player.Value.Team == Team.Blue)
                {
                    bl.Append(string.Format("{0}({1}), ", player.Value.ShortName,
                        player.Value.EloData.GetEloFromGameType(_sst.ServerInfo.CurrentServerGameType)));
                }
                if (player.Value.Team == Team.Red)
                {
                    rd.Append(string.Format("{0}({1}), ", player.Value.ShortName,
                        player.Value.EloData.GetEloFromGameType(_sst.ServerInfo.CurrentServerGameType)));
                }
                if (player.Value.Team == Team.Spec || player.Value.Team == Team.None)
                {
                    sp.Append(string.Format("{0}({1}), ", player.Value.ShortName,
                        player.Value.EloData.GetEloFromGameType(_sst.ServerInfo.CurrentServerGameType)));
                }
            }

            if (bl.Length != 0)
            {
                elos.Append(string.Format("^5{0}", bl.ToString().TrimEnd(',', ' ')));
            }
            if (rd.Length != 0)
            {
                elos.Append(string.Format("^3 * ^1{0}", rd.ToString().TrimEnd(',', ' ')));
            }
            if (sp.Length != 0)
            {
                elos.Append(string.Format("^3 * {0}", sp.ToString().TrimEnd(',', ' ')));
            }

            StatusMessage = string.Format("^7{0}: {1} ^3* ^5Avg {2}, ^1Avg {3} ^3* ^to 7balance use: ^2{4}{5}",
                _sst.ServerInfo.CurrentServerGameType.ToString().ToUpper(), elos, GetTeamAvgElo(Team.Blue),
                GetTeamAvgElo(Team.Red), CommandList.GameCommandPrefix, CommandList.CmdSuggestTeams);
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        /// Gets the average Elo for the specified team.
        /// </summary>
        /// <param name="team">The team.</param>
        /// <returns>The team's average Elo.</returns>
        private long GetTeamAvgElo(Team team)
        {
            var totalTeamElo = _sst.ServerInfo.GetTeam(team).Sum(
                player => player.EloData.GetEloFromGameType(_sst.ServerInfo.CurrentServerGameType));
            var count = _sst.ServerInfo.GetTeam(team).Count;

            return (count == 0) ? 0 : (totalTeamElo / count);
        }

        /// <summary>
        /// Shows the server's Elo data for the duel gametype.
        /// </summary>
        /// <param name="c">The command information.</param>
        private async Task ShowEloDuel(Cmd c)
        {
            var sb = new StringBuilder();
            long eloTotal = 0;
            foreach (var player in _sst.ServerInfo.CurrentPlayers)
            {
                var elo = player.Value.EloData.GetEloFromGameType(_sst.ServerInfo.CurrentServerGameType);
                sb.Append(string.Format("{0}({1}), ", player.Value.ShortName, elo));
                eloTotal += elo;
            }

            StatusMessage = string.Format("^7{0} Elo Avg:^5 {1}^7 *^3 {2}",
                _sst.ServerInfo.CurrentServerGameType.ToString().ToUpper(),
                (_sst.ServerInfo.CurrentPlayers.Count == 0 ? 0 : (eloTotal / _sst.ServerInfo.CurrentPlayers.Count)),
                sb.ToString().TrimEnd(',', ' '));

            await SendServerSay(c, StatusMessage);
        }
    }
}
