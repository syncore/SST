using System;
using System.Windows.Forms;
using SSB.Core;
using SSB.Ui;
using SSB.Util;

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
            
            // Create data directory
            if (!Filepaths.CreateDataDirectory())
            {
                MessageBox.Show(
                    @"Could not create data directory! Make sure that your SSB directory is not read-only. Exiting.",
                    @"Fatal error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // Main class
            _ssb = new SynServerBot();

            // Load the GUI
            Application.Run(new UserInterface(_ssb));
        }
    }
}