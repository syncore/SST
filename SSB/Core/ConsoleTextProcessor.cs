﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for handling the QL console text.
    /// </summary>
    public class ConsoleTextProcessor
    {
        private readonly PlayerEventProcessor _playerEventProcessor;
        private readonly SynServerBot _ssb;
        private readonly VoteHandler _voteHandler;
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
            _voteHandler = new VoteHandler(_ssb);
        }

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
        public void ProcessShortConsoleLines(string msg)
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
            string[] arr = msg.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            DetectConsoleEvent(arr);
        }

        /// <summary>
        ///     Determines whether the text matches that of an accuracy server command and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the text matches that of accuracy server command info, otherwise <c>false</c>.</returns>
        private bool AccuracyInfoDetected(string text)
        {
            if (!_ssb.Parser.ScmdAccuracy.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdAccuracy.Match(text);
            _playerEventProcessor.HandlePlayerAccuracyData(m);
            return true;
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
                _ssb.GuiControls.ConsoleTextBox.BeginInvoke(a, new object[] {text, length});
                return;
            }
            // If appending to textbox, must clear first
            _ssb.GuiControls.ConsoleTextBox.Clear();
            _ssb.GuiControls.ConsoleTextBox.AppendText(text);
        }

        /// <summary>
        ///     Determines whether the text matches that of a request for the bot's name and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the text matches that of a request for the bot's name, otherwise <c>false</c>.</returns>
        private bool BotNameDetected(string text)
        {
            if (!_ssb.Parser.CvarBotAccountName.IsMatch(text)) return false;
            Match m = _ssb.Parser.CvarBotAccountName.Match(text);
            _ssb.ServerEventProcessor.GetBotAccountName(m.Value);
            return true;
        }

        /// <summary>
        ///     Determines whether the text matches that of a bot POV change in spectate mode and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private bool BotSpectatesPlayerDetected(string text)
        {
            if (!_ssb.Parser.CcmdFollowPlayer.IsMatch(text)) return false;
            Match m = _ssb.Parser.CcmdFollowPlayer.Match(text);
            string player = m.Groups["player"].Value;
            if (!Tools.KeyExists(player, _ssb.ServerInfo.CurrentPlayers))
            {
                Debug.WriteLine(
                    string.Format(
                        "POV change in spectate mode detected, but player {0} does not exist. Ignoring.",
                        player));
                return false;
            }
            _ssb.ServerInfo.PlayerCurrentlyFollowing = player;
            Debug.WriteLine(string.Format("Detected POV change in spectate mode. Currently following: {0}",
                player));
            return true;
        }

        /// <summary>
        ///     Determines whether the text matches that of a player chat message and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>
        ///     First, make sure the player is detected in the internal list of the server's current players,
        ///     if not, then do nothing. Use the full clan tag (if any) and name for the determination.
        /// </remarks>
        private bool ChatMessageDetected(string text)
        {
            if (!_ssb.Parser.ScmdChatMessage.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdChatMessage.Match(text);
            _playerEventProcessor.HandlePlayerChatMessage(m.Groups["fullplayerandmsg"].Value);
            return true;
        }

        /// <summary>
        ///     Detects various events (such as connections, disconnections, chat messages, etc.)
        ///     that occur as short lines of text within the console.
        /// </summary>
        /// <param name="events">The events.</param>
        private void DetectConsoleEvent(string[] events)
        {
            // Most of time the text will include multiple lines. Iterate and process.
            foreach (string text in events)
            {
                // 'player connected' detected.
                if (IncomingPlayerDetected(text).Result) continue;
                // player configstring info detected
                if (PlayerConfigStringDetected(text)) continue;
                // 'player disconnected' detected, 'player was kicked' detected, or 'player ragequits' detected
                if (OutgoingPlayerDetected(text).Result) continue;
                // 'player joined the spectators' detected
                if (PlayerJoinedSpectatorsDetected(text).Result) continue;
                // player accuracy data detected
                if (AccuracyInfoDetected(text)) continue;
                // player pov changes using 'follow' command in spec mode detected
                if (BotSpectatesPlayerDetected(text)) continue;
                // bot account name
                if (BotNameDetected(text)) continue;
                // vote start
                if (VoteStartDetected(text)) continue;
                // vote details
                if (VoteDetailsDetected(text)) continue;
                // vote end
                if (VoteEndDetected(text)) continue;
                // gamestate change (\time\# format)
                if (GameStateTimeChangeDetected(text)) continue;
                // intermission (game end) detected
                if (IntermissionDetected(text)) continue;
                // chat message
                if (ChatMessageDetected(text))
                {
                    /*intentionally empty*/
                }
            }
        }

        /// <summary>
        ///     Detects important QL events that occur over multiple lines of the console.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <remarks>
        ///     This method generally detects events contained within
        ///     large blocks of text related to crucial server information that we manually request
        ///     such as player names & ids via 'players' cmd, 'configstrings' cmd, 'serverinfo' cmd, map changes.
        /// </remarks>
        private void DetectMultiLineEvent(string text)
        {
            // 'configstrings' command has been detected; get the player information from it.
            if (_ssb.Parser.CfgStringPlayerInfo.IsMatch(text))
            {
                var cmd = QlCommandType.ConfigStrings;
                //ProcessCommand(cmd, _ssb.Parser.CfgStringPlayerInfo.Matches(text));
                foreach (Match m in _ssb.Parser.CfgStringPlayerInfo.Matches(text))
                {
                    ProcessCommand(cmd, m);
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
                // 'serverinfo' command has been detected; extract the relevant information from it.
            else if (_ssb.Parser.CvarServerPublicId.IsMatch(text) || _ssb.Parser.CvarGameType.IsMatch(text) ||
                     _ssb.Parser.CvarGameState.IsMatch(text))
            {
                QlCommandType cmd;
                Match m;
                if (_ssb.Parser.CvarServerPublicId.IsMatch(text))
                {
                    cmd = QlCommandType.ServerInfoServerId;
                    m = _ssb.Parser.CvarServerPublicId.Match(text);
                    ProcessCommand(cmd, m.Groups["serverid"].Value);
                }
                if (_ssb.Parser.CvarGameType.IsMatch(text))
                {
                    cmd = QlCommandType.ServerInfoServerGametype;
                    m = _ssb.Parser.CvarGameType.Match(text);
                    ProcessCommand(cmd, m.Groups["gametype"].Value);
                }
                if (_ssb.Parser.CvarGameState.IsMatch(text))
                {
                    cmd = QlCommandType.ServerInfoServerGamestate;
                    m = _ssb.Parser.CvarGameState.Match(text);
                    ProcessCommand(cmd, m.Groups["gamestate"].Value);
                }
            }
                // gamestate change detected either via bcs0 0 or cs 0 multi-line configstring
            else if (_ssb.Parser.ScmdGameStateChange.IsMatch(text))
            {
                var cmd = QlCommandType.ServerInfoServerGamestate;
                Match m = _ssb.Parser.ScmdGameStateChange.Match(text);
                ProcessCommand(cmd, m.Groups["gamestatus"].Value);
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
        ///     Determines whether a gamestate change was detected using the \time\# format.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///     <c>true</c> if a gamestate change was detected with the time info, otherwise
        ///     <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     Occassionally, this text is not printed or is missed for whatever reason, so the more accurate
        ///     method of detecting gamestate changes is through the the appropriate conditional in the
        ///     <see cref="DetectMultiLineEvent" />
        ///     method which is also performed.
        /// </remarks>
        private bool GameStateTimeChangeDetected(string text)
        {
            if (!_ssb.Parser.ScmdGameStateTimeChange.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdGameStateTimeChange.Match(text);
            if (m.Groups["time"].Value.Equals(@"\time\-1", StringComparison.InvariantCultureIgnoreCase))
            {
                _ssb.ServerInfo.CurrentServerGameState = QlGameStates.Warmup;
                Debug.WriteLine(@"Gamestate change detected (\time\ info): setting warm-up mode.");
            }
            else if (m.Groups["time"].Value.Equals(@"\time\0", StringComparison.InvariantCultureIgnoreCase))
            {
                _ssb.ServerInfo.CurrentServerGameState = QlGameStates.InProgress;
                Debug.WriteLine(@"Gamestate change detected (\time\ info): setting in-progress mode.");
            }
            return true;
        }

        /// <summary>
        ///     Determines whether the text matches that of an incoming player and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if a player connection was detected and handled, otherwise <c>false</c>.</returns>
        private async Task<bool> IncomingPlayerDetected(string text)
        {
            // 'player connected' detected.
            if (!_ssb.Parser.ScmdPlayerConnected.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdPlayerConnected.Match(text);
            await _playerEventProcessor.HandleIncomingPlayerConnection(m.Groups["player"].Value);
            return true;
        }

        /// <summary>
        ///     Determines whether an intermission (game end) was detected.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if an intermission (game end) was detected, otherwise <c>false</c>.</returns>
        private bool IntermissionDetected(string text)
        {
            if (!_ssb.Parser.ScmdIntermission.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdIntermission.Match(text);
            if (m.Groups["intermissionvalue"].Value.Equals("1", StringComparison.InvariantCultureIgnoreCase))
            {
                _ssb.ServerInfo.CurrentServerGameState = QlGameStates.Warmup;
                Debug.WriteLine("Intermission (game end) detected: setting status back to warm-up mode.");
            }
            return true;
        }

        /// <summary>
        ///     Determines whether the text matches that of an outgoing player and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if a outgoing player disconnection was detected and handled, otherwise <c>false</c>.</returns>
        /// <remarks>This handles disconnections, kicks, and ragequits.</remarks>
        private async Task<bool> OutgoingPlayerDetected(string text)
        {
            if (!_ssb.Parser.ScmdPlayerDisconnected.IsMatch(text) &&
                !_ssb.Parser.ScmdPlayerKicked.IsMatch(text) && !_ssb.Parser.ScmdPlayerRageQuits.IsMatch(text))
                return false;
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
            await _playerEventProcessor.HandleOutgoingPlayerConnection(outgoingPlayer);
            return true;
        }

        /// <summary>
        ///     Determines whether the text matches that of a player's config string and handles it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if a player's configstring was detected and handled, otherwise <c>false</c>.</returns>
        private bool PlayerConfigStringDetected(string text)
        {
            if (!_ssb.Parser.ScmdPlayerConfigString.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdPlayerConfigString.Match(text);
            _playerEventProcessor.HandlePlayerConfigString(m);
            return true;
        }

        /// <summary>
        ///     Determines whether the text matches that of a player who has joined the spectators and handle it if it does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if a player joined specs was detected and handled, otherwise <c>false</c>.</returns>
        private async Task<bool> PlayerJoinedSpectatorsDetected(string text)
        {
            if (!_ssb.Parser.ScmdPlayerJoinedSpectators.IsMatch(text)) return false;
            Match m = _ssb.Parser.ScmdPlayerJoinedSpectators.Match(text);
            await _playerEventProcessor.HandlePlayerWentToSpec(m.Groups["player"].Value);
            return true;
        }

        /// <summary>
        ///     Processes the command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmdType">Type of the command.</param>
        /// <param name="t">The text or other data to act upon.</param>
        private void ProcessCommand<T>(QlCommandType cmdType, T t)
        {
            switch (cmdType)
            {
                case QlCommandType.Players:
                    // Synchronous
                    // ReSharper disable once UnusedVariable
                    Task g =
                        _ssb.ServerEventProcessor.HandlePlayersAndIdsFromPlayersCmd(t as IEnumerable<string>);
                    break;

                case QlCommandType.ConfigStrings:
                    _playerEventProcessor.HandlePlayerConfigString(t as Match);
                    break;

                case QlCommandType.ServerInfoServerId:
                    _ssb.ServerEventProcessor.SetServerId(t as string);
                    break;

                case QlCommandType.ServerInfoServerGametype:
                    _ssb.ServerEventProcessor.SetServerGameType(t as string);
                    break;

                case QlCommandType.ServerInfoServerGamestate:
                    _ssb.ServerEventProcessor.SetServerGameState(t as string);
                    break;

                case QlCommandType.InitInfo:
                    _ssb.ServerEventProcessor.HandleMapLoad(t as string);
                    break;
            }
        }

        /// <summary>
        ///     Determines whether the details of the vote were detected.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the details of the vote were detected, otherwise <c>false</c>.</returns>
        private bool VoteDetailsDetected(string text)
        {
            if (!_ssb.Parser.ScmdVoteCalledDetails.IsMatch(text)) return false;
            _voteHandler.VoteDetails = _ssb.Parser.ScmdVoteCalledDetails.Match(text);
            return true;
        }

        /// <summary>
        ///     Determines whether the end result of a vote was detected.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the end result of the vote was detected, otherwise <c>false</c>.</returns>
        private bool VoteEndDetected(string text)
        {
            if (!_ssb.Parser.ScmdVoteFinalResult.IsMatch(text)) return false;
            // Only care about the fact that the vote has ended.
            _voteHandler.HandleVoteEnd();
            return true;
        }

        /// <summary>
        ///     Determines whether the start of a vote was detected.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the state of a vote was detected, otherwise <c>false</c>.</returns>
        private bool VoteStartDetected(string text)
        {
            if (!_ssb.Parser.ScmdVoteCalledTagAndPlayer.IsMatch(text)) return false;
            // Don't care about the player who called the vote, just fact that vote was called.
            _voteHandler.HandleVoteStart(text);
            return true;
        }

        private delegate void ProcessEntireConsoleTextCb(string text, int length);
    }
}