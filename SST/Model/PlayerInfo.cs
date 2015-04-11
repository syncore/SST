using SST.Enums;
using SST.Model.QlRanks;

namespace SST.Model
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
        /// This constructor is typically used with the 'configstrings' command.
        /// </remarks>
        public PlayerInfo(string shortName, string clan, Team team, int id)
        {
            ShortName = shortName;
            ClanTag = clan;
            Team = team;
            Id = id;
        }

        /// <summary>
        /// Gets or sets the player's accuracy data.
        /// </summary>
        /// <value>
        /// The player's accuracy data.
        /// </value>
        public AccuracyInfo Acc { get; set; }

        /// <summary>
        /// Gets or sets the clan tag, if any.
        /// </summary>
        /// <value>
        /// The clan tag, if any.
        /// </value>
        public string ClanTag { get; set; }

        /// <summary>
        /// Gets or sets the full player name including the clan tag.
        /// </summary>
        /// <value>
        /// The full player name including the clan tag.
        /// </value>
        public string ClanTagAndName
        {
            get { return (ClanTag + ShortName); }
        }

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        /// <value>
        /// The country code.
        /// </value>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the QLRanks elo data.
        /// </summary>
        /// <value>
        /// The QLRanks elo data.
        /// </value>
        public EloData EloData { get; set; }

        /// <summary>
        /// Gets or sets the full name of the clan.
        /// </summary>
        /// <value>
        /// The full name of the clan.
        /// </value>
        public string FullClanName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this player has made a successful
        /// sub request prior to prematurely leaving the game in the case of pick-up games.
        /// </summary>
        /// <value>
        /// <c>true</c> if this player has made a successful sub request prior to
        /// leaving a pick-up game early; otherwise, <c>false</c>.
        /// </value>
        public bool HasMadeSuccessfulSubRequest { get; set; }

        /// <summary>
        /// Gets or sets the player identifier number.
        /// </summary>
        /// <value>
        /// The player identifier number.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the player's ready status.
        /// </summary>
        /// <value>
        /// The ready status.
        /// </value>
        public ReadyStatus Ready { get; set; }

        /// <summary>
        /// Gets or sets the player name, excluding the clan tag.
        /// </summary>
        /// <value>
        /// The player's name, excluding the clan tag.
        /// </value>
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the subscriber status.
        /// </summary>
        /// <value>
        /// The subscriber status.
        /// </value>
        public string Subscriber { get; set; }

        /// <summary>
        /// Gets or sets the team.
        /// </summary>
        /// <value>
        /// The team.
        /// </value>
        public Team Team { get; set; }
    }
}