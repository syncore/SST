using System;
using System.Windows.Forms;
using SST.Core;
using SST.Ui;
using SST.Util;

namespace SST
{
    internal static class EntryPoint
    {
        private static SynServerTool _sst;

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
                    @"Could not create data directory! Make sure that your SST directory is not read-only. Exiting.",
                    @"Fatal error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            // Main class
            _sst = new SynServerTool();

            // Load the GUI
            Application.Run(new UserInterface(_sst));
        }
    }
}