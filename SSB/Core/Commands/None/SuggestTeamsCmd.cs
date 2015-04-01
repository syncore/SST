using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SSB.Config;
using SSB.Database;
using SSB.Enums;
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
        private readonly Timer _suggestionTimer;
        private readonly TeamBalancer _teamBalancer;
        private readonly UserLevel _userLevel = UserLevel.None;
        private readonly DbUsers _users;
        private List<PlayerInfo> _balancedBlueTeam;
        private List<PlayerInfo> _balancedRedTeam;
        private bool _isIrcAccessAllowed = true;
        private int _qlMinArgs = 0;

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
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <remarks>
        ///     Helpers.GetArgVal(c, 1) if specified: user to check
        /// </remarks>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            // Must be a team gametype that is supported by QLRanks
            if (_ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Ca &&
                _ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Ctf &&
                _ssb.ServerInfo.CurrentServerGameType != QlGameTypes.Tdm)
            {
                StatusMessage = "^1[ERROR]^3 Team balancing is not available on this server!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);
                return false;
            }

            // Disable this command if the pickup module is active
            if (_ssb.Mod.Pickup.Active)
            {
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);
                StatusMessage = "^1[ERROR]^3 Team balancing is unavailable when pickup module is active!";
                return false;
            }

            var blueTeam = _ssb.ServerInfo.GetTeam(Team.Blue);
            var redTeam = _ssb.ServerInfo.GetTeam(Team.Red);
            var redAndBlueTotalPlayers = (blueTeam.Count + redTeam.Count);

            if ((redAndBlueTotalPlayers) % 2 != 0)
            {
                StatusMessage = "^1[ERROR]^3 Teams can only be suggested if there are a total even number of red and blue players!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);
                return false;
            }
            if ((redAndBlueTotalPlayers) < 4)
            {
                StatusMessage = "^1[ERROR]^3 There must be at least 4 total players for the team suggestion!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);
                return false;
            }

            if (_ssb.VoteManager.IsTeamSuggestionVotePending)
            {
                StatusMessage = "^1[ERROR]^3 A team balance vote is already pending!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);
                return false;
            }

            // SuperUser or higher... Force the vote
            if ((c.Args.Length == (c.FromIrc ? 3 : 2)) &&
                (Helpers.GetArgVal(c, 1).Equals("force", StringComparison.InvariantCultureIgnoreCase)))
            {
                var userLevel = IsIrcOwner(c) ? UserLevel.Owner : _users.GetUserLevel(c.FromUser);
                if (userLevel < UserLevel.SuperUser)
                {
                    StatusMessage = "^1[ERROR]^7 You do not have permission to use that command.";
                    return false;
                }
                await InitiateBalance(redTeam, blueTeam, true);
                return true;
            }

            await InitiateBalance(redTeam, blueTeam, false);
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
            return string.Empty;
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
        ///     Gets and displays the results.
        /// </summary>
        /// <param name="teamRed">The red team.</param>
        /// <param name="teamBlue">The blue team.</param>
        /// <remarks>
        /// Note: I purposefully did not touch the QlCmdSay stuff in this method, so if it
        /// is requested via IRC, the players on the server can actually see it.
        /// </remarks>
        private async Task DisplayBalanceResults(IList<PlayerInfo> teamRed, IList<PlayerInfo> teamBlue)
        {
            var red = new StringBuilder();
            var blue = new StringBuilder();

            foreach (var player in teamRed)
            {
                red.Append(string.Format("^1{0}^7, ", player.ShortName));
            }
            foreach (var player in teamBlue)
            {
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
        ///     Initiates the balance process.
        /// </summary>
        /// <param name="redTeam">The red team.</param>
        /// <param name="blueTeam">The blue team.</param>
        /// <param name="isForcedBalance">
        ///     if set to <c>true</c> then a user with required permissions is forcing balance; vote will
        ///     be bypassed.
        /// </param>
        /// <remarks> Note: I purposefully did not touch the QlCmdSay stuff in this method, so if it
        /// is requested via IRC, the players on the server can actually see it.
        /// </remarks>
        private async Task InitiateBalance(List<PlayerInfo> redTeam, List<PlayerInfo> blueTeam,
            bool isForcedBalance)
        {
            try
            {
                // Verify all player elo data prior to making any suggestions.
                await VerifyElo(_ssb.ServerInfo.CurrentPlayers);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not verify all players' Elo data. Will skip team suggestion." +
                                ex.Message);
                StatusMessage =
                    "^1[ERROR]^3 Couldn't verify player data. Team suggestion is not possible at this time.";
                return;
            }

            var allPlayers = redTeam;
            allPlayers.AddRange(blueTeam);
            _balancedRedTeam = _teamBalancer.DoBalance(allPlayers, _ssb.ServerInfo.CurrentServerGameType,
                Team.Red);
            _balancedBlueTeam = _teamBalancer.DoBalance(allPlayers, _ssb.ServerInfo.CurrentServerGameType,
                Team.Blue);
            await DisplayBalanceResults(_balancedRedTeam, _balancedBlueTeam);
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
        ///     Determines whether the command was sent from the owner of
        ///     the bot via IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was sent from IRC and from
        ///     an the IRC owner.
        /// </returns>
        private bool IsIrcOwner(CmdArgs c)
        {
            if (!c.FromIrc) return false;
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            return
                (c.FromUser.Equals(cfgHandler.Config.IrcOptions.ircAdminNickname,
                    StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Moves the players to the suggested teams.
        /// </summary>
        /// <remarks>
        /// Note: I purposefully did not touch the QlCmdSay stuff in this method, so if it
        /// is requested via IRC, the players on the server can actually see it.
        /// </remarks>
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
        ///     Starts the team suggestion vote and associated timer.
        /// </summary>
        /// <remarks>
        /// Note: I purposefully did not touch the QlCmdSay stuff in this method, so if it
        /// is requested via IRC, the players on the server can actually see it.
        /// </remarks>
        private async Task StartTeamSuggestionVote()
        {
            double interval = 20000;

            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^2[TEAMBALANCE]^7 You have {0} seconds to vote. Type ^2{1}{2}^7 to accept" +
                        " the team suggestion, ^1{1}{3}^7 to reject the suggestion.",
                        (interval / 1000), CommandList.GameCommandPrefix,
                        CommandList.CmdAcceptTeamSuggestion, CommandList.CmdRejectTeamSuggestion));
            _suggestionTimer.Interval = interval;
            _suggestionTimer.Elapsed += TeamSuggestionTimerElapsed;
            _suggestionTimer.AutoReset = false;
            _suggestionTimer.Start();
            _ssb.VoteManager.IsTeamSuggestionVotePending = true;
        }

        /// <summary>
        ///     Method called when the teams suggestion vote timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        /// <remarks>
        /// Note: I purposefully did not touch the QlCmdSay stuff in this method, so if it
        /// is requested via IRC, the players on the server can actually see it.
        /// </remarks>
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
                Debug.WriteLine(
                    "Caught exception in TeamSuggestionTimerElapsed asynchronous void (event handler) method: " +
                    ex.Message);
            }
        }

        /// <summary>
        ///     Verify all player elo data.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unable to verify player Elo data</exception>
        private async Task VerifyElo(Dictionary<string, PlayerInfo> players)
        {
            var update =
                (from player in players
                 where _qlrHelper.PlayerHasInvalidEloData(player.Value)
                 select player.Key).ToList();
            if (update.Any())
            {
                var qlrData =
                    await _qlrHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, update);
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