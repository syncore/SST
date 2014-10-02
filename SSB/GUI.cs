using System;
using System.Windows.Forms;

namespace SSB
{
    public partial class Gui : Form
    {
        private readonly SynServerBot _ssb;

        /// <summary>
        /// Initializes a new instance of the <see cref="Gui"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Gui(SynServerBot ssb)
        {
            InitializeComponent();
            _ssb = ssb;
            SetGuiControls();
        }

        /// <summary>
        ///     Handles the CheckedChanged event of the appendCheckbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void appendCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            _ssb.GuiOptions.IsAppendToSsbGuiConsole = appendCheckbox.Checked;
        }

        /// <summary>
        ///     Handles the Click event of the clearQlConsoleButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void clearQlConsoleButton_Click(object sender, EventArgs e)
        {
            _ssb.QlCommands.ClearBothQlConsoles();
            ConsoleTextBox.Clear();
            commandTextBox.Focus();
        }

        /// <summary>
        ///     Handles the Click event of the findQLWindowButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void findQLWindowButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(_ssb.QlWindowUtils.QuakeLiveConsoleWindowExists()
                ? "Found necessarry Quake Live window information"
                : "Unable to find necessary Quake Live window information");
        }

        /// <summary>
        ///     Handles the Click event of the getPlayersButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void getPlayersButton_Click(object sender, EventArgs e)
        {
            _ssb.QlCommands.QlCmdPlayers(true);
        }

        /// <summary>
        ///     Handles the Click event of the sendButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void sendButton_Click(object sender, EventArgs e)
        {
            bool delay;
            if (commandTextBox.Text.Equals("players", StringComparison.InvariantCultureIgnoreCase))
            {
                delay = true;
            }
            else if (commandTextBox.Text.Equals("serverinfo", StringComparison.InvariantCultureIgnoreCase))
            {
                delay = true;
            }
            else if (commandTextBox.Text.Equals("configstrings", StringComparison.InvariantCultureIgnoreCase))
            {
                delay = true;
            }
            else
            {
                delay = false;
            }
            _ssb.QlCommands.SendToQl(commandTextBox.Text, delay);
            commandTextBox.Focus();
        }

        /// <summary>
        ///     Handles the Click event of the ServerIdButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void ServerIdButton_Click(object sender, EventArgs e)
        {
            _ssb.QlCommands.SendToQl("serverinfo", true);
        }

        /// <summary>
        ///     Sets references to GUI controls so they can be used from other classes.
        /// </summary>
        private void SetGuiControls()
        {
            _ssb.GuiControls.ConsoleTextBox = ConsoleTextBox;
        }

        /// <summary>
        ///     Handles the Click event of the startReadQLConsoleButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void startReadQLConsoleButton_Click(object sender, EventArgs e)
        {
            _ssb.StartConsoleReadThread();
            commandTextBox.Focus();
        }

        /// <summary>
        ///     Handles the Click event of the stopReadQLConsoleButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void stopReadQLConsoleButton_Click(object sender, EventArgs e)
        {
            _ssb.StopConsoleReadThread();
            ConsoleTextBox.Clear();
            commandTextBox.Focus();
        }
    }
}