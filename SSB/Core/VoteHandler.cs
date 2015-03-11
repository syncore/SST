using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Model;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for handling vote events on a QL server.
    /// </summary>
    public class VoteHandler
    {
        private readonly SynServerBot _ssb;
        private readonly DbUsers _users;
        private Match _voteDetails;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VoteHandler" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public VoteHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new DbUsers();
        }

        /// <summary>
        /// Gets or sets the vote caller; this is the short name with the clan stripped
        /// away if it existed, which corresponds to an internal bot player name;
        /// </summary>
        /// <value>
        /// The vote caller.
        /// </value>
        public string VoteCaller { get; set; }

        /// <summary>
        ///     Gets or sets the vote details.
        /// </summary>
        /// <value>
        ///     The vote details.
        /// </value>
        public Match VoteDetails
        {
            get { return _voteDetails; }
            set
            {
                _voteDetails = value;
                // Synchronous
                // ReSharper disable once UnusedVariable
                Task v = VoteDetailsSet(value);
            }
        }

        /// <summary>
        ///     Handles the end of the vote.
        /// </summary>
        public void HandleVoteEnd()
        {
            _ssb.VoteManager.StopQlVoteTimer();
            VoteCaller = string.Empty;
        }

        /// <summary>
        ///     Handles the start of the vote.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandleVoteStart(string text)
        {
            _ssb.VoteManager.StartQlVoteTimer();
        }

        /// <summary>
        ///     Denies the attempted kick of the administrator.
        /// </summary>
        /// <param name="details">The matched vote details.</param>
        private async Task DenyAdminKick(Match details)
        {
            string admin = details.Groups["votearg"].Value;
            int id;
            await _ssb.QlCommands.SendToQlAsync("vote no", false);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^1[ATTEMPTED ADMIN KICK]^7 {0} is an admin and cannot be kicked.",
                        (int.TryParse(admin, out id) ? "User with id " + id : admin)));
            Debug.WriteLine(string.Format("Denied admin kick of {0}",
                (int.TryParse(admin, out id) ? "id: " + id + "who is an admin" : admin)));
        }

        /// <summary>
        /// Denies the uneven shuffle vote.
        /// </summary>
        private async Task DenyUnevenShuffle()
        {
            await _ssb.QlCommands.SendToQlAsync("vote no", false);
            await _ssb.QlCommands.QlCmdSay("^3Please do not shuffle with an uneven number of players.");
        }

        /// <summary>
        /// Denies the disallowed vote in pickup mode.
        /// </summary>
        private async Task DenyPickupModeVotes()
        {
            await _ssb.QlCommands.SendToQlAsync("vote no", false);
            await
                _ssb.QlCommands.QlCmdSay("^3This type of vote is not allowed when pickup module is active!");
        }

        /// <summary>
        ///     Evaluates the current vote with the auto-voter module if active.
        /// </summary>
        /// <param name="details">The vote details.</param>
        private async Task EvalVoteWithAutoVote(Match details)
        {
            string votetype = details.Groups["votetype"].Value;
            string votearg = details.Groups["votearg"].Value;
            string fullVote = string.Format("{0} {1}", votetype, votearg);
            // Some votes by their nature if called properly (i.e. shuffle) will not have args.
            // But sometimes people might try to get clever and call shuffle with joke args (seen it done before)
            // so might want to handle. But better option is to inform admins to just specify shuffle as a no arg vote rule when adding.
            bool hasArg = !string.IsNullOrEmpty(votearg);

            // Vote with args -- match exactly
            foreach (AutoVote a in _ssb.Mod.AutoVoter.AutoVotes.Where
                (a => a.VoteHasArgs)
                .Where(a => hasArg && a.VoteText.Equals(fullVote, StringComparison.InvariantCultureIgnoreCase))
                )
            {
                await PerformAutoVoteAction(a);
            }
            // Generic vote (specified without args in the auto voter module) -- match beginning
            foreach (AutoVote a in _ssb.Mod.AutoVoter.AutoVotes.Where
                (a => !a.VoteHasArgs)
                .Where(a => a.VoteText.StartsWith(votetype, StringComparison.InvariantCultureIgnoreCase)))
            {
                await PerformAutoVoteAction(a);
            }
        }

        /// <summary>
        ///     Determines whether the vote is an attempted clientkick of an administrator.
        /// </summary>
        /// <param name="details">The vote details.</param>
        /// <returns>
        ///     <c>true</c> if the attempted clientkick is a kick of an administrator, otherwise <c>false</c>.
        /// </returns>
        private bool IsAdminClientKickAttempt(Match details)
        {
            string type = details.Groups["votetype"].Value;
            string votearg = details.Groups["votearg"].Value;
            if (!type.Equals("clientkick", StringComparison.InvariantCultureIgnoreCase)) return false;
            int id;
            if (!int.TryParse(votearg, out id)) return false;
            string user = _ssb.ServerEventProcessor.GetPlayerNameFromId(id);
            if (string.IsNullOrEmpty(user)) return false;
            Debug.WriteLine(string.Format("Detected attempted admin kick of {0}", user));
            return _users.GetUserLevel(user) >= UserLevel.Admin;
        }

        /// <summary>
        ///     Determines whether the vote is an attempted kick of an administrator.
        /// </summary>
        /// <param name="details">The vote details.</param>
        /// <returns>
        ///     <c>true</c> if the attempted kick is a kick of an administrator, otherwise <c>false</c>.
        /// </returns>
        private bool IsAdminKickAttempt(Match details)
        {
            string type = details.Groups["votetype"].Value;
            string votearg = details.Groups["votearg"].Value;
            if (!type.Equals("kick", StringComparison.InvariantCultureIgnoreCase)) return false;
            Debug.WriteLine(string.Format("Detected attempted admin kick of {0}", votearg));
            return _users.GetUserLevel(votearg) >= UserLevel.Admin;
        }

        /// <summary>
        /// Determines whether a shuffle vote occurs while the teams are uneven.
        /// </summary>
        /// <param name="details">The details.</param>
        /// <returns><c>true</c> if shuffle vote occurred with uneven teams, otherwise <c>false</c>
        /// </returns>
        private bool IsUnevenShuffle(Match details)
        {
            string type = details.Groups["votetype"].Value;
            if (!type.StartsWith("shuffle", StringComparison.InvariantCultureIgnoreCase)) return false;
            return _ssb.ServerInfo.HasEvenTeams();
        }

        /// <summary>
        /// Determines whether the speci.
        /// </summary>
        /// <param name="details">The details.</param>
        /// <returns><c>true</c> if the vote is one that is not allowed in pickup mode; otherwise
        /// <c>false</c></returns>
        private bool IsDisallowedPickupModeVote(Match details)
        {
            string type = details.Groups["votetype"].Value;
            // Ignore cases when the bot calls the vote, i.e. setting the teamsize when setting up the pickup teams.
            if (VoteCaller.Equals(_ssb.AccountName)) return false;
            // Shuffle votes are not allowed in pickup mode
            if (type.StartsWith("shuffle", StringComparison.InvariantCultureIgnoreCase)) return true;
            // Teamsize votes are not allowed in pickup mode;
            if (type.StartsWith("teamsize", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        ///     Performs the automatic vote action based on the type of vote.
        /// </summary>
        /// <param name="vote">The vote.</param>
        private async Task PerformAutoVoteAction(AutoVote vote)
        {
            await
                _ssb.QlCommands.SendToQlAsync(
                    (vote.IntendedResult == IntendedVoteResult.Yes ? "vote yes" : "vote no"), false);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^7[{0}^7] ^3{1}^7 matches {0}^7 rule set by ^3{2}. {3}",
                        (vote.IntendedResult == IntendedVoteResult.Yes ? "^2AUTO YES" : "^1AUTO NO"),
                        vote.VoteText, vote.AddedBy,
                        (vote.IntendedResult == IntendedVoteResult.Yes ? "^2Passing..." : "^1Rejecting...")));
        }

        /// <summary>
        ///     Method called when the vote details property has been set.
        /// </summary>
        /// <param name="details">The vote details.</param>
        private async Task VoteDetailsSet(Match details)
        {
            if (IsAdminKickAttempt(details) || IsAdminClientKickAttempt(details))
            {
                await DenyAdminKick(details);
            }
            if (IsUnevenShuffle(details))
            {
                await DenyUnevenShuffle();
            }
            if (_ssb.Mod.AutoVoter.Active)
            {
                await EvalVoteWithAutoVote(details);
            }
            if (_ssb.Mod.Pickup.Active && (_ssb.Mod.Pickup.Manager.IsPickupPreGame ||
                _ssb.Mod.Pickup.Manager.IsPickupInProgress))
            {
                if (IsDisallowedPickupModeVote(details))
                {
                    await DenyPickupModeVotes();
                }
            }
        }
    }
}