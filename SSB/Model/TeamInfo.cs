using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Enum;

namespace SSB.Model
{
    /// <summary>
    /// Class for team information.
    /// </summary>
    public class TeamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamInfo" /> class.
        /// </summary>
        /// <param name="shortName">The player's name only, excluding clan tag.</param>
        /// <param name="team">The team.</param>
        /// <remarks>
        /// This is information that is retrieved after using the 'configstrings' command.
        /// </remarks>
        public TeamInfo(string shortName, Team team)
        {
            ShortName = shortName;
            Team = team;
            
        }
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
