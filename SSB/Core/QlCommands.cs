using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for sending various commands to QL.
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
            IntPtr consoleWindow = _ssb.QlWindowUtils.GetQuakeLiveConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                IntPtr child = Win32Api.FindWindowEx(consoleWindow, IntPtr.Zero, "Button", "clear");
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
            int id = _ssb.ServerEventProcessor.GetPlayerId(player);
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
            int id = _ssb.ServerEventProcessor.GetPlayerId(player);
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
            // lower resolutions based on guestimate. however, QL actually supports
            // sending up to 135 characters at a time
            if ((text.Length) > MaxChatlineLength)
            {
                // .5 ensures we always round up to next int, no matter size
                // ReSharper disable once PossibleLossOfFraction
                double l = ((text.Length/MaxChatlineLength) + .5);
                double linesRoundUp = Math.Ceiling(l);
                try
                {
                    int numLines = Convert.ToInt32(linesRoundUp);
                    var multiLine = new string[numLines];
                    int startPos = 0;
                    string lastColor = string.Empty;
                    Debug.WriteLine("Received very large text of length {0}. Will send to QL over {1} lines.",
                        text.Length, numLines);
                    for (int i = 0; i <= multiLine.Length - 1; i++)
                    {
                        if (i != 0)
                        {
                            // Keep the text colors consistent across multiple lines of text.
                            if (_ssb.Parser.UtilCaretColor.IsMatch(multiLine[i - 1]))
                            {
                                MatchCollection m = _ssb.Parser.UtilCaretColor.Matches(multiLine[i - 1]);
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
                        await Task.Delay(DefaultCommandDelayMsec*2);
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
        ///     Sends the 'serverinfo' command to QL.
        /// </summary>
        public async Task QlCmdServerInfo()
        {
            await SendToQlAsync("serverinfo", true);
        }

        /// <summary>
        ///     Send a synchronous command, typically for retrieving cvars.
        /// </summary>
        /// <param name="toSend">The command (cvar) to send.</param>
        /// <param name="delay">if set to <c>true</c> send with a delay.</param>
        public void SendCvarReq(string toSend, bool delay)
        {
            SendQlCommand(toSend, delay);
        }

        /// <summary>
        ///     Sends the given text to the QL console.
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
        ///     Sends the 'say' command to QL.
        /// </summary>
        /// <param name="text">The text.</param>
        private void DoSay(string text)
        {
            SendQlCommand(string.Format("say {0}", text), false);
        }

        /// <summary>
        ///     Sends the QL command.
        /// </summary>
        /// <param name="toSend">To send.</param>
        /// <param name="delay">if set to <c>true</c> [delay].</param>
        private void SendQlCommand(string toSend, bool delay)
        {
            IntPtr iText =
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

            // Tiny delay Sometimes necessary with QL commands that send back a lot of info (i.e. players, serverinfo)
            if (delay)
            {
                // Creates a new event handler that will never be set, and then waits the full timeout period
                new ManualResetEvent(false).WaitOne(10);
            }
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
        private async Task SendToQlDelayedAsync(string toSend, bool delay, int runCmdInSeconds)
        {
            await Task.Delay(runCmdInSeconds*1000);
            Action<string, bool> sendQl = SendQlCommand;
            sendQl(toSend, delay);
        }
    }
}