using System.Diagnostics;

namespace SSB
{
    /// <summary>
    ///     Class responsible for handling player events.
    /// </summary>
    public class PlayerEventProcessor
    {
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerEventProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PlayerEventProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Handles the player connection.
        /// </summary>
        /// <param name="player">The player.</param>
        public void HandlePlayerConnection(string player)
        {
            Debug.WriteLine("Detected incoming connection for " + player);
            _ssb.QlCommands.SendToQl(string.Format("tell {0} hello there {0}.", player), false);
        }
    }
}