using System;
using System.Threading;
using System.Windows.Forms;
using SST.Core;
using SST.Ui;
using SST.Util;

namespace SST
{
    internal static class EntryPoint
    {
        private static readonly Mutex Mutex = new Mutex(true, "{8f2334f9-3466-4aa0-bb24-09cfe88a02f8}");
        private static SynServerTool _sst;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            if (Mutex.WaitOne(TimeSpan.Zero, true))
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
            else
            {
                MessageBox.Show(
                        @"SST is already running. Only one copy of SST can run at a time!",
                        @"SST is already running",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}