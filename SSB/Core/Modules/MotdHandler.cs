using System;
using System.Diagnostics;
using System.Timers;

namespace SSB.Core.Modules
{
    /// <summary>
    ///     Class responsible for handling the message of the day set by the motd command.
    /// </summary>
    public class MotdHandler
    {
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MotdHandler" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public MotdHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            MotdTimer = new Timer();
        }

        /// <summary>
        ///     Gets or sets the message to repeat.
        /// </summary>
        /// <value>
        ///     The message to repeat.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        ///     Gets the motd timer.
        /// </summary>
        /// <value>
        ///     The motd timer.
        /// </value>
        public Timer MotdTimer { get; private set; }

        /// <summary>
        ///     Gets or sets the repeat time in minutes.
        /// </summary>
        /// <value>
        ///     The repeat time in minutes.
        /// </value>
        public uint RepeatInterval { get; set; }

        /// <summary>
        ///     Starts the motd timer.
        /// </summary>
        public void StartMotdTimer()
        {
            MotdTimer.Interval = (RepeatInterval * 60000);
            MotdTimer.AutoReset = true;
            MotdTimer.Elapsed += MotdTimerElapsed;
            MotdTimer.Enabled = true;
            Debug.WriteLine("MOTD timer started. MOTD is enabled.");
        }

        /// <summary>
        ///     Stops the motd timer.
        /// </summary>
        public void StopMotdTimer()
        {
            MotdTimer.Enabled = false;
            Debug.WriteLine("MOTD timer stopped. MOTD is disabled.");
        }

        /// <summary>
        ///     Method that is executed when the MOTD timer elapses.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private async void MotdTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^3**^7 {0}^3 **", Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Caught exception in MotdTimerElapsed asynchronous void (event handler) method: " + ex.Message);
            }
        }
    }
}