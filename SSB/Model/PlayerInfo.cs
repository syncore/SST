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
        /// <param name="clan">The clan tag, if any.</param>
        /// <param name="team">The team.</param>
        /// <param name="id">The identifier.</param>
        /// <remarks>
        /// This constructor is to only be used with 'configstrings' command.
        /// </remarks>
        public PlayerInfo(string shortName, string clan, Team team, int id)
        {
            ShortName = shortName;
            ClanTag = clan;
            Team = team;
            Id = id;
            //pi[1] = name
            //pi[25] = ready
            //pi[37] = clan tag
            //pi[39] = subscriber
            //pi[41] = full clan name
            //pi[43] = country code
        }

        //// players cmd ctor
        ///// <summary>
        ///// Initializes a new instance of the <see cref="PlayerInfo" /> class.
        ///// </summary>
        ///// <param name="shortName">The player's name only, excluding clan tag.</param>
        ///// <param name="fullName">The full name including clan tag, if any.</param>
        ///// <param name="id">The identifier.</param>
        ///// <remarks>
        ///// This constructor is to only be used with 'players' command.
        ///// </remarks>
        //public PlayerInfo(string shortName, string fullName, string id)
        //{
        //    ShortName = shortName;
        //    ClanTagAndName = fullName;
        //    Id = id;
        //}

        /// <summary>
        /// Gets or sets the player's ready status.
        /// </summary>
        /// <value>
        /// The ready status.
        /// </value>
        public ReadyStatus Ready { get; set; }
        
        /// <summary>
        /// Gets or sets the clan tag, if any.
        /// </summary>
        /// <value>
        /// The clan tag, if any.
        /// </value>
        public string ClanTag { get; set; }

        /// <summary>
        /// Gets or sets the subscriber status.
        /// </summary>
        /// <value>
        /// The subscriber status.
        /// </value>
        public string Subscriber { get; set; }

        /// <summary>
        /// Gets or sets the full name of the clan.
        /// </summary>
        /// <value>
        /// The full name of the clan.
        /// </value>
        public string FullClanName { get; set; }

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        /// <value>
        /// The country code.
        /// </value>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the full player name including the clan tag.
        /// </summary>
        /// <value>
        /// The full player name including the clan tag.
        /// </value>
        public string ClanTagAndName {
            get { return (ClanTag + ShortName); }
        }

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
        public int Id { get; set; }

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