using System.Windows.Forms;

namespace SSB.Ui
{
    /// <summary>
    ///     Class that holds references to some of the selected GUI controls in GUI.cs, allowing us to access them from other
    ///     classes.
    /// </summary>
    public class GuiControls
    {
        /// <summary>
        ///     Gets or sets the SSB GUI console text box.
        /// </summary>
        /// <value>
        ///     The SSB GUI console text box.
        /// </value>
        public TextBox ConsoleTextBox { get; set; }
    }
}