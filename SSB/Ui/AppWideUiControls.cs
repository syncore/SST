using System;
using System.Windows.Forms;

namespace SSB.Ui
{
    /// <summary>
    ///     Class that holds references to some of the selected UI controls in UserInterface.cs, allowing us to access them from other
    ///     classes.
    /// </summary>
    public class AppWideUiControls
    {
        private delegate void SetStartMonitoringBtnStatus(bool isMonitoring);

        private delegate void SetStatusBarText(string text);

        private delegate void SetStopMonitoringBtnStatus(bool isMonitoring);

        /// <summary>
        ///     Gets or sets the SSB UI activity log text box.
        /// </summary>
        /// <value>
        ///     The SSB UI activity log text box.
        /// </value>
        public TextBox LogConsoleTextBox { get; set; }

        /// <summary>
        /// Gets or sets the 'start monitoring' button.
        /// </summary>
        /// <value>
        /// The 'start monitoring' button.
        /// </value>
        public Button StartMonitoringButton { get; set; }

        /// <summary>
        /// Gets or sets the status bar label.
        /// </summary>
        /// <value>
        /// The status bar label.
        /// </value>
        public ToolStripStatusLabel StatusBar { get; set; }

        /// <summary>
        /// Gets or sets the 'stop monitoring' button.
        /// </summary>
        /// <value>
        /// The 'stop monitoring' button.
        /// </value>
        public Button StopMonitoringButton { get; set; }

        /// <summary>
        /// Updates the application wide UI controls.
        /// </summary>
        /// <param name="isMonitoring">if set to <c>true</c> then server
        /// monitoring is active. if set to <c>false</c> then
        /// server monitoring is inactive.</param>
        /// <param name="serverId">The server identifier.</param>
        public void UpdateAppWideControls(bool isMonitoring, string serverId)
        {
            SetStartMonitoringButtonStatus(isMonitoring);
            SetStopMonitoringButtonStatus(isMonitoring);
            if (isMonitoring)
            {
                SetStatusBarLabelText(string.Format(
                    "Monitoring server at http://www.quakelive.com/#!join/{0}",
                    (string.IsNullOrEmpty(serverId)
                        ? "..."
                        : serverId)));
            }
            else
            {
                SetStatusBarLabelText("Not monitoring a server.");
            }
        }

        /// <summary>
        /// Enables or disables the 'start monitoring button'.
        /// </summary>
        /// <param name="isMonitoring">if set to <c>true</c> then server
        /// monitoring is active. if set to <c>false</c> then
        /// server monitoring is inactive.</param>
        private void SetStartMonitoringButtonStatus(bool isMonitoring)
        {
            if (StartMonitoringButton.InvokeRequired)
            {
                StartMonitoringButton.Invoke(new Action<bool>(SetStartMonitoringButtonStatus), isMonitoring);
                return;
            }
            StartMonitoringButton.Enabled = !isMonitoring;
        }

        /// <summary>
        /// Sets the status bar label text.
        /// </summary>
        /// <param name="text">The text.</param>
        private void SetStatusBarLabelText(string text)
        {
            StatusBar.Text = text;
        }

        /// <summary>
        /// Enables or disables the 'stop monitoring button'.
        /// </summary>
        /// <param name="isMonitoring">if set to <c>true</c> then server
        /// monitoring is active. if set to <c>false</c> then
        /// server monitoring is inactive.</param>
        private void SetStopMonitoringButtonStatus(bool isMonitoring)
        {
            if (StopMonitoringButton.InvokeRequired)
            {
                StopMonitoringButton.Invoke(new Action<bool>(SetStopMonitoringButtonStatus), isMonitoring);
                return;
            }

            StopMonitoringButton.Enabled = isMonitoring;
        }
    }
}