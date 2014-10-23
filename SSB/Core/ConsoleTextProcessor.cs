using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Enum;

namespace SSB.Core
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
        ///     Returns the value of a parsed cvar with the quotes removed.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The value of a parsed cvar with the quotes removed.</returns>
        public static string GetCvarValue(string text)
        {
            return text.Substring(text.IndexOf(":", StringComparison.Ordinal) + 1).Replace(@"""", "");
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
            // Handle larger, crucial events.
            DetectMultiLineEvent(text);

            // Append to our pretty UI or not
            if (_ssb.GuiOptions.IsAppendToSsbGuiConsole)
            {
                AppendConsoleTextToGui(text, length);
            }
        }

        /// <summary>
        ///     Handles the last line on input received on the QL console.
        /// </summary>
        /// <param name="msg">The text of the incoming message.</param>
        /// <param name="length">The length of the incoming message.</param>
        public async Task ProcessLastLineOfConsole(string msg, int length)
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

            await DetectSingleLineEvent(msg);
        }

        /// <summary>
        ///     Appends the console text to GUI.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="length">The length of the text.</param>
        private void AppendConsoleTextToGui(string text, int length)
        {
            // Can't update UI control from different thread
            if (_ssb.GuiControls.ConsoleTextBox.InvokeRequired)
            {
                var a = new ProcessEntireConsoleTextCb(ProcessEntireConsoleText);
                _ssb.GuiControls.ConsoleTextBox.BeginInvoke(a, new object[] { text, length });
                return;
            }
            // If appending to textbox, must clear first
            _ssb.GuiControls.ConsoleTextBox.Clear();
            _ssb.GuiControls.ConsoleTextBox.AppendText(text);
        }

        /// <summary>
        ///     Detects important QL events that occur over multiple lines of the console.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>
        ///     This method generally detects events contained within
        ///     large blocks of text related to crucial server information
        ///     such as player names & ids, server id, and map changes, whereas <see cref="DetectSingleLineEvent" />
        ///     detects important one-line events.
        /// </remarks>
        private void DetectMultiLineEvent(string text)
        {
            // 'configstrings' command has been detected; get the players and team #s from it.
            if (_ssb.Parser.CsPlayerAndTeam.IsMatch(text))
            {
                var cmd = QlCommandType.ConfigStrings;
                foreach (Match m in _ssb.Parser.CsPlayerAndTeam.Matches(text))
                {
                    text = m.Value;
                    ProcessCommand(cmd, text);
                }
            }
            // 'players' command has been detected; extract the player names and ids from it.
            else if (_ssb.Parser.PlPlayerNameAndId.IsMatch(text))
            {
                var cmd = QlCommandType.Players;
                var playersToParse = new HashSet<string>();
                foreach (Match m in _ssb.Parser.PlPlayerNameAndId.Matches(text))
                {
                    text = Strip(m.Value);
                    playersToParse.Add(text);
                }
                ProcessCommand(cmd, playersToParse);
            }
            // 'serverinfo' command has been detected; extract the server id from it.
            else if (_ssb.Parser.CvarServerPublicId.IsMatch(text))
            {
                var cmd = QlCommandType.ServerInfo;
                Match m = _ssb.Parser.CvarServerPublicId.Match(text);
                text = m.Value;
                ProcessCommand(cmd, text);
            }
            // map load or map change detected; handle it.
            else if (_ssb.Parser.EvMapLoaded.IsMatch(text))
            {
                var cmd = QlCommandType.InitInfo;
                Match m = _ssb.Parser.EvMapLoaded.Match(text);
                text = m.Value;
                ProcessCommand(cmd, text);
            }
        }

        /// <summary>
        ///     Detects various events (such as connections, disconnections, chat messages, etc.)
        ///     that occur as single lines of text within the console.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>
        ///     This method basically handles one-liners whereas <see cref="DetectMultiLineEvent" />
        ///     handles the events that occur within larger blocks of text.
        /// </remarks>
        private async Task DetectSingleLineEvent(string text)
        {
            // 'player connected' detected
            if (_ssb.Parser.EvPlayerConnected.IsMatch(text))
            {
                Match m = _ssb.Parser.EvPlayerConnected.Match(text);
                string incomingPlayer = m.Value.Replace(" connected", "");
                await _playerEventProcessor.HandleIncomingPlayerConnection(incomingPlayer);
                return;
            }
            // 'player disconnected' detected, 'player was kicked' detected, or 'player ragequits' detected
            if (_ssb.Parser.EvPlayerDisconnected.IsMatch(text) || _ssb.Parser.EvPlayerKicked.IsMatch(text) || _ssb.Parser.EvPlayerRageQuit.IsMatch(text))
            {
                Match m;
                string outgoingPlayer = string.Empty;
                if (_ssb.Parser.EvPlayerDisconnected.IsMatch(text))
                {
                    m = _ssb.Parser.EvPlayerDisconnected.Match(text);
                    outgoingPlayer = m.Value.Replace(" disconnected", "");
                }
                else if (_ssb.Parser.EvPlayerKicked.IsMatch(text))
                {
                    m = _ssb.Parser.EvPlayerKicked.Match(text);
                    outgoingPlayer = m.Value.Replace(" was kicked", "");
                }
                else if (_ssb.Parser.EvPlayerRageQuit.IsMatch(text))
                {
                    m = _ssb.Parser.EvPlayerRageQuit.Match(text);
                    outgoingPlayer = m.Value.Replace(" ragequits", "");
                }
                await _playerEventProcessor.HandleOutgoingPlayerConnection(outgoingPlayer);
                return;
            }
            // bot account name
            if (_ssb.Parser.CvarBotAccountName.IsMatch(text))
            {
                Match m = _ssb.Parser.CvarBotAccountName.Match(text);
                _ssb.ServerEventProcessor.GetBotAccountName(m.Value);
                return;
            }

            // Chat message detected
            // First make sure player is detected in our internal list, if not then do nothing.
            // Use the full clan tag (if any) and name for the comparison.
            foreach (var player in _ssb.ServerInfo.CurrentPlayers.Where(player => text.StartsWith(player.Value.ClanTagAndName + ":")))
            {
                _playerEventProcessor.HandlePlayerChatMessage(text, player.Key);
            }

            // TODO: other one-liners such as votes
        }

        /// <summary>
        ///     Processes the command.
        /// </summary>
        /// <param name="cmdType">Type of the command.</param>
        /// <param name="text">The text.</param>
        private void ProcessCommand<T>(QlCommandType cmdType, T text)
        {
            switch (cmdType)
            {
                case QlCommandType.ConfigStrings:
                    _ssb.ServerEventProcessor.GetTeamInfoFromCfgString(text as string);
                    break;

                case QlCommandType.Players:
                    var g =
                        _ssb.ServerEventProcessor.GetPlayersAndIdsFromPlayersCmd(text as IEnumerable<string>);
                    break;

                case QlCommandType.ServerInfo:
                    _ssb.ServerEventProcessor.GetServerId(text as string);
                    break;

                case QlCommandType.InitInfo:
                    _ssb.ServerEventProcessor.HandleMapLoad(text as string);
                    break;
            }
        }
    }
}