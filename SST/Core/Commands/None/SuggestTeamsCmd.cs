using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using SST.Config;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    ///     Command: Suggest balanced teams based on player Elo data.
    /// </summary>
    public class SuggestTeamsCmd : IBotCommand
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:SUGGEST]";
        private readonly QlRanksHelper _qlrHelper;
        private readonly SynServerTool _sst;
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
        /// <param name="sst">The main class.</param>
        public SuggestTeamsCmd(SynServerTool sst)
        {
            _sst = sst;
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
            if (_sst.ServerInfo.CurrentServerGameType != QlGameTypes.Ca &&
                _sst.ServerInfo.CurrentServerGameType != QlGameTypes.Ctf &&
                _sst.ServerInfo.CurrentServerGameType != QlGameTypes.Tdm)
            {
                StatusMessage = "^1[ERROR]^3 Team balancing is not available on this server!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to request team balance suggestion, but server is running game type" +
                    " unsupported by QLRanks. Ignoring.", c.FromUser), _logClassType, _logPrefix);

                return false;
            }

            // Disable this command if the pickup module is active
            if (_sst.Mod.Pickup.Active)
            {
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);
                StatusMessage = "^1[ERROR]^3 Team balancing is unavailable when pickup module is active!";

                Log.Write(string.Format(
                    "{0} attempted to request team balance suggestion, but balancing is unavailable when" +
                    " pickup module is active. Ignoring.", c.FromUser), _logClassType, _logPrefix);

                return false;
            }

            var blueTeam = _sst.ServerInfo.GetTeam(Team.Blue);
            var redTeam = _sst.ServerInfo.GetTeam(Team.Red);
            var redAndBlueTotalPlayers = (blueTeam.Count + redTeam.Count);

            if ((redAndBlueTotalPlayers) % 2 != 0)
            {
                StatusMessage = "^1[ERROR]^3 Teams can only be suggested if there are a total even number of red and blue players!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to request team balance suggestion, but teams are uneven. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                return false;
            }
            if ((redAndBlueTotalPlayers) < 4)
            {
                StatusMessage = "^1[ERROR]^3 There must be at least 4 total players for the team suggestion!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to request team balance suggestion, but there are fewer than 4 players. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                return false;
            }

            if (_sst.VoteManager.IsTeamSuggestionVotePending)
            {
                StatusMessage = "^1[ERROR]^3 A team balance vote is already pending!";
                // send to everyone (/say; success)
                await SendServerSay(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to request team balance suggestion, but balance suggestion vote is already pending. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

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

                    Log.Write(string.Format(
                    "{0} tried to force team balance suggestion, but has an insufficient access level. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                    return false;
                }

                Log.Write(string.Format("Player {0} with sufficient access level {1} forced balanced teams from {2}.",
                    c.FromUser, Enum.GetName(typeof(UserLevel), userLevel),
                    (c.FromIrc ? "IRC" : "in-game")), _logClassType, _logPrefix);

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
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
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

            await _sst.QlCommands.QlCmdSay("^2[TEAMBALANCE]^7 Suggested ^2balanced^7 teams are:");
            await
                _sst.QlCommands.QlCmdSay(string.Format(
                    "^1[RED]: {0}", red.ToString().TrimEnd(',', ' ')));

            await
                _sst.QlCommands.QlCmdSay(string.Format(
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
                await VerifyElo(_sst.ServerInfo.CurrentPlayers);
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Could not verify all players' Elo data. Will skip team suggestion." +
                                ex.Message, _logClassType, _logPrefix);
                StatusMessage =
                    "^1[ERROR]^3 Couldn't verify player data. Team suggestion is not possible at this time.";
                return;
            }

            var allPlayers = redTeam;
            allPlayers.AddRange(blueTeam);
            _balancedRedTeam = _teamBalancer.DoBalance(allPlayers, _sst.ServerInfo.CurrentServerGameType,
                Team.Red);
            _balancedBlueTeam = _teamBalancer.DoBalance(allPlayers, _sst.ServerInfo.CurrentServerGameType,
                Team.Blue);
            await DisplayBalanceResults(_balancedRedTeam, _balancedBlueTeam);
            if (!isForcedBalance)
            {
                await StartTeamSuggestionVote();
            }
            else
            {
                _sst.VoteManager.IsTeamSuggestionVotePending = true;
                await _sst.QlCommands.QlCmdSay("^2[TEAMBALANCE]^7 Forcing team balance.");
                await MovePlayersToBalancedTeams();
                _sst.VoteManager.IsTeamSuggestionVotePending = false;
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
            var cfg = cfgHandler.ReadConfiguration();
            return
                (c.FromUser.Equals(cfg.IrcOptions.ircAdminNickname,
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
            Log.Write("Attempting to move players to balanced teams.", _logClassType, _logPrefix);

            await _sst.QlCommands.QlCmdSay("^2[TEAMBALANCE]^7 Balancing teams, please wait....");
            foreach (var player in _balancedBlueTeam)
            {
                await _sst.QlCommands.CustCmdPutPlayer(player.ShortName, Team.Blue);
            }
            foreach (var player in _balancedRedTeam)
            {
                await _sst.QlCommands.CustCmdPutPlayer(player.ShortName, Team.Red);
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
                _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^2[TEAMBALANCE]^7 You have {0} seconds to vote. Type ^2{1}{2}^7 to accept" +
                        " the team suggestion, ^1{1}{3}^7 to reject the suggestion.",
                        (interval / 1000), CommandList.GameCommandPrefix,
                        CommandList.CmdAcceptTeamSuggestion, CommandList.CmdRejectTeamSuggestion));
            _suggestionTimer.Interval = interval;
            _suggestionTimer.Elapsed += TeamSuggestionTimerElapsed;
            _suggestionTimer.AutoReset = false;
            _suggestionTimer.Start();
            _sst.VoteManager.IsTeamSuggestionVotePending = true;

            Log.Write(string.Format("Started team suggestion vote. {0} seconds until results are known.",
                (interval / 1000)), _logClassType, _logPrefix);
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
                    _sst.QlCommands.QlCmdSay(
                        string.Format("^2[TEAMBALANCE]^7 Vote results -- YES: ^2{0}^7 -- NO: ^1{1}^7 -- {2}",
                            _sst.VoteManager.TeamSuggestionYesVoteCount,
                            _sst.VoteManager.TeamSuggestionNoVoteCount,
                            ((_sst.VoteManager.TeamSuggestionYesVoteCount >
                              _sst.VoteManager.TeamSuggestionNoVoteCount)
                                ? ("Teams will be balanced.")
                                : "Teams will remain unchanged.")));

                if (_sst.VoteManager.TeamSuggestionYesVoteCount > _sst.VoteManager.TeamSuggestionNoVoteCount)
                {
                    Log.Write(
                        "Team suggestion 'YES' vote count is greater than 'NO' vote count. Will attempt" +
                        " to move players to balanced teams.", _logClassType, _logPrefix);

                    await MovePlayersToBalancedTeams();
                }
                else
                {
                    Log.Write(
                        "Team suggestion 'NO' vote count is greater than 'YES' vote count. Teams will remain unchanged",
                        _logClassType, _logPrefix);
                }

                // Reset votes
                _sst.VoteManager.ResetTeamSuggestionVote();
            }
            catch (Exception)
            {
                // ignored
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
                    await _qlrHelper.RetrieveEloDataFromApiAsync(_sst.ServerInfo.CurrentPlayers, update);
                if (qlrData == null)
                {
                    await _sst.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Unable to verify player data. Try again in a few seconds.");

                    Log.WriteCritical("Unable to verify player Elo data.", _logClassType, _logPrefix);
                    throw new Exception("Unable to verify player Elo data");
                }
            }
        }
    }
}