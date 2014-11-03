using SSB.Enum;

namespace SSB.Model
{
    /// <summary>
    /// Model class representing a vote for use with the auto-voter module.
    /// </summary>
    public class AutoVote
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoVote" /> class.
        /// </summary>
        /// <param name="voteText">The text of the vote's string.</param>
        /// <param name="hasArgs">if set to <c>true</c> then vote has arguments.</param>
        /// <param name="intendedResult">The intended result of the vote.</param>
        /// <param name="addedBy">The name of the admin who added this auto-vote rule.</param>
        public AutoVote(string voteText, bool hasArgs, IntendedVoteResult intendedResult, string addedBy)
        {
            VoteText = voteText;
            VoteHasArgs = hasArgs;
            IntendedResult = intendedResult;
            AddedBy = addedBy;
        }

        /// <summary>
        /// Gets or sets the name of the admin who added this auto-vote rule.
        /// </summary>
        /// <value>
        /// The name of the admin who added this auto-vote rule.
        /// </value>
        public string AddedBy { get; private set; }

        /// <summary>
        /// Gets or sets the intended vote result.
        /// </summary>
        /// <value>
        /// The intended vote result.
        /// </value>
        public IntendedVoteResult IntendedResult { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the vote text has arguments.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the vote text has arguments; otherwise, <c>false</c>.
        /// </value>
        public bool VoteHasArgs { get; private set; }

        /// <summary>
        /// Gets or sets the text of the vote string.
        /// </summary>
        /// <value>
        /// The text of the vote string.
        /// </value>
        public string VoteText { get; private set; }
    }
}