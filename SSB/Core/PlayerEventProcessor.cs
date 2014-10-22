using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SSB.Core
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
        public async Task HandleIncomingPlayerConnection(string player)
        {
            Debug.WriteLine("Detected incoming connection for " + player);
            // Now update the current players from server. This will also take care of
            // adding the player to our internal list and getting the player's elo data.
            await _ssb.QlCommands.QlCmdPlayers();
        }

        /// <summary>
        ///     Handles the outgoing player connection, either by disconnect or kick.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task HandleOutgoingPlayerConnection(string player)
        {
            // Remove player from our internal list
            RemovePlayer(player);
            // Now update the current players from server
            await _ssb.QlCommands.QlCmdPlayers();
            Debug.WriteLine("Detected outgoing connection for " + player);
        }

        /// <summary>
        /// Handles the player chat message.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="msgFrom">The user who sent the message.</param>
        public void HandlePlayerChatMessage(string text, string msgFrom)
        {
            string msgContent =
                ConsoleTextProcessor.Strip(text.Substring(text.IndexOf(": ", StringComparison.Ordinal) + 1))
                    .ToLowerInvariant();
            Debug.WriteLine("** Detected chat message {0} from {1} **", msgContent, msgFrom);
            // Check to see if chat message is a valid command
            if (msgContent.StartsWith(CommandProcessor.BotCommandPrefix))
            {
                var s = _ssb.CommandProcessor.ProcessBotCommand(msgFrom, msgContent);
            }
        }

        /// <summary>
        ///     Removes the player from the current in-game players.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        private void RemovePlayer(string player)
        {
            if (_ssb.ServerInfo.CurrentPlayers.Remove(player))
            {
                Debug.WriteLine(string.Format("Removed {0} from the current in-game players.", player));
            }
            else
            {
                Debug.WriteLine(
                    string.Format(
                        "Unable to remove {0} from the current in-game players. Player was not in list of current in-game players.",
                        player));
            }
        }
    }
}