﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Core.Commands.Modules;
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
        private readonly Users _users;
        private Match _voteDetails;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VoteHandler" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public VoteHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
        }

        /// <summary>
        ///     Gets or sets the vote details.
        /// </summary>
        /// <value>
        ///     The vote details.
        /// </value>
        public Match VoteDetails
        {
            get
            {
                return _voteDetails;
            }
            set
            {
                _voteDetails = value;
                Task v = VoteDetailsSet(value);
            }
        }

        /// <summary>
        ///     Handles the end of the vote.
        /// </summary>
        public void HandleVoteEnd()
        {
            _ssb.ServerInfo.StopVoteTimer();
        }

        /// <summary>
        ///     Handles the start of the vote.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandleVoteStart(string text)
        {
            _ssb.ServerInfo.StartVoteTimer();
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
        /// Evaluates the current vote with the auto-voter module if active.
        /// </summary>
        /// <param name="details">The vote details.</param>
        private async Task EvalVoteWithAutoVote(Match details)
        {
            string votetype = details.Groups["votetype"].Value;
            string votearg = details.Groups["votearg"].Value;
            string fullVote = string.Format("{0} {1}", votetype, votearg);
            // Some votes by their nature if called properly (i.e. shuffle) will not have args.
            // But sometimes people might try to get clever and call shuffle with joke args (seen it done before),
            // so might want to handle. But better option is to inform admins to just specify shuffle as a no arg vote rule when adding.
            bool hasArg = !string.IsNullOrEmpty(votearg);

            // Vote with args -- match exactly
            foreach (AutoVote a in _ssb.CommandProcessor.Mod.AutoVoter.AutoVotes.Where
                (a => a.VoteHasArgs).Where(a => hasArg && a.VoteText.Equals(fullVote, StringComparison.InvariantCultureIgnoreCase)))
            {
                await PerformAutoVoteAction(a);
            }
            // Generic vote (specified without args in the auto voter module) -- match beginning
            foreach (AutoVote a in _ssb.CommandProcessor.Mod.AutoVoter.AutoVotes.Where
                (a => !a.VoteHasArgs).Where(a => a.VoteText.StartsWith(votetype, StringComparison.InvariantCultureIgnoreCase)))
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
            var user = _ssb.ServerEventProcessor.GetPlayerNameFromId(id);
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
        /// Performs the automatic vote action based on the type of vote.
        /// </summary>
        /// <param name="vote">The vote.</param>
        private async Task PerformAutoVoteAction(AutoVote vote)
        {
            await _ssb.QlCommands.SendToQlAsync((vote.IntendedResult == IntendedVoteResult.Yes ? "vote yes" : "vote no"), false);
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
            if (AutoVoter.IsModuleActive)
            {
                await EvalVoteWithAutoVote(details);
            }
        }
    }
}