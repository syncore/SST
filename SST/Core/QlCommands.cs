using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SST.Enums;
using SST.Util;

namespace SST.Core
{
    /// <summary>
    /// Class responsible for sending various commands to the QL game instance.
    /// </summary>
    public class QlCommands
    {
        private const int DefaultCommandDelayMsec = 600;
        private const int MaxChatlineLength = 134;
        private const int MaxTellLineLength = 106;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CORE]";
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="QlCommands"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public QlCommands(SynServerTool sst)
        {
            _sst = sst;
        }

        public async Task CheckCmdStatus()
        {
            await SendToQlAsync("cmd", false);
            //QlCmdClear();
            Log.Write("Checking status of server connection (cmd).", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Checks the main menu (ui_mainmenu) status.
        /// </summary>
        public async Task CheckMainMenuStatus()
        {
            await SendToQlAsync("ui_mainmenu", false);
            //QlCmdClear();
            Log.Write("Checking QL main menu status.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Clear both the QL WinConsole and the in-game console.
        /// </summary>
        public void ClearBothQlConsoles()
        {
            // Windows console window
            ClearQlWinConsole();
            // In-game console window
            QlCmdClear();
        }

        /// <summary>
        /// Clears the Ql windows console.
        /// </summary>
        public void ClearQlWinConsole()
        {
            var consoleWindow = _sst.QlWindowUtils.GetQuakeLiveConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                var child = Win32Api.FindWindowEx(consoleWindow, IntPtr.Zero, "Button", "clear");
                Win32Api.SendMessage(child, Win32Api.BN_CLICKED, IntPtr.Zero, IntPtr.Zero);
                Win32Api.SendMessage(child, Win32Api.BN_CLICKED, IntPtr.Zero, IntPtr.Zero);
                // Re-focus the window
                Win32Api.SwitchToThisWindow(QlWindowUtils.QlWindowHandle, true);
            }
            else
            {
                Log.WriteCritical("Unable to get necessary console handle.", _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Sends the 'unban' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task CmdUnban(string player)
        {
            await SendToQlAsync(string.Format("unban {0}", player), false);

            Log.Write(string.Format("Attempted to unban player {0} using QL's ban system.",
                player), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sends the 'kickban' command to QL after the specified delay.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="delayInSecs">The delay in secs.</param>
        /// <remarks>
        /// This is typically used as a courtesy when auto-kickbanning players so that they have
        /// some idea why they were kicked.
        /// </remarks>
        public async Task CustCmdDelayedKickban(string player, int delayInSecs)
        {
            var id = _sst.ServerEventProcessor.GetPlayerId(player.ToLowerInvariant());
            if (id != -1)
            {
                await Task.Delay(delayInSecs * 1000);
                await SendToQlAsync(string.Format("kickban {0}", id), false);
                Log.Write(string.Format(
                    "Attempted to kickban player {0} (id: {1}) using QL's ban system.",
                    player, id), _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(string.Format(
                    "Unable to send kickban for player {0} because player ID could not be retrieved.",
                    player), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Sends the 'kickban' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        /// Our version takes the player name as an argument and converts it into the playerID
        /// required for the actual QL command.
        /// </remarks>
        public async Task CustCmdKickban(string player)
        {
            var id = _sst.ServerEventProcessor.GetPlayerId(player.ToLowerInvariant());
            if (id != -1)
            {
                await SendToQlAsync(string.Format("kickban {0}", id), false);
                Log.Write(string.Format(
                    "Attempted to kickban player {0} (id: {1}) using QL's ban system.",
                    player, id), _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(string.Format(
                    "Unable to send kickban for player {0} because player ID could not be retrieved.",
                    player), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Sends the 'put' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        /// <remarks>
        /// Our version takes the player name as an argument and converts it into the playerID
        /// required for the actual QL command.
        /// </remarks>
        public async Task CustCmdPutPlayer(string player, Team team)
        {
            var id = _sst.ServerEventProcessor.GetPlayerId(player);
            if (id != -1)
            {
                switch (team)
                {
                    case Team.Blue:
                        await SendToQlAsync(string.Format("put {0} b", id), false);
                        break;

                    case Team.Red:
                        await SendToQlAsync(string.Format("put {0} r", id), false);
                        break;

                    case Team.Spec:
                        await SendToQlAsync(string.Format("put {0} s", id), false);
                        break;
                }

                Log.Write(string.Format("Attempted to move player {0} to team {1}",
                    player, Enum.GetName(typeof(Team), team)), _logClassType, _logPrefix);
            }
            else
            {
                Log.Write(string.Format(
                    "Unable to move player {0} to team {1} because player ID could not be retrieved",
                    player, Enum.GetName(typeof(Team), team)), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Sends the /put command after a given delay in seconds.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        /// <param name="runCmdInSeconds">
        /// The time to wait, in seconds, before sending the 'put' command.
        /// </param>
        /// <returns></returns>
        public async Task CustCmdPutPlayerDelayed(string player, Team team, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            await CustCmdPutPlayer(player, team);
        }

        /// <summary>
        /// Disables in-game console printing. <remarks> With this set (con_noprint 1) no text will
        /// be shown in the in-game console. This is the preferred mode when developer mode is
        /// enabled. Note, text will ALWAYS be appended to the winconsole, regardless of this
        /// setting. </remarks>
        /// </summary>
        public void DisableConsolePrinting()
        {
            SendQlCommand("con_noprint 1", false);
            QlCmdClear();
            Log.Write("Disabling in-game console printing.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Disables the developer mode.
        /// </summary>
        public void DisableDeveloperMode()
        {
            SendQlCommand("developer 0", false);
            QlCmdClear();
            Log.Write("Disabling developer mode.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Enables in-game console printing.
        /// </summary>
        /// <remarks>
        /// With this set (con_noprint 0), text will be shown on the in-game console.
        /// Note: this might be annoying when attempting to play with developer mode on since there
        ///       will be multiple 'tinfo' messages.
        /// </remarks>
        public void EnableConsolePrinting()
        {
            SendQlCommand("con_noprint 0", false);
            QlCmdClear();
            Log.Write("Enabling in-game console printing.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Enables the developer mode.
        /// </summary>
        public void EnableDeveloperMode()
        {
            SendQlCommand("developer 1", false);
            QlCmdClear();
            Log.Write("Enabling developer mode.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sends the 'clear' command to QL.
        /// </summary>
        public void QlCmdClear()
        {
            SendQlCommand("clear", false);
        }

        /// <summary>
        /// Sends the 'configstrings' command to QL.
        /// </summary>
        public async Task QlCmdConfigStrings()
        {
            //await SendToQlAsync("configstrings", true);
            await SendToQlAsync("configstrings", false);
            QlCmdClear();
        }

        /// <summary>
        /// Sends the /say command after a given delay in seconds.
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// <param name="runCmdInSeconds">
        /// The time to wait, in seconds, before sending the 'say' command.
        /// </param>
        public async Task QlCmdDelayedSay(string text, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            await QlCmdSay(text);
        }

        /// <summary>
        /// Sends the /tell command to a player after a given delay in seconds.
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// <param name="player">The player.</param>
        /// <param name="runCmdInSeconds">
        /// The time to wait, in seconds, before sending the 'tell' command.
        /// </param>
        public async Task QlCmdDelayedTell(string text, string player, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            await QlCmdTell(text, player);
        }

        /// <summary>
        /// Sends the 'players' command to QL.
        /// </summary>
        public async Task QlCmdPlayers()
        {
            await SendToQlAsync("players", true);
        }

        /// <summary>
        /// Sends the 'say' command to QL. If too much text is received then issue 'say' command
        /// over multiple lines.
        /// </summary>
        /// <param name="text">The text to say.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdSay(string text)
        {
            // Text to send might be too long, so send over multiple lines. Line length of between
            // 98 & 115 chars is probably optimal for lower resolutions based on guestimate.
            // However, QL actually supports sending up to 135 characters at a time.
            if ((text.Length) > MaxChatlineLength)
            {
                // ReSharper disable once PossibleLossOfFraction : .5 ensures we always round up to next int, no matter size
                var l = ((text.Length / MaxChatlineLength) + .5);
                var linesRoundUp = Math.Ceiling(l);
                try
                {
                    var numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    var startPos = 0;
                    var lastColor = string.Empty;
                    Log.Write(string.Format(
                        "Must process very large text of length {0} characters for chat message. Will send to QL over {1} lines.",
                        text.Length, numLines), _logClassType, _logPrefix);
                    for (var i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_sst.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                var m = _sst.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
                                lastColor = m[m.Count - 1].Value;
                            }
                            if (multiLine[i - 1].EndsWith("^"))
                            {
                                lastColor = "^";
                            }
                        }
                        if (i == multiLine.Length - 1)
                        {
                            // last iteration; string length cannot be specified
                            multiLine[i] = string.Format("{0}{1}", lastColor, text.Substring(startPos));
                        }
                        else
                        {
                            multiLine[i] = string.Format("{0}{1}", lastColor,
                                text.Substring(startPos, MaxChatlineLength));
                        }

                        // QL drops any server command that exceeds 500 ms threshold
                        await Task.Delay(DefaultCommandDelayMsec);
                        DoSay(multiLine[i]);
                        startPos += MaxChatlineLength;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical("Unable send chat message text to QL: " + ex.Message,
                        _logClassType, _logPrefix);
                }
            }
            else
            {
                await Task.Delay(DefaultCommandDelayMsec);
                DoSay(text);
            }
        }

        /// <summary>
        /// Sends the 'say_team' command to QL. If too much text is received then issue 'say_team'
        /// command over multiple lines.
        /// </summary>
        /// <param name="text">The text to say.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdSayTeam(string text)
        {
            // Text to send might be too long, so send over multiple lines. Line length of between
            // 98 & 115 chars is probably optimal for lower resolutions based on guestimate.
            // However, QL actually supports sending up to 135 characters at a time.
            if ((text.Length) > MaxChatlineLength)
            {
                // ReSharper disable once PossibleLossOfFraction : .5 ensures we always round up to next int, no matter size
                var l = ((text.Length / MaxChatlineLength) + .5);
                var linesRoundUp = Math.Ceiling(l);
                try
                {
                    var numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    var startPos = 0;
                    var lastColor = string.Empty;
                    Log.Write(string.Format(
                        "Must process very large text of length {0} characters for team chat message. Will send to QL over {1} lines.",
                        text.Length, numLines), _logClassType, _logPrefix);
                    for (var i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_sst.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                var m = _sst.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
                                lastColor = m[m.Count - 1].Value;
                            }
                            if (multiLine[i - 1].EndsWith("^"))
                            {
                                lastColor = "^";
                            }
                        }
                        if (i == multiLine.Length - 1)
                        {
                            // last iteration; string length cannot be specified
                            multiLine[i] = string.Format("{0}{1}", lastColor, text.Substring(startPos));
                        }
                        else
                        {
                            multiLine[i] = string.Format("{0}{1}", lastColor,
                                text.Substring(startPos, MaxChatlineLength));
                        }

                        // QL drops any server command that exceeds 500 ms threshold
                        await Task.Delay(DefaultCommandDelayMsec);
                        DoSayTeam(multiLine[i]);
                        startPos += MaxChatlineLength;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical("Unable send team chat message text to QL: " + ex.Message,
                        _logClassType, _logPrefix);
                }
            }
            else
            {
                await Task.Delay(DefaultCommandDelayMsec);
                DoSayTeam(text);
            }
        }

        /// <summary>
        /// Sends the 'serverinfo' command to QL.
        /// </summary>
        public async Task QlCmdServerInfo()
        {
            await SendToQlAsync("serverinfo", true);
        }

        /// <summary>
        /// Sends the 'tell player' command to QL. If too much text is received then issue 'tell'
        /// command over multiple lines.
        /// </summary>
        /// <param name="player">The player to whom the message is to be sent.</param>
        /// <param name="text">The text to tell.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdTell(string text, string player)
        {
            // tell command uses the players id
            if (!Helpers.KeyExists(player, _sst.ServerInfo.CurrentPlayers)) return;

            var playerId = _sst.ServerInfo.CurrentPlayers[player].Id;

            // Text to send might be too long, so send over multiple lines. With 'tell' it is 107
            // characters, which is shorter than for chat.
            if ((text.Length) > MaxTellLineLength)
            {
                // ReSharper disable once PossibleLossOfFraction : .5 ensures we always round up to next int, no matter size
                var l = ((text.Length / MaxTellLineLength) + .5);
                var linesRoundUp = Math.Ceiling(l);
                try
                {
                    var numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    var startPos = 0;
                    var lastColor = string.Empty;
                    Log.Write(string.Format(
                        "Must process very large text of length {0} characters for tell message. Will send to QL over {1} lines.",
                        text.Length, numLines), _logClassType, _logPrefix);
                    for (var i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_sst.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                var m = _sst.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
                                lastColor = m[m.Count - 1].Value;
                            }
                            if (multiLine[i - 1].EndsWith("^"))
                            {
                                lastColor = "^";
                            }
                        }
                        if (i == multiLine.Length - 1)
                        {
                            // last iteration; string length cannot be specified
                            multiLine[i] = string.Format("{0}{1}", lastColor, text.Substring(startPos));
                        }
                        else
                        {
                            multiLine[i] = string.Format("{0}{1}", lastColor,
                                text.Substring(startPos, MaxTellLineLength));
                        }

                        // QL drops any server command that exceeds 500 ms threshold
                        await Task.Delay(DefaultCommandDelayMsec);
                        DoTell(playerId, multiLine[i]);
                        startPos += MaxTellLineLength;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical("Unable to send tell message text to QL: " + ex.Message,
                        _logClassType, _logPrefix);
                }
            }
            else
            {
                await Task.Delay(DefaultCommandDelayMsec);
                DoTell(playerId, text);
            }
        }

        /// <summary>
        /// Requests the server address.
        /// </summary>
        public void RequestServerAddress()
        {
            SendToQl("cl_currentserveraddress", false);
            Log.Write("Requesting monitored server's address.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Send a synchronous command, typically for retrieving cvars.
        /// </summary>
        /// <param name="toSend">The command (cvar) to send.</param>
        /// <param name="delay">if set to <c>true</c> send with a delay.</param>
        public void SendToQl(string toSend, bool delay)
        {
            SendQlCommand(toSend, delay);
        }

        /// <summary>
        /// Asynchronously sends the given text to the QL console.
        /// </summary>
        /// <param name="toSend">To text to send.</param>
        /// <param name="delay">
        /// if set to <c>true</c> then sends the text to QL and waits with a slight delay.
        /// </param>
        /// <remarks>
        /// Some commands that return significant amounts of text (i.e. serverinfo) have to have
        /// some time to be received, so a delay is necessary.
        /// </remarks>
        public async Task SendToQlAsync(string toSend, bool delay)
        {
            await Task.Delay(DefaultCommandDelayMsec);
            SendQlCommand(toSend, delay);
        }

        /// <summary>
        /// Asynchronously sends the given text to the QL console after a specified time delay.
        /// </summary>
        /// <param name="toSend">The text to send.</param>
        /// <param name="delay">if set to <c>true</c> [delay].</param>
        /// <param name="runCmdInSeconds">
        /// The number of seconds to wait before sending the given text..
        /// </param>
        /// <remarks>
        /// This is primarily used for the 'players' command since the player info is not
        /// immediately available when a player connects, we wait a certain number of seconds before
        /// requesting it.
        /// </remarks>
        public async Task SendToQlDelayedAsync(string toSend, bool delay, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            SendQlCommand(toSend, delay);
        }

        /// <summary>
        /// Sends the 'say' command to QL.
        /// </summary>
        /// <param name="text">The text.</param>
        private void DoSay(string text)
        {
            SendQlCommand(string.Format("say {0}", text), false);
        }

        /// <summary>
        /// Sends the 'say_team' command to QL.
        /// </summary>
        /// <param name="text">The text.</param>
        private void DoSayTeam(string text)
        {
            SendQlCommand(string.Format("say_team {0}", text), false);
        }

        /// <summary>
        /// Sends the 'tell player' command to QL.
        /// </summary>
        /// <param name="playerId">The player's id.</param>
        /// <param name="text">The text.</param>
        private void DoTell(int playerId, string text)
        {
            SendQlCommand(string.Format("tell {0} {1}", playerId, text), false);
        }

        /// <summary>
        /// Sends the QL command.
        /// </summary>
        /// <param name="toSend">To send.</param>
        /// <param name="delay">if set to <c>true</c> [delay].</param>
        private void SendQlCommand(string toSend, bool delay)
        {
            var iText =
                _sst.QlWindowUtils.GetQuakeLiveConsoleInputArea(_sst.QlWindowUtils.GetQuakeLiveConsoleWindow());
            if (iText == IntPtr.Zero)
            {
                Log.WriteCritical("Unable to get necessary console handle.", _logClassType, _logPrefix);
                return;
            }

            // Set the console's edit box text to what we need to send.
            Win32Api.SendMessage(iText, Win32Api.WM_SETTEXT, IntPtr.Zero, toSend);

            // Simulate the pressing of 'ENTER' key to send message.
            Win32Api.SendMessage(iText, Win32Api.WM_CHAR, new IntPtr(Win32Api.VK_RETURN), IntPtr.Zero);

            // Tiny delay is sometimes necessary with QL commands that send back a lot of info (i.e.
            // players, serverinfo)
            if (delay)
            {
                // Creates a new event handler that will never be set, and then waits the full
                // timeout period
                new ManualResetEvent(false).WaitOne(10);
            }
        }
    }
}
