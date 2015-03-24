namespace SSB.Model
{
    /// <summary>
    ///     Model class that represents a player who prematurely leaves a game.
    /// </summary>
    public class EarlyQuitter
    {
        /// <summary>
        /// Gets the early quit format display.
        /// </summary>
        /// <value>
        /// The early quit format display.
        /// </value>
        public string EarlyQuitFormatDisplay
        {
            get
            {
                return string.Format("{0} ({1} {2})",
                    Name, QuitCount,
                    ((QuitCount != 1) ? "quits" : "quit"));
            }
        }

        /// <summary>
        ///     Gets or sets the name of the player.
        /// </summary>
        /// <value>
        ///     The name of the player.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the player's quit count.
        /// </summary>
        /// <value>
        ///     The player's quit count.
        /// </value>
        /// <remarks>
        ///     SQLite stores values as 64 bit integers.
        /// </remarks>
        public long QuitCount { get; set; }
    }
}