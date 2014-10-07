namespace SSB
{
    /// <summary>
    /// Class that represents an individual player.
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="team">The team.</param>
        /// <param name="id">The identifier.</param>
        /// <remarks>This constructor is to only be used with 'configstrings' command.</remarks>
        public PlayerInfo(string name, Team team, string id)
        {
            Name = name;
            Team = team;
            Id = id;
        }

        // players cmd ctor
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The identifier.</param>
        /// <remarks>This constructor is to only be used with 'players' command.</remarks>
        public PlayerInfo(string name, string id)
        {
            Name = name;
            Id = id;
        }

        /// <summary>
        /// Gets or sets the player identifier number.
        /// </summary>
        /// <value>
        /// The player identifier number.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the player name.
        /// </summary>
        /// <value>
        /// The player name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the team.
        /// </summary>
        /// <value>
        /// The team.
        /// </value>
        public Team Team { get; set; }
    }
}