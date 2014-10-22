using System;
using System.Diagnostics;
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
        /// <remarks>Our version takes the player name as an argument and converts it into the
        /// playerID required for the actual QL command.</remarks>
        public async Task CustCmdKickban(string player)
        {
            string id = _ssb.ServerEventProcessor.GetPlayerId(player).Result;
            if (!String.IsNullOrEmpty(id))
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
        /// Sends the 'put' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="team">The team.</param>
        /// <remarks>
        /// Our version takes the player name as an argument and converts it into the playerID required for the actual QL command.
        /// </remarks>
        public async Task CustCmdPutPlayer(string player, Team team)
        {
            string id = _ssb.ServerEventProcessor.GetPlayerId(player).Result;
            if (!String.IsNullOrEmpty(id))
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
                Debug.WriteLine(string.Format("Unable to force join player {0} because ID could not be retrieved.",
                    player));
            }
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
        ///     Sends the 'say' command to QL.
        /// </summary>
        /// <param name="text">The text to say.</param>
        /// <remarks>This requires a delay, otherwise the command is not sent.</remarks>
        public async Task QlCmdSay(string text)
        {
            await Task.Delay(500);
            Action<string> say = DoSay;
            say(text);
        }

        /// <summary>
        ///     Sends the 'serverinfo' command to QL.
        /// </summary>
        public async Task QlCmdServerInfo()
        {
            await SendToQlAsync("serverinfo", true);
        }

        /// <summary>
        /// Send a synchronous command, typically for retrieving cvars.
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
            await Task.Delay(500);
            Action<string, bool> sendQl = SendQlCommand;
            sendQl(toSend, delay);
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
        /// Sends the QL command.
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
            foreach (char c in toSend)
            {
                Win32Api.SendMessage(iText, Win32Api.WM_CHAR, new IntPtr(c), IntPtr.Zero);
            }

            // Simulate the pressing of 'ENTER' key to send message.
            Win32Api.SendMessage(iText, Win32Api.WM_CHAR, new IntPtr(Win32Api.VK_RETURN), IntPtr.Zero);

            // Sometimes necessary with QL commands that send back a lot of info (i.e. players, serverinfo)
            if (delay)
            {
                //Thread.Sleep(1);
                // Creates a new event handler that will never be set, and then waits the full timeout period
                new ManualResetEvent(false).WaitOne(10);
            }
        }
    }
}