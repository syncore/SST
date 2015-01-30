using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;
using Timer = System.Timers.Timer;

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
        private readonly DbUsers _users;
        private List<PlayerInfo> _balancedBlueTeam;
        private List<PlayerInfo> _balancedRedTeam;
        private int _minArgs = 0;
        private readonly Timer _suggestionTimer;
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
            _users = new DbUsers();
            _balancedRedTeam = new List<PlayerInfo>();
            _balancedBlueTeam = new List<PlayerInfo>();
            _suggestionTimer = new Timer();
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
            // Must be a team gametype that is supported by QLRanks
            if (_ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Ca &&
                _ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Ctf &&
                _ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Tdm)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Team balancing is not available on this server!");
                return;
            }

            // Disable this command if the pickup module is active
            if (_ssb.Mod.Pickup.Active)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Team balancing is unavailable when pickup module is active!");
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

            if (_ssb.VoteManager.IsTeamSuggestionVotePending)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 A team balance vote is already pending!");
                return;
            }

            // SuperUser or higher... Force the vote
            if ((c.Args.Length == 2) && (c.Args[1].Equals("force", StringComparison.InvariantCultureIgnoreCase)))
            {
                if (_users.GetUserLevel(c.FromUser) < UserLevel.SuperUser)
                {
                    await
                        _ssb.QlCommands.QlCmdSay("^1[ERROR]^7 You do not have permission to use that command.");
                    return;
                }
                await InitiateBalance(redTeam, blueTeam, true);
                return;
            }

            await InitiateBalance(redTeam, blueTeam, false);
        }

        /// <summary>
        /// Initiates the balance process.
        /// </summary>
        /// <param name="redTeam">The red team.</param>
        /// <param name="blueTeam">The blue team.</param>
        /// <param name="isForcedBalance">if set to <c>true</c> then a user with required permissions is forcing balance; vote will be bypassed.</param>
        private async Task InitiateBalance(List<PlayerInfo> redTeam, List<PlayerInfo> blueTeam, bool isForcedBalance)
        {
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
            if (!isForcedBalance)
            {
                await StartTeamSuggestionVote();
            }
            else
            {
                _ssb.VoteManager.IsTeamSuggestionVotePending = true;
                await _ssb.QlCommands.QlCmdSay(string.Format("^2[TEAMBALANCE]^7 Forcing team balance."));
                await MovePlayersToBalancedTeams();
                _ssb.VoteManager.IsTeamSuggestionVotePending = false;
            }
        }

        /// <summary>
        /// Starts the team suggestion vote and associated timer.
        /// </summary>
        private async Task StartTeamSuggestionVote()
        {
            double interval = 20000;

            await _ssb.QlCommands.QlCmdSay(string.Format("^2[TEAMBALANCE]^7 You have {0} seconds to vote. Type ^2{1}{2}^7 to accept the team suggestion, ^1{1}{3}^7 to reject the suggestion.",
                (interval / 1000), CommandProcessor.BotCommandPrefix, CommandProcessor.CmdAcceptTeamSuggestion, CommandProcessor.CmdRejectTeamSuggestion));
            _suggestionTimer.Interval = interval;
            _suggestionTimer.Elapsed += TeamSuggestionTimerElapsed;
            _suggestionTimer.AutoReset = false;
            _suggestionTimer.Start();
            _ssb.VoteManager.IsTeamSuggestionVotePending = true;
        }

        /// <summary>
        /// Method called when the teams suggestion vote timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private async void TeamSuggestionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Do balance if enough votes
            try
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^2[TEAMBALANCE]^7 Vote results -- YES: ^2{0}^7 -- NO: ^1{1}^7 -- {2}",
                            _ssb.VoteManager.TeamSuggestionYesVoteCount,
                            _ssb.VoteManager.TeamSuggestionNoVoteCount,
                            ((_ssb.VoteManager.TeamSuggestionYesVoteCount >
                              _ssb.VoteManager.TeamSuggestionNoVoteCount)
                                ? ("Teams will be balanced.")
                                : "Teams will remain unchanged.")));

                if (_ssb.VoteManager.TeamSuggestionYesVoteCount > _ssb.VoteManager.TeamSuggestionNoVoteCount)
                {
                    await MovePlayersToBalancedTeams();
                }

                // Reset votes
                _ssb.VoteManager.ResetTeamSuggestionVote();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Caught exception in TeamSuggestionTimerElapsed asynchronous void (event handler) method: " + ex.Message);
            }
        }

        /// <summary>
        /// Moves the players to the suggested teams.
        /// </summary>
        private async Task MovePlayersToBalancedTeams()
        {
            await _ssb.QlCommands.QlCmdSay("^2[TEAMBALANCE]^7 Balancing teams, please wait....");
            foreach (var player in _balancedBlueTeam)
            {
                await _ssb.QlCommands.CustCmdPutPlayer(player.ShortName, Team.Blue);
            }
            foreach (var player in _balancedRedTeam)
            {
                await _ssb.QlCommands.CustCmdPutPlayer(player.ShortName, Team.Red);
            }
        }

        /// <summary>
        /// Gets and displays the results.
        /// </summary>
        /// <param name="teamRed">The red team.</param>
        /// <param name="teamBlue">The blue team.</param>
        /// <param name="gametype">The gametype.</param>
        private async Task DisplayBalanceResults(IList<PlayerInfo> teamRed, IList<PlayerInfo> teamBlue, QlGameTypes gametype)
        {
            //long redTeamElo = teamRed.Sum(player => player.EloData.GetEloFromGameType(gametype));
            //long redTeamAvgElo = (redTeamElo / teamRed.Count);
            //long blueTeamElo = teamBlue.Sum(player => player.EloData.GetEloFromGameType(gametype));
            //long blueTeamAvgElo = (blueTeamElo / teamBlue.Count);
            var red = new StringBuilder();
            var blue = new StringBuilder();

            foreach (var player in teamRed)
            {
                //red.Append(string.Format("^1{0} [{1}]^7, ", player.ShortName,
                    //player.EloData.GetEloFromGameType(gametype)));
                red.Append(string.Format("^1{0}^7, ", player.ShortName));
            }
            foreach (var player in teamBlue)
            {
                //blue.Append(string.Format("^5{0} [{1}]^7, ", player.ShortName,
                    //player.EloData.GetEloFromGameType(gametype)));
                blue.Append(string.Format("^5{0}^7, ", player.ShortName));
            }

            await _ssb.QlCommands.QlCmdSay("^2[TEAMBALANCE]^7 Suggested ^2balanced^7 teams are:");
            await
                _ssb.QlCommands.QlCmdSay(string.Format(
                    "^1[RED]: {0}", red.ToString().TrimEnd(',', ' ')));

            await
                _ssb.QlCommands.QlCmdSay(string.Format(
                    "^5[BLUE]: {0}", blue.ToString().TrimEnd(',', ' ')));
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