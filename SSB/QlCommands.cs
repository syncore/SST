using System;
using System.Diagnostics;
using System.Threading;

namespace SSB
{
    /// <summary>
    ///     Class responsible for sending various commands to QL.
    /// </summary>
    public class QlCommands : IQlCommands
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
        ///     Sends the given text to the QL console.
        /// </summary>
        /// <param name="toSend">To text to send.</param>
        /// <param name="delay">if set to <c>true</c> then sends the text to QL and waits with a slight delay.</param>
        /// <remarks>
        ///     Some commands that return significant amounts of text (i.e. serverinfo) have to have some time to be
        ///     received, so a delay is necessary.
        /// </remarks>
        public void SendToQl(string toSend, bool delay)
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
        ///     Sends the 'clear' command to QL.
        /// </summary>
        public void QlCmdClear()
        {
            SendToQl("clear", false);
        }

        /// <summary>
        ///     Sends the 'configstrings' command to QL.
        /// </summary>
        public void QlCmdConfigStrings()
        {
            SendToQl("configstrings", true);
        }

        /// <summary>
        ///     Sends the 'players' command to QL.
        /// </summary>
        public void QlCmdPlayers()
        {
            SendToQl("players", true);
        }

        /// <summary>
        ///     Sends the 'say' command to QL.
        /// </summary>
        /// <param name="text">The text to say.</param>
        public void QlCmdSay(string text)
        {
            SendToQl(string.Format("say {0}", text), false);
        }

        /// <summary>
        ///     Sends the 'serverinfo' command to QL.
        /// </summary>
        public void QlCmdServerInfo()
        {
            SendToQl("serverinfo", true);
        }
    }
}