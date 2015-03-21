using System;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;
using SSB.Core;
using SSB.Ui;
using SSB.Util;
using Timer = System.Timers.Timer;

namespace SSB
{
    internal static class EntryPoint
    {
        private static SynServerBot _ssb;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create necessary directories
            Filepaths.CreateDataDirectory();
            // Main class
            _ssb = new SynServerBot();
            // Load the GUI
            Application.Run(new UserInterface(_ssb));
        }
    }
}