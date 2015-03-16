using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSB.Util;

namespace SSB.Ui
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void ssbLogo_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == 0)
                return;
            Win32Api.ReleaseCapture();
            Win32Api.SendMessage(this.Handle, Win32Api.WM_NCLBUTTONDOWN, Win32Api.HT_CAPTION, 0);
        }

    }
}
