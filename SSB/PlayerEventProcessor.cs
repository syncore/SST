using System;
using System.Diagnostics;
using System.Linq;

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
        public void HandleIncomingPlayerConnection(string player)
        {
            Debug.WriteLine("Detected incoming connection for " + player);
            // Now update the current players from server. This will also take care of
            // adding the player to our internal list.
            _ssb.QlCommands.QlCmdPlayers();
            _ssb.QlCommands.SendToQl(string.Format("tell {0} hello there {0}.", player), false);
        }

        /// <summary>
        ///     Handles the outgoing player connection, either by disconnect or kick.
        /// </summary>
        /// <param name="player">The player.</param>
        public void HandleOutgoingPlayerConnection(string player)
        {
            // Now update the current players
            _ssb.QlCommands.QlCmdPlayers();
            // Remove player from our internal list
            RemovePlayer(player);
            // Now update the current players from server
            _ssb.QlCommands.QlCmdPlayers();
            Debug.WriteLine("Detected outgoing connection for " + player);
        }

        /// <summary>
        ///     Handles the player chat message.
        /// </summary>
        /// <param name="text">The text.</param>
        public void HandlePlayerChatMessage(string text)
        {
            string msgContent =
                ConsoleTextProcessor.Strip(text.Substring(text.IndexOf(": ", StringComparison.Ordinal) + 1));
            string msgFrom = text.Substring(0, text.IndexOf(": ", StringComparison.Ordinal));
            Debug.WriteLine("** Detected chat message {0} from {1} **", msgContent, msgFrom);

            // Commands
            if (_ssb.BotCommands.AllBotCommands.Any(msgContent.StartsWith))
            {
                _ssb.BotCommands.ProcessBotCommand(msgContent);
            }
            
            //if (msgContent.Equals("!hello", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    _ssb.QlCommands.SendToQl("say ^3Hi there^6!", false);
            //}
            //if (msgContent.Equals("!idtest", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    _ssb.QlCommands.SendToQl("say qlpt's id is: ^1" + RetrievePlayerId("qlpt"),
            //        false);
            //}
        }

        /// <summary>
        ///     Removes the player from the current in-game players.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        private void RemovePlayer(string player)
        {
            if (_ssb.CurrentPlayers.Remove(player))
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

        /// <summary>
        ///     Retrieves a given player's player id (clientnum) from our internal list or
        ///     queries the server with the 'players' command and returns the id if the player is
        ///     not detected.
        /// </summary>
        /// <param name="player">The player whose id needs to be retrieved.</param>
        /// <returns>The player</returns>
        private string RetrievePlayerId(string player)
        {
            PlayerInfo pinfo;
            string id = string.Empty;
            if (_ssb.CurrentPlayers.TryGetValue(player, out pinfo))
            {
                Debug.WriteLine("Retrieved id {0} for player {1}", id, player);
                id = pinfo.Id;
            }
            else
            {
                // Player doesn't exist, request players from server
                _ssb.QlCommands.QlCmdPlayers();
                // Try again
                if (!_ssb.CurrentPlayers.TryGetValue(player, out pinfo)) return id;
                Debug.WriteLine("Retrieved id {0} for player {1}", id, player);
                id = pinfo.Id;
                // Only clear if we've had to use 'players' command
                _ssb.QlCommands.ClearBothQlConsoles();
            }

            return id;
        }
    }
}