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
        private static bool _qlIsRunning;
        private static SynServerBot _ssb;
        private static Timer _qlProcessDetectionTimer;

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

            var qlw = new QlWindowUtils();
            if (qlw.QuakeLiveConsoleWindowExists())
            {
                _qlIsRunning = false;
                _qlProcessDetectionTimer = new Timer(15000);
                _qlProcessDetectionTimer.Elapsed += QlProcessDetectionTimerOnElapsed;
                _qlProcessDetectionTimer.Enabled = true;
                Application.Run(new Gui(_ssb));
            }
            else
            {
                MessageBox.Show("Unable to locate Quake Live window! Exiting...", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        /// <summary>
        ///     Method that runs when the QL Process Detection Timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private static void QlProcessDetectionTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            bool active = QlWindowUtils.QlWindowHandle != IntPtr.Zero;
            if (_qlIsRunning && !active)
            {
                // QuakeLive not found, quit SSB.
                //Application.Exit();
                if (_ssb.IsReadingConsole)
                {
                    Debug.WriteLine("Quake Live not found...Stopping console read thread.");
                    _ssb.StopConsoleReadThread();
                }
            }
            else if (active)
            {
                _qlIsRunning = true;
                if (!_ssb.IsReadingConsole)
                {
                    _ssb.StartConsoleReadThread();
                }
            }
        }
    }
}