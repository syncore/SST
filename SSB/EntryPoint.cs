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

            // Create log directory
            Filepaths.CreateLogDirectory();
            
            // Set up logging
            Log.Configure();
            
            // Create data directory
            Filepaths.CreateDataDirectory();
            
            // Main class
            _ssb = new SynServerBot();
            
            // Load the GUI
            Application.Run(new UserInterface(_ssb));
        }
    }
}