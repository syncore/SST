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
        private volatile string _oldLastLineText;
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
        ///     Handles small lines of console text
        /// </summary>
        /// <param name="msg">The text of the incoming message.</param>
        //public async Task ProcessLastLineOfConsole(string msg, int length)
        public async Task ProcessShortConsoleLines(string msg)
        {
            if (msg.Equals(_oldLastLineText)) return;

            Debug.WriteLine(string.Format("Received console text: {0}", msg));
            _oldLastLineText = msg;
            // See if it's something we've issued
            if (msg.StartsWith("]"))
            {
                Debug.WriteLine(string.Format("** Detected our own command: {0} **", Strip(msg)));
                return;
            }
            // Batch process, as there will sometimes be multiple lines.
            // TODO: Fix this. This is terrible and very buggy
            string[] arr = msg.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            await DetectConsoleEvent(arr);
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
        /// Detects various events (such as connections, disconnections, chat messages, etc.)
        /// that occur as short lines of text within the console.
        /// </summary>
        /// <param name="events">The events.</param>
        private async Task DetectConsoleEvent(string[] events)
        {
            // Sometimes the text will include multiple lines. Iterate and process.
            foreach (var text in events)
            {
                // 'player connected' detected.
                if (_ssb.Parser.ScmdPlayerConnected.IsMatch(text))
                {
                    Match m = _ssb.Parser.ScmdPlayerConnected.Match(text);
                    _playerEventProcessor.HandleIncomingPlayerConnection(m.Groups["player"].Value);
                    continue;
                }
                
                // player configstring info detected
                if (_ssb.Parser.CsPlayerInfo.IsMatch(text))
                {
                    Match m = _ssb.Parser.CsPlayerInfo.Match(text);
                    await _playerEventProcessor.HandlePlayerConfigString(m);
                    continue;
                }
                
                // 'player disconnected' detected, 'player was kicked' detected, or 'player ragequits' detected
                if (_ssb.Parser.ScmdPlayerDisconnected.IsMatch(text) || _ssb.Parser.ScmdPlayerKicked.IsMatch(text) || _ssb.Parser.ScmdPlayerRageQuits.IsMatch(text))
                {
                    Match m;
                    string outgoingPlayer = string.Empty;
                    if (_ssb.Parser.ScmdPlayerDisconnected.IsMatch(text))
                    {
                        m = _ssb.Parser.ScmdPlayerDisconnected.Match(text);
                        outgoingPlayer = m.Groups["player"].Value;
                    }
                    else if (_ssb.Parser.ScmdPlayerKicked.IsMatch(text))
                    {
                        m = _ssb.Parser.ScmdPlayerKicked.Match(text);
                        outgoingPlayer = m.Groups["player"].Value;
                    }
                    else if (_ssb.Parser.ScmdPlayerRageQuits.IsMatch(text))
                    {
                        m = _ssb.Parser.ScmdPlayerRageQuits.Match(text);
                        outgoingPlayer = m.Groups["player"].Value;
                    }
                    _playerEventProcessor.HandleOutgoingPlayerConnection(outgoingPlayer);
                    continue;
                }
                // bot account name
                if (_ssb.Parser.CvarBotAccountName.IsMatch(text))
                {
                    Match m = _ssb.Parser.CvarBotAccountName.Match(text);
                    _ssb.ServerEventProcessor.GetBotAccountName(m.Value);
                    continue;
                }

                // Chat message detected
                // First make sure player is detected in our internal list, if not then do nothing.
                // Use the full clan tag (if any) and name for the comparison.

                // Foreach in closure
                string text1 = text;
                foreach (var player in _ssb.ServerInfo.CurrentPlayers.Where(player => text1.StartsWith(player.Value.ClanTagAndName + ":")))
                {
                    _playerEventProcessor.HandlePlayerChatMessage(text, player.Key);
                }

                // TODO: other one-liners such as votes
            }
        }

        /// <summary>
        ///     Detects important QL events that occur over multiple lines of the console.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>
        ///     This method generally detects events contained within
        ///     large blocks of text related to crucial server information that we manually request
        ///     such as player names & ids, server id, and map changes.
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
        ///     Processes the command.
        /// </summary>
        /// <param name="cmdType">Type of the command.</param>
        /// <param name="text">The text.</param>
        private void ProcessCommand<T>(QlCommandType cmdType, T text)
        {
            switch (cmdType)
            {
                //case QlCommandType.ConfigStrings:
                //    _ssb.ServerEventProcessor.GetTeamInfoFromCfgString(text as string);
                //    break;

                case QlCommandType.Players:
                    var g =
                        _ssb.ServerEventProcessor.HandlePlayersAndIdsFromPlayersCmd(text as IEnumerable<string>);
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