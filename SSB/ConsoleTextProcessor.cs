using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SSB
{
    /// <summary>
    ///     Class responsible for handling the QL console text.
    /// </summary>
    public class ConsoleTextProcessor
    {
        private readonly PlayerEventProcessor _playerEventProcessor;
        private readonly SynServerBot _ssb;
        private volatile int _oldLastLineLength;
        private volatile int _oldWholeConsoleLineLength;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsoleTextProcessor" /> class.
        /// </summary>
        public ConsoleTextProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _playerEventProcessor = new PlayerEventProcessor(_ssb);
        }

        private delegate void ProcessEntireConsoleTextCb(string text, int length);

        /// <summary>
        ///     Gets or sets the old length of the last line.
        /// </summary>
        /// <value>
        ///     The old length of the last line.
        /// </value>
        public int OldLastLineLength
        {
            get { return _oldLastLineLength; }
            set { _oldLastLineLength = value; }
        }

        /// <summary>
        ///     Gets or sets the old length of the whole console text.
        /// </summary>
        /// <value>
        ///     The old length of the whole console text.
        /// </value>
        public int OldWholeConsoleLineLength
        {
            get { return _oldWholeConsoleLineLength; }
            set { _oldWholeConsoleLineLength = value; }
        }

        /// <summary>
        ///     Strips the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>A trimmed string with newline characters removed.</returns>
        public static string Strip(string text)
        {
            return text.Trim().TrimEnd('\r', '\n');
        }

        /// <summary>
        ///     Detects various events (such as connections, disconnections, chat messages, etc.)
        /// </summary>
        /// <param name="text">The text.</param>
        public void DetectEvent(string text)
        {
            if (_ssb.Parser.EvPlayerConnected.IsMatch(text))
            {
                Match m = _ssb.Parser.EvPlayerConnected.Match(text);
                string incomingPlayer = m.Value.Replace(" connected", "");
                _ssb.QlCommands.QlCmdPlayers(true);
                _playerEventProcessor.HandlePlayerConnection(incomingPlayer);
                return;
            }
            if (_ssb.Parser.EvPlayerDisconnected.IsMatch(text))
            {
                Match m = _ssb.Parser.EvPlayerDisconnected.Match(text);
                string outgoingPlayer = m.Value.Replace(" disconnected", "");
                Debug.WriteLine("Detected outgoing disconnection for " + outgoingPlayer);
                _ssb.QlCommands.QlCmdPlayers(true);
                return;
            }
            if (_ssb.Parser.EvPlayerKicked.IsMatch(text))
            {
                Match m = _ssb.Parser.EvPlayerKicked.Match(text);
                string outgoingPlayer = m.Value.Replace(" was kicked", "");
                Debug.WriteLine("Detected outgoing disconnection [kick] for " + outgoingPlayer);
                _ssb.QlCommands.RemovePlayer(outgoingPlayer);
                _ssb.QlCommands.QlCmdPlayers(true);
                return;
            }

            // Chat message
            if (!_ssb.CurrentPlayers.Keys.Any(p => text.StartsWith(p + ":"))) return;
            string msgContent = Strip(text.Substring(text.IndexOf(": ", StringComparison.Ordinal) + 1));
            string msgFrom = text.Substring(0, text.IndexOf(": ", StringComparison.Ordinal));
            Debug.WriteLine("** Detected chat message {0} from {1} **", msgContent, msgFrom);

            // Commands
            if (msgContent.Equals("!hello", StringComparison.InvariantCultureIgnoreCase))
            {
                _ssb.QlCommands.SendToQl("say ^3Hi there^6!", false);
            }
            if (msgContent.Equals("!idtest", StringComparison.InvariantCultureIgnoreCase))
            {
                _ssb.QlCommands.SendToQl("say qlpt's id is: ^1" + _ssb.QlCommands.RetrievePlayerId("qlpt"),
                    false);
            }
        }

        /// <summary>
        ///     Processes all of the text currently in the QL console.
        /// </summary>
        /// <param name="text">All of the text in the QL console.</param>
        /// <param name="length">The length of all of the text in the QL console.</param>
        public void ProcessEntireConsoleText(string text, int length)
        {
            if (_oldWholeConsoleLineLength == length)
            {
                Debug.WriteLine(
                    "text length is same: {0} equals {1}...nothing new to add to console UI window.",
                    _oldWholeConsoleLineLength, length);
                return;
            }

            _oldWholeConsoleLineLength = length;

            var cmd = QlCommandType.Ignored;
            if (_ssb.Parser.CsPlayerAndTeam.IsMatch(text))
            {
                cmd = QlCommandType.ConfigStrings;
                foreach (Match m in _ssb.Parser.CsPlayerAndTeam.Matches(text))
                {
                    text = m.Value;
                    ProcessCommand(cmd, text);
                }
            }
            else if (_ssb.Parser.PlPlayerNameAndId.IsMatch(text))
            {
                cmd = QlCommandType.Players;
                foreach (Match m in _ssb.Parser.PlPlayerNameAndId.Matches(text))
                {
                    text = Strip(m.Value);
                    ProcessCommand(cmd, text);
                }
            }
            else if (_ssb.Parser.CvarServerPublicId.IsMatch(text))
            {
                cmd = QlCommandType.ServerInfo;
                Match m = _ssb.Parser.CvarServerPublicId.Match(text);
                text = m.Value;
                ProcessCommand(cmd, text);
            }

            // Append to our pretty UI or not
            if (_ssb.GuiOptions.IsAppendToSsbGuiConsole)
            {
                // Can't update UI control from different thread
                if (_ssb.GuiControls.ConsoleTextBox.InvokeRequired)
                {
                    var a = new ProcessEntireConsoleTextCb(ProcessEntireConsoleText);
                    _ssb.GuiControls.ConsoleTextBox.BeginInvoke(a, new object[] { text, length });
                    return;
                }
                //TODO: handle \r\n in text ( http://stackoverflow.com/questions/7013034/does-windows-carriage-return-r-n-consist-of-two-characters-or-one-character )
                // If appending to textbox, must clear first
                _ssb.GuiControls.ConsoleTextBox.Clear();
                _ssb.GuiControls.ConsoleTextBox.AppendText(text);
            }
        }

        /// <summary>
        ///     Handles the last line on input received on the QL console.
        /// </summary>
        /// <param name="msg">The text of the incoming message.</param>
        /// <param name="length">The length of the incoming message.</param>
        public void ProcessLastLineOfConsole(string msg, int length)
        {
            if (_oldLastLineLength == length)
            {
                Debug.WriteLine(
                    "single message text length is same: {0} equals {1}...nothing new to add.",
                    _oldLastLineLength, length);
                return;
            }
            Debug.WriteLine(string.Format("Received single console line: {0}",
                msg.Replace(Environment.NewLine, "")));
            _oldLastLineLength = length;

            // See if it's something we've issued
            if (msg.StartsWith("]"))
            {
                Debug.WriteLine(string.Format("** Detected our own command: {0} **", Strip(msg)));
                return;
            }

            DetectEvent(msg);
        }

        /// <summary>
        ///     Processes the command.
        /// </summary>
        /// <param name="cmdType">Type of the command.</param>
        /// <param name="text">The text.</param>
        private void ProcessCommand(QlCommandType cmdType, string text)
        {
            switch (cmdType)
            {
                case QlCommandType.ConfigStrings:
                    _ssb.QlCommands.GetPlayersAndTeamsFromCfgString(text);
                    _ssb.QlCommands.ClearBothQlConsoles();
                    break;

                case QlCommandType.Players:
                    _ssb.QlCommands.GetPlayersAndIdsFromPlayersCmd(text);
                    _ssb.QlCommands.ClearBothQlConsoles();
                    break;

                case QlCommandType.ServerInfo:
                    _ssb.QlCommands.GetServerId(text);
                    _ssb.QlCommands.ClearBothQlConsoles();
                    break;
            }
        }
    }
}