﻿using System.Windows.Forms;
using SSB.Util;

namespace SSB.Ui
{
    /// <summary>
    ///     Class that holds references to some of the selected UI controls in
    ///     UserInterface.cs, allowing access from other classes.
    /// </summary>
    public class AppWideUiControls
    {
        /// <summary>
        ///     Gets or sets the SSB UI activity log text box.
        /// </summary>
        /// <value>
        ///     The SSB UI activity log text box.
        /// </value>
        public TextBox LogConsoleTextBox { get; set; }

        /// <summary>
        ///     Gets or sets the monitoring status bar label.
        /// </summary>
        /// <value>
        ///     The monitoring status bar label.
        /// </value>
        public Label MonitoringLabel { get; set; }

        /// <summary>
        ///     Gets or sets the 'start monitoring' button.
        /// </summary>
        /// <value>
        ///     The 'start monitoring' button.
        /// </value>
        public Button StartMonitoringButton { get; set; }

        /// <summary>
        ///     Gets or sets the 'stop monitoring' button.
        /// </summary>
        /// <value>
        ///     The 'stop monitoring' button.
        /// </value>
        public Button StopMonitoringButton { get; set; }

        /// <summary>
        ///     Updates the application wide UI controls.
        /// </summary>
        /// <param name="isMonitoring">
        ///     if set to <c>true</c> then server
        ///     monitoring is active. if set to <c>false</c> then
        ///     server monitoring is inactive.
        /// </param>
        /// <param name="serverId">The server identifier.</param>
        public void UpdateAppWideControls(bool isMonitoring, string serverId)
        {
            StartMonitoringButton.InvokeIfRequired(c => { c.Enabled = !isMonitoring; });
            StopMonitoringButton.InvokeIfRequired(c => { c.Enabled = isMonitoring; });

            MonitoringLabel.InvokeIfRequired(c =>
            {
                c.Text = string.Format("{0}", (isMonitoring)
                    ? string.Format(
                        "Monitoring server at http://www.quakelive.com/#!join/{0}",
                        (string.IsNullOrEmpty(serverId)
                            ? "..."
                            : serverId))
                    : "Not monitoring a server");
            });
        }
    }
}