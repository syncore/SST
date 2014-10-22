using SSB.Enum;
using SSB.Model.QlRanks;

namespace SSB.Model
{
    /// <summary>
    /// Class that represents an individual player.
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfo" /> class.
        /// </summary>
        /// <param name="shortName">The player's name only, excluding clan tag.</param>
        /// <param name="team">The team.</param>
        /// <param name="id">The identifier.</param>
        /// <remarks>
        /// This constructor is to only be used with 'configstrings' command.
        /// </remarks>
        public PlayerInfo(string shortName, Team team, string id)
        {
            ShortName = shortName;
            Team = team;
            Id = id;
        }

        // players cmd ctor
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfo" /> class.
        /// </summary>
        /// <param name="shortName">The player's name only, excluding clan tag.</param>
        /// <param name="fullName">The full name including clan tag, if any.</param>
        /// <param name="id">The identifier.</param>
        /// <remarks>
        /// This constructor is to only be used with 'players' command.
        /// </remarks>
        public PlayerInfo(string shortName, string fullName, string id)
        {
            ShortName = shortName;
            ClanTagAndName = fullName;
            Id = id;
        }

        /// <summary>
        /// Gets or sets the full player name including the clan tag.
        /// </summary>
        /// <value>
        /// The full player name including the clan tag.
        /// </value>
        public string ClanTagAndName { get; set; }

        /// <summary>
        /// Gets or sets the QLRanks elo data.
        /// </summary>
        /// <value>
        /// The QLRanks elo data.
        /// </value>
        public EloData EloData { get; set; }

        /// <summary>
        /// Gets or sets the player identifier number.
        /// </summary>
        /// <value>
        /// The player identifier number.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the player name, excluding the clan tag.
        /// </summary>
        /// <value>
        /// The player's name, excluding the clan tag.
        /// </value>
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the team.
        /// </summary>
        /// <value>
        /// The team.
        /// </value>
        public Team Team { get; set; }
    }
}