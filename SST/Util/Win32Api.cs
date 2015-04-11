using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SST.Util
{
    /// <summary>
    ///     Class that handles various functions from the Windows API
    /// </summary>
    internal static class Win32Api
    {
        public const int BN_CLICKED = 245;

        public const int EM_GETSEL = 0x00B0;

        public const int EM_SETSEL = 0x00B1;

        public const int VK_RETURN = 0x0D;

        public const int WM_CHAR = 0x0102;

        public const int WM_SETTEXT = 0x000C;

        public const int WM_GETTEXT = 0xD;

        public const int WM_GETTEXTLENGTH = 0x000E;

        public const int WM_NCLBUTTONDOWN = 0x00A1;

        public const int HT_CAPTION = 2;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        // Get text of console window
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        // Get length of console window text
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // Send text to console edit control; note don't set CharSet.Auto since using MarshalAs
        // http://www.pinvoke.net/default.aspx/user32.sendmessage
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);

        // Get total selection length (for calculating when an auto-clear needs to be sent so buffer isn't full)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, out int wParam, out int lParam);

        // Focus window (also, in testing environment, if viewlog "1" then you will have annoying
        //loss of window focus while playing -- doesn't apply to real game environment where viewlog is cheat protected)
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        // Moving the GUI window, which is a borderless form (FormBorderStyle 'None')
        // http://stackoverflow.com/questions/1592876/make-a-borderless-form-movable
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        // Moving the GUI window, which is a borderless form (FormBorderStyle 'None')
        // http://stackoverflow.com/questions/1592876/make-a-borderless-form-movable
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
    }
}