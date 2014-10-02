using System;
using System.Windows.Forms;

namespace SSB
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Generate the regex assembly (.dll)
            var compiledRegex = new CompileRegex();
            compiledRegex.GenerateAssembly();

            // Main class
            var ssb = new SynServerBot();

            var qlw = new QlWindowUtils();
            if (qlw.QuakeLiveConsoleWindowExists())
            {
                Application.Run(new Gui(ssb));
            }
            else
            {
                MessageBox.Show("Unable to locate Quake Live window! Exiting...", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }
}