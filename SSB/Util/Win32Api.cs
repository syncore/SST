using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SSB.Util
{
    /// <summary>
    ///     Class that handles various functions from the Windows API
    /// </summary>
    internal static class Win32Api
    {
        public const int BN_CLICKED = 245;

        public const int EM_GETSEL = 0x00B0;
        
        public const int VK_RETURN = 0x0D;

        public const int WM_CHAR = 0x0102;

        public const int WM_GETTEXT = 0xD;

        public const int WM_GETTEXTLENGTH = 0x000E;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        // get text
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        // length
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // get total selection length (for calculating when an auto-clear needs to be sent so buffer isn't full)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, out int wParam, out int lParam);
        
        // focus window
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
    }
}