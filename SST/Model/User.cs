using SST.Enums;
using System;

namespace SST.Model
{
    /// <summary>
    /// Model class that represents an SST user.
    /// </summary>
    public class User
    {
        /// <summary>
        ///     Gets or sets the player's user (access) level.
        /// </summary>
        /// <value>
        ///     The player's user (access) level.
        /// </value>
        public UserLevel AccessLevel { get; set; }

        /// <summary>
        ///     Gets or sets the name of the player.
        /// </summary>
        /// <value>
        ///     The name of the player.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the user format display.
        /// </summary>
        /// <value>
        /// The user format display.
        /// </value>
        public string UserFormatDisplay
        {
            get
            {
                return string.Format("{0} ({1})",
                    Name, Enum.GetName(typeof(UserLevel),
                    AccessLevel));
            }
        }
    }
}