﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using SSB.Enums;
using Timer = System.Timers.Timer;

namespace SSB.Core
{
    /// <summary>
    /// Class responsible for managing various votes.
    /// </summary>
    public class VoteManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoteManager"/> class.
        /// </summary>
        public VoteManager()
        {
            QlVoteTimer = new Timer();
            TeamSuggestionVoters = new Dictionary<string, TeamBalanceVote>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether a Quake Live vote is in progress.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a QL vote is in progress; otherwise, <c>false</c>.
        /// </value>
        public bool QlVoteInProgress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a team suggestion vote is pending.
        /// </summary>
        /// <value>
        /// <c>true</c> if a team suggestion voteis  pending; otherwise, <c>false</c>.
        /// </value>
        public bool IsTeamSuggestionVotePending { get; set; }

        /// <summary>
        /// Gets or sets the vote timer.
        /// </summary>
        /// <value>
        /// The vote timer.
        /// </value>
        public Timer QlVoteTimer { get; private set; }

        /// <summary>
        /// Gets or sets the team suggestion yes vote count.
        /// </summary>
        /// <value>
        /// The team suggestion yes vote count.
        /// </value>
        public int TeamSuggestionYesVoteCount { get; set; }

        /// <summary>
        /// Gets or sets the team suggestion no vote count.
        /// </summary>
        /// <value>
        /// The team suggestion no vote count.
        /// </value>
        public int TeamSuggestionNoVoteCount { get; set; }

        /// <summary>
        /// Gets or sets the players who have already voted in the team balance vote.
        /// </summary>
        /// <value>
        /// The players who have already voted in the team balance vote.
        /// </value>
        public Dictionary<string, TeamBalanceVote> TeamSuggestionVoters { get; private set; }

        /// <summary>
        /// Starts the QL vote timer and sets the <see cref="QlVoteInProgress"/> boolean to true.
        /// </summary>
        public void StartQlVoteTimer()
        {
            QlVoteTimer.Interval = 30000;
            QlVoteTimer.AutoReset = true;
            QlVoteTimer.Elapsed += QlVoteTimerElapsed;
            QlVoteTimer.Enabled = true;
            QlVoteInProgress = true;
            Debug.WriteLine("Vote timer started. Vote is in progress.");
        }

        /// <summary>
        /// Resets the team suggestion vote.
        /// </summary>
        public void ResetTeamSuggestionVote()
        {
            TeamSuggestionNoVoteCount = 0;
            TeamSuggestionYesVoteCount = 0;
            TeamSuggestionVoters.Clear();
            IsTeamSuggestionVotePending = false;
        }

        /// <summary>
        /// Stops the QL vote timer and sets the <see cref="QlVoteInProgress"/> boolean to false.
        /// </summary>
        public void StopQlVoteTimer()
        {
            QlVoteTimer.Enabled = false;
            QlVoteInProgress = false;
            Debug.WriteLine("Vote timer stopped. Vote is no longer active.");
        }

        /// <summary>
        /// Method that is executed when the QL vote timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private void QlVoteTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            QlVoteInProgress = false;
        }
    }
}