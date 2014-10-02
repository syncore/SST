using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SSB
{
    public class QlWindowUtils
    {
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

        public IntPtr GetQuakeLiveConsoleWindow()
        {
            return Win32Api.FindWindow("Q3 WinConsole", null);
        }

        public IntPtr GetQuakeLiveConsoleInputArea(IntPtr consoleWindow)
        {
            var inputArea = IntPtr.Zero;
            if (consoleWindow != IntPtr.Zero)
            {
                inputArea = Win32Api.FindWindowEx(consoleWindow, IntPtr.Zero, "Edit", null);
            }
            return inputArea;
        }

        public IntPtr GetQuakeLiveConsoleTextArea(IntPtr consoleWindow, IntPtr consoleInputArea)
        {
            var consoleTextArea = IntPtr.Zero;
            if (consoleWindow != IntPtr.Zero)
            {
                consoleTextArea = Win32Api.FindWindowEx(consoleWindow, consoleInputArea, "Edit", null);
            }
            return consoleTextArea;
        }
    }


}
