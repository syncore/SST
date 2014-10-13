using System;
using System.Diagnostics;

namespace SSB.Util
{
    /// <summary>
    /// Class responsible for various QL window functions.
    /// </summary>
    public class QlWindowUtils
    {
        /// <summary>
        /// Gets the QL window handle.
        /// </summary>
        /// <value>
        /// The QL window handle.
        /// </value>
        public static IntPtr QlWindowHandle
        {
            get
            {
                foreach (Process proc in Process.GetProcessesByName("quakelive")) // standalone
                    return proc.MainWindowHandle;
                foreach (Process proc in Process.GetProcessesByName("quakelive_steam")) // steam
                    return proc.MainWindowHandle;
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the quake live console input area.
        /// </summary>
        /// <param name="consoleWindow">The console window.</param>
        /// <returns>A handle/pointer to the QL console input area.</returns>
        public IntPtr GetQuakeLiveConsoleInputArea(IntPtr consoleWindow)
        {
            var inputArea = IntPtr.Zero;
            if (consoleWindow != IntPtr.Zero)
            {
                inputArea = Win32Api.FindWindowEx(consoleWindow, IntPtr.Zero, "Edit", null);
            }
            return inputArea;
        }

        /// <summary>
        /// Gets the quake live console text area.
        /// </summary>
        /// <param name="consoleWindow">The console window.</param>
        /// <param name="consoleInputArea">The console input area.</param>
        /// <returns>A handle/pointer to the QL console text area.</returns>
        public IntPtr GetQuakeLiveConsoleTextArea(IntPtr consoleWindow, IntPtr consoleInputArea)
        {
            var consoleTextArea = IntPtr.Zero;
            if (consoleWindow != IntPtr.Zero)
            {
                consoleTextArea = Win32Api.FindWindowEx(consoleWindow, consoleInputArea, "Edit", null);
            }
            return consoleTextArea;
        }

        /// <summary>
        /// Gets the quake live console window.
        /// </summary>
        /// <returns>A handle/pointer to the QL console window.</returns>
        public IntPtr GetQuakeLiveConsoleWindow()
        {
            return Win32Api.FindWindow("Q3 WinConsole", null);
        }

        /// <summary>
        /// Determines whether the QL window exists or not.
        /// </summary>
        /// <returns><c>true</c>if the QL window exists, otherwise <c>false</c>.</returns>
        public bool QuakeLiveConsoleWindowExists()
        {
            bool found = false;
            var consoleWindow = GetQuakeLiveConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                // Got window, now find console edit and text area
                var inAr = GetQuakeLiveConsoleInputArea(consoleWindow);
                var ctAr = GetQuakeLiveConsoleTextArea(consoleWindow, inAr);
                if ((inAr != IntPtr.Zero) && (ctAr != IntPtr.Zero))
                {
                    found = true;
                }
            }
            return found;
        }
    }
}