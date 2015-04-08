using System;
using System.Reflection;
using System.Timers;
using SSB.Util;

namespace SSB.Core.Modules
{
    /// <summary>
    ///     Class responsible for handling the message of the day set by the motd command.
    /// </summary>
    public class MotdHandler
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:MOTD]";
        private readonly Timer _motdTimer;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MotdHandler" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public MotdHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            _motdTimer = new Timer();
        }

        /// <summary>
        ///     Gets or sets the message to repeat.
        /// </summary>
        /// <value>
        ///     The message to repeat.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        ///     Gets or sets the repeat time in minutes.
        /// </summary>
        /// <value>
        ///     The repeat time in minutes.
        /// </value>
        public int RepeatInterval { get; set; }

        /// <summary>
        /// Restarts the motd timer.
        /// </summary>
        public void RestartMotdTimer()
        {
            StopMotdTimer();
            StartMotdTimer();
        }

        /// <summary>
        ///     Starts the motd timer.
        /// </summary>
        public void StartMotdTimer()
        {
            _motdTimer.Interval = (RepeatInterval * 60000);
            _motdTimer.AutoReset = true;
            _motdTimer.Elapsed += _motdTimerElapsed;
            _motdTimer.Enabled = true;

            Log.Write("Started message of the day timer.",
                _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Stops the motd timer.
        /// </summary>
        public void StopMotdTimer()
        {
            _motdTimer.Elapsed -= _motdTimerElapsed;
            _motdTimer.Enabled = false;

            Log.Write("Stopped message of the day timer.",
                _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Method that is executed when the MOTD timer elapses.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private async void _motdTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // Kill it if we're no longer monitoring
            if (!_ssb.IsMonitoringServer)
            {
                StopMotdTimer();
                return;
            }

            try
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^7{0}", Message));
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}