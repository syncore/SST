// ReSharper disable InconsistentNaming
namespace SST.Model.QuakeLiveApi
{
    /// <summary>
    /// Model representing a player on a QL server contained within the Server class.
    /// </summary>
    public class Player
    {
        private string _name;

        /// <summary>
        /// Gets or sets the bot.
        /// </summary>
        /// <value>The bot.</value>
        public int bot
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the clan.
        /// </summary>
        /// <value>The clan.</value>
        public string clan
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public string model
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets the rank.
        /// </summary>
        /// <value>The rank.</value>
        public int rank
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        public int score
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sub_level.
        /// </summary>
        /// <value>The sub_level.</value>
        public int sub_level
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the team.
        /// </summary>
        /// <value>The team.</value>
        public int team
        {
            get;
            set;
        }
    }
}
