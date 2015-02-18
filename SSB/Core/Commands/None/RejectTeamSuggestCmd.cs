using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: vote no to a team balance suggestion.
    /// </summary>
    public class RejectTeamSuggestCmd : IBotCommand
    {
        private bool _isIrcAccessAllowed = false;
        private int _minArgs = 0;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RejectTeamSuggestCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public RejectTeamSuggestCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

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
        /// <returns>null, since this command requires no arguments.</returns>
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!_ssb.VoteManager.IsTeamSuggestionVotePending)
            {
                await _ssb.QlCommands.QlCmdSay(
                    "^1[ERROR]^3 A team balance vote is not in progress.");
                return;
            }
            if (!Helpers.KeyExists(c.FromUser, _ssb.ServerInfo.CurrentPlayers))
            {
                Debug.WriteLine(string.Format("{0} is not in the list of current players, ignoring vote",
                    c.FromUser));
                return;
            }
            if (_ssb.ServerInfo.CurrentPlayers[c.FromUser].Team.Equals(Team.None) ||
                _ssb.ServerInfo.CurrentPlayers[c.FromUser].Team.Equals(Team.Spec))
            {
                await _ssb.QlCommands.QlCmdSay(
                    "^1[ERROR]^3 Only active players may vote.");
                return;
            }
            if (Helpers.KeyExists(c.FromUser, _ssb.VoteManager.TeamSuggestionVoters))
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format("^1[ERROR]^3 {0} has already voted.", c.FromUser));
                return;
            }
            _ssb.VoteManager.TeamSuggestionNoVoteCount++;
            _ssb.VoteManager.TeamSuggestionVoters.Add(c.FromUser, TeamBalanceVote.No);
            Debug.WriteLine(string.Format("Counted 'no' team suggestion vote for {0}", c.FromUser));
        }
    }
}