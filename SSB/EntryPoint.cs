using System;
using System.Timers;
using System.Windows.Forms;

namespace SSB
{
    internal static class EntryPoint
    {
        private static bool _qlIsRunning;
        private static System.Timers.Timer _qlProcessDetectionTimer;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Main class
            var ssb = new SynServerBot();

            var qlw = new QlWindowUtils();
            if (qlw.QuakeLiveConsoleWindowExists())
            {
                _qlIsRunning = false;
                _qlProcessDetectionTimer = new System.Timers.Timer(5500);
                _qlProcessDetectionTimer.Elapsed += QlProcessDetectionTimerOnElapsed;
                _qlProcessDetectionTimer.Enabled = true;
                Application.Run(new Gui(ssb));
            }
            else
            {
                MessageBox.Show("Unable to locate Quake Live window! Exiting...", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        /// <summary>
        /// Method that runs when the QL Process Detection Timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private static void QlProcessDetectionTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            bool active = QlWindowUtils.QlWindowHandle != IntPtr.Zero;
            if (_qlIsRunning && !active)
            {
                // QuakeLive not found, quit SSB.
                Application.Exit();
            }
            else if (active)
            {
                _qlIsRunning = true;
            }
        }
    }
}