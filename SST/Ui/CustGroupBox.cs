using System.Drawing;
using System.Windows.Forms;

namespace SST.Ui
{
    /// <summary>
    /// A custom GroupBox class.
    /// </summary>
    public class CustGroupBox : GroupBox
    {
        private Color _borderColor = Color.FromArgb(130, 255, 255, 255);

        /// <summary>
        /// Gets or sets the color of the border.
        /// </summary>
        /// <value>The color of the border.</value>
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; }
        }

        /// <summary>
        /// </summary>
        /// <param name="e">
        /// A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.
        /// </param>
        protected override void OnPaint(PaintEventArgs e)
        {
            //get the text size in groupbox
            var tSize = TextRenderer.MeasureText(Text, Font);

            var borderRect = e.ClipRectangle;
            borderRect.Y = (borderRect.Y + (tSize.Height / 2));
            borderRect.Height = (borderRect.Height - (tSize.Height / 2));
            ControlPaint.DrawBorder(e.Graphics, borderRect, _borderColor, ButtonBorderStyle.Dotted);

            var textRect = e.ClipRectangle;
            textRect.X = (textRect.X + 6);
            textRect.Width = tSize.Width;
            textRect.Height = tSize.Height;
            e.Graphics.FillRectangle(new SolidBrush(BackColor), textRect);
            //e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), textRect);
            e.Graphics.DrawString(Text, Font, new SolidBrush(Color.FromArgb(211, 211, 211)), textRect);
        }
    }
}
