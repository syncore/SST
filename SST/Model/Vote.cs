﻿using SST.Enums;

namespace SST.Model
{
    /// <summary>
    /// Model class representing a Quake Live vote.
    /// </summary>
    public class Vote
    {
        /// <summary>
        /// Gets or sets the name of the vote.
        /// </summary>
        /// <value>The name of the vote.</value>
        /// <remarks>This represents the name of the vote in Quake Live.</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of vote.
        /// </summary>
        /// <value>The type of vote.</value>
        public VoteType Type { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
