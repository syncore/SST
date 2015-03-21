using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for sending various commands to the QL game instance.
    /// </summary>
    public class QlCommands
    {
        private const int DefaultCommandDelayMsec = 500;
        private const int MaxChatlineLength = 134;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="QlCommands" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public QlCommands(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        public async Task CheckCmdStatus()
        {
            await SendToQlAsync("cmd", false);
            //QlCmdClear();
            Debug.WriteLine("Checking status of server connection by sending cmd...");
        }

        /// <summary>
        /// Checks the main menu (ui_mainmenu) status.
        /// </summary>
        public async Task CheckMainMenuStatus()
        {
            await SendToQlAsync("ui_mainmenu", false);
            //QlCmdClear();
            Debug.WriteLine("Checking status of main menu by sending ui_mainmenu...");
        }

        /// <summary>
        ///     Clear both the QL WinConsole and the in-game console.
        /// </summary>
        public void ClearBothQlConsoles()
        {
            // Windows console window
            ClearQlWinConsole();
            // In-game console window
            QlCmdClear();
        }

        /// <summary>
        ///     Clears the Ql windows console.
        /// </summary>
        public void ClearQlWinConsole()
        {
            var consoleWindow = _ssb.QlWindowUtils.GetQuakeLiveConsoleWindow();
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
                Debug.WriteLine("Unable to find 'clear' button.");
            }
        }

        /// <summary>
        ///     Sends the 'kickban' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <remarks>
        ///     Our version takes the player name as an argument and converts it into the
        ///     playerID required for the actual QL command.
        /// </remarks>
        public async Task CustCmdKickban(string player)
        {
            var id = _ssb.ServerEventProcessor.GetPlayerId(player.ToLowerInvariant());
            if (id != -1)
            {
                await SendToQlAsync(string.Format("kickban {0}", id), false);
            }
            else
            {
                Debug.WriteLine(string.Format("Unable to kick player {0} because ID could not be retrieved.",
                    player));
            }
        }

        /// <summary>
        ///     Sends the 'put' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        /// <remarks>
        ///     Our version takes the player name as an argument and converts it into the playerID required for the actual QL
        ///     command.
        /// </remarks>
        public async Task CustCmdPutPlayer(string player, Team team)
        {
            var id = _ssb.ServerEventProcessor.GetPlayerId(player);
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
            }
            else
            {
                Debug.WriteLine(
                    string.Format("Unable to force join player {0} because ID could not be retrieved.",
                        player));
            }
        }

        /// <summary>
        ///     Sends the /put command after a given delay in seconds.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        /// <param name="runCmdInSeconds">The time to wait, in seconds, before sending the 'put' command.</param>
        /// <returns></returns>
        public async Task CustCmdPutPlayerDelayed(string player, Team team, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            await CustCmdPutPlayer(player, team);
        }

        /// <summary>
        ///     Disables in-game console printing.
        ///     <remarks>
        ///         With this set (con_noprint 1) no text will be shown in the in-game console. This
        ///         is the preferred mode when developer mode is enabled. Note, text will ALWAYS be appended
        ///         to the winconsole, regardless of this setting.
        ///     </remarks>
        /// </summary>
        public void DisableConsolePrinting()
        {
            SendQlCommand("con_noprint 1", false);
            QlCmdClear();
            Debug.WriteLine("Disabling in-game console printing...");
        }

        /// <summary>
        ///     Disables the developer mode.
        /// </summary>
        public void DisableDeveloperMode()
        {
            SendQlCommand("developer 0", false);
            QlCmdClear();
            Debug.WriteLine("Disabling developer mode...");
        }

        /// <summary>
        ///     Enables in-game console printing.
        /// </summary>
        /// <remarks>
        ///     With this set (con_noprint 0), text will be shown on the in-game console.
        ///     Note: this might be annoying when attempting to play with developer mode on since
        ///     there will be multiple 'tinfo' messages.
        /// </remarks>
        public void EnableConsolePrinting()
        {
            SendQlCommand("con_noprint 0", false);
            QlCmdClear();
            Debug.WriteLine("Enabling in-game console printing...");
        }

        /// <summary>
        ///     Enables the developer mode.
        /// </summary>
        public void EnableDeveloperMode()
        {
            SendQlCommand("developer 1", false);
            QlCmdClear();
            Debug.WriteLine("Enabling developer mode...");
        }

        /// <summary>
        ///     Sends the 'clear' command to QL.
        /// </summary>
        public void QlCmdClear()
        {
            SendQlCommand("clear", false);
        }

        /// <summary>
        ///     Sends the 'configstrings' command to QL.
        /// </summary>
        public async Task QlCmdConfigStrings()
        {
            await SendToQlAsync("configstrings", true);
            // ...
            QlCmdClear();
        }

        /// <summary>
        ///     Sends the /say command after a given delay in seconds.
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// <param name="runCmdInSeconds">The time to wait, in seconds, before sending the 'say' command.</param>
        public async Task QlCmdDelayedSay(string text, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            await QlCmdSay(text);
        }

        /// <summary>
        ///     Sends the /tell command to a player after a given delay in seconds.
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// <param name="player">The player.</param>
        /// <param name="runCmdInSeconds">The time to wait, in seconds, before sending the 'tell' command.</param>
        public async Task QlCmdDelayedTell(string text, string player, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            await QlCmdTell(text, player);
        }

        /// <summary>
        ///     Sends the 'players' command to QL.
        /// </summary>
        public async Task QlCmdPlayers()
        {
            await SendToQlAsync("players", true);
        }

        /// <summary>
        ///     Sends the 'players' command to QL when a player connects.
        /// </summary>
        /// <remarks>This command will execute after a specified delay in seconds.</remarks>
        public async Task QlCmdPlayersOnConnect()
        {
            await SendToQlDelayedAsync("players", true, 15);
        }

        /// <summary>
        ///     Sends the 'say' command to QL. If too much text is received then issue 'say' command
        ///     over multiple lines.
        /// </summary>
        /// <param name="text">The text to say.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdSay(string text)
        {
            // Text to send might be too long, so send over multiple lines.
            // Line length of between 98 & 115 chars is probably optimal for
            // lower resolutions based on guestimate. However, QL actually supports
            // sending up to 135 characters at a time.
            if ((text.Length) > MaxChatlineLength)
            {
                // .5 ensures we always round up to next int, no matter size
                // ReSharper disable once PossibleLossOfFraction
                var l = ((text.Length / MaxChatlineLength) + .5);
                var linesRoundUp = Math.Ceiling(l);
                try
                {
                    var numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    var startPos = 0;
                    var lastColor = string.Empty;
                    Debug.WriteLine("Received very large text of length {0}. Will send to QL over {1} lines.",
                        text.Length, numLines);
                    for (var i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_ssb.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                var m = _ssb.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
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

                        // Double the usual delay when sending multiple lines.
                        await Task.Delay(DefaultCommandDelayMsec * 2);
                        Action<string> say = DoSay;
                        say(multiLine[i]);
                        startPos += MaxChatlineLength;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to send text to QL: " + ex.Message);
                }
            }
            else
            {
                await Task.Delay(DefaultCommandDelayMsec);
                Action<string> say = DoSay;
                say(text);
            }
        }

        /// <summary>
        ///     Sends the 'say_team' command to QL. If too much text is received then issue 'say_team' command
        ///     over multiple lines.
        /// </summary>
        /// <param name="text">The text to say.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdSayTeam(string text)
        {
            // Text to send might be too long, so send over multiple lines.
            // Line length of between 98 & 115 chars is probably optimal for
            // lower resolutions based on guestimate. However, QL actually supports
            // sending up to 135 characters at a time.
            if ((text.Length) > MaxChatlineLength)
            {
                // .5 ensures we always round up to next int, no matter size
                // ReSharper disable once PossibleLossOfFraction
                var l = ((text.Length / MaxChatlineLength) + .5);
                var linesRoundUp = Math.Ceiling(l);
                try
                {
                    var numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    var startPos = 0;
                    var lastColor = string.Empty;
                    Debug.WriteLine("Received very large text of length {0}. Will send to QL over {1} lines.",
                        text.Length, numLines);
                    for (var i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_ssb.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                var m = _ssb.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
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

                        // Double the usual delay when sending multiple lines.
                        await Task.Delay(DefaultCommandDelayMsec * 2);
                        Action<string> sayTeam = DoSayTeam;
                        sayTeam(multiLine[i]);
                        startPos += MaxChatlineLength;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to send text to QL: " + ex.Message);
                }
            }
            else
            {
                await Task.Delay(DefaultCommandDelayMsec);
                Action<string> sayTeam = DoSayTeam;
                sayTeam(text);
            }
        }

        /// <summary>
        ///     Sends the 'serverinfo' command to QL.
        /// </summary>
        public async Task QlCmdServerInfo()
        {
            await SendToQlAsync("serverinfo", true);
        }

        /// <summary>
        ///     Sends the 'tell player' command to QL. If too much text is received then issue 'tell' command
        ///     over multiple lines.
        /// </summary>
        /// <param name="player">The player to whom the message is to be sent.</param>
        /// <param name="text">The text to tell.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdTell(string text, string player)
        {
            // tell command uses the players id
            if (!Helpers.KeyExists(player, _ssb.ServerInfo.CurrentPlayers)) return;

            var playerId = _ssb.ServerInfo.CurrentPlayers[player].Id;

            // Text to send might be too long, so send over multiple lines.
            // Line length of between 98 & 115 chars is probably optimal for
            // lower resolutions based on guestimate. However, QL actually supports
            // sending up to 135 characters at a time.
            if ((text.Length) > MaxChatlineLength)
            {
                // .5 ensures we always round up to next int, no matter size
                // ReSharper disable once PossibleLossOfFraction
                var l = ((text.Length / MaxChatlineLength) + .5);
                var linesRoundUp = Math.Ceiling(l);
                try
                {
                    var numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    var startPos = 0;
                    var lastColor = string.Empty;
                    Debug.WriteLine("Received very large text of length {0}. Will send to QL over {1} lines.",
                        text.Length, numLines);
                    for (var i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_ssb.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                var m = _ssb.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
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

                        // Double the usual delay when sending multiple lines.
                        await Task.Delay(DefaultCommandDelayMsec * 2);
                        Action<int, string> tell = DoTell;
                        tell(playerId, multiLine[i]);
                        startPos += MaxChatlineLength;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to send text to QL: " + ex.Message);
                }
            }
            else
            {
                await Task.Delay(DefaultCommandDelayMsec);
                Action<int, string> tell = DoTell;
                tell(playerId, text);
            }
        }

        /// <summary>
        ///     Send a synchronous command, typically for retrieving cvars.
        /// </summary>
        /// <param name="toSend">The command (cvar) to send.</param>
        /// <param name="delay">if set to <c>true</c> send with a delay.</param>
        public void SendToQl(string toSend, bool delay)
        {
            SendQlCommand(toSend, delay);
        }

        /// <summary>
        ///     Asynchronously sends the given text to the QL console.
        /// </summary>
        /// <param name="toSend">To text to send.</param>
        /// <param name="delay">if set to <c>true</c> then sends the text to QL and waits with a slight delay.</param>
        /// <remarks>
        ///     Some commands that return significant amounts of text (i.e. serverinfo) have to have some time to be
        ///     received, so a delay is necessary.
        /// </remarks>
        public async Task SendToQlAsync(string toSend, bool delay)
        {
            await Task.Delay(DefaultCommandDelayMsec);
            Action<string, bool> sendQl = SendQlCommand;
            sendQl(toSend, delay);
        }

        /// <summary>
        ///     Asynchronously sends the given text to the QL console after a specified time delay.
        /// </summary>
        /// <param name="toSend">The text to send.</param>
        /// <param name="delay">if set to <c>true</c> [delay].</param>
        /// <param name="runCmdInSeconds">The number of seconds to wait before sending the given text..</param>
        /// <remarks>
        ///     This is primarily used for the 'players' command since the player info is not immediately available
        ///     when a player connects, we wait a certain number of seconds before requesting it.
        /// </remarks>
        public async Task SendToQlDelayedAsync(string toSend, bool delay, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds * 1000);
            Action<string, bool> sendQl = SendQlCommand;
            sendQl(toSend, delay);
        }

        /// <summary>
        ///     Sends the 'say' command to QL.
        /// </summary>
        /// <param name="text">The text.</param>
        private void DoSay(string text)
        {
            SendQlCommand(string.Format("say {0}", text), false);
        }

        /// <summary>
        ///     Sends the 'say_team' command to QL.
        /// </summary>
        /// <param name="text">The text.</param>
        private void DoSayTeam(string text)
        {
            SendQlCommand(string.Format("say_team {0}", text), false);
        }

        /// <summary>
        ///     Sends the 'tell player' command to QL.
        /// </summary>
        /// <param name="playerId">The player's id.</param>
        /// <param name="text">The text.</param>
        private void DoTell(int playerId, string text)
        {
            SendQlCommand(string.Format("tell {0} {1}", playerId, text), false);
        }

        /// <summary>
        ///     Sends the QL command.
        /// </summary>
        /// <param name="toSend">To send.</param>
        /// <param name="delay">if set to <c>true</c> [delay].</param>
        private void SendQlCommand(string toSend, bool delay)
        {
            var iText =
                _ssb.QlWindowUtils.GetQuakeLiveConsoleInputArea(_ssb.QlWindowUtils.GetQuakeLiveConsoleWindow());
            if (iText == IntPtr.Zero)
            {
                Debug.WriteLine("Couldn't find Quake Live input text area");
                return;
            }

            // Set the console's edit box text to what we need to send.
            Win32Api.SendMessage(iText, Win32Api.WM_SETTEXT, IntPtr.Zero, toSend);

            // Simulate the pressing of 'ENTER' key to send message.
            Win32Api.SendMessage(iText, Win32Api.WM_CHAR, new IntPtr(Win32Api.VK_RETURN), IntPtr.Zero);

            // Tiny delay is sometimes necessary with QL commands that send back a lot of info (i.e. players, serverinfo)
            if (delay)
            {
                // Creates a new event handler that will never be set, and then waits the full timeout period
                new ManualResetEvent(false).WaitOne(10);
            }
        }
    }
}