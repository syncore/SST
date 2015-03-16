namespace SSB.Ui
{
    partial class Gui
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.sendButton = new System.Windows.Forms.Button();
            this.ConsoleTextBox = new System.Windows.Forms.TextBox();
            this.commandTextBox = new System.Windows.Forms.TextBox();
            this.findQLWindowButton = new System.Windows.Forms.Button();
            this.startReadQLConsoleButton = new System.Windows.Forms.Button();
            this.stopReadQLConsoleButton = new System.Windows.Forms.Button();
            this.clearQlConsoleButton = new System.Windows.Forms.Button();
            this.serverIdButton = new System.Windows.Forms.Button();
            this.getPlayersButton = new System.Windows.Forms.Button();
            this.appendCheckbox = new System.Windows.Forms.CheckBox();
            this.newGuiButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // sendButton
            // 
            this.sendButton.Location = new System.Drawing.Point(71, 564);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(82, 32);
            this.sendButton.TabIndex = 0;
            this.sendButton.Text = "Send";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // ConsoleTextBox
            // 
            this.ConsoleTextBox.Location = new System.Drawing.Point(71, 83);
            this.ConsoleTextBox.Multiline = true;
            this.ConsoleTextBox.Name = "ConsoleTextBox";
            this.ConsoleTextBox.Size = new System.Drawing.Size(767, 475);
            this.ConsoleTextBox.TabIndex = 1;
            // 
            // commandTextBox
            // 
            this.commandTextBox.Location = new System.Drawing.Point(184, 571);
            this.commandTextBox.Name = "commandTextBox";
            this.commandTextBox.Size = new System.Drawing.Size(654, 20);
            this.commandTextBox.TabIndex = 2;
            // 
            // findQLWindowButton
            // 
            this.findQLWindowButton.Location = new System.Drawing.Point(675, 12);
            this.findQLWindowButton.Name = "findQLWindowButton";
            this.findQLWindowButton.Size = new System.Drawing.Size(163, 38);
            this.findQLWindowButton.TabIndex = 3;
            this.findQLWindowButton.Text = "Find QL Window";
            this.findQLWindowButton.UseVisualStyleBackColor = true;
            this.findQLWindowButton.Click += new System.EventHandler(this.findQLWindowButton_Click);
            // 
            // startReadQLConsoleButton
            // 
            this.startReadQLConsoleButton.Location = new System.Drawing.Point(71, 12);
            this.startReadQLConsoleButton.Name = "startReadQLConsoleButton";
            this.startReadQLConsoleButton.Size = new System.Drawing.Size(163, 38);
            this.startReadQLConsoleButton.TabIndex = 4;
            this.startReadQLConsoleButton.Text = "Start Reading QL Console";
            this.startReadQLConsoleButton.UseVisualStyleBackColor = true;
            this.startReadQLConsoleButton.Click += new System.EventHandler(this.startReadQLConsoleButton_Click);
            // 
            // stopReadQLConsoleButton
            // 
            this.stopReadQLConsoleButton.Location = new System.Drawing.Point(252, 12);
            this.stopReadQLConsoleButton.Name = "stopReadQLConsoleButton";
            this.stopReadQLConsoleButton.Size = new System.Drawing.Size(163, 38);
            this.stopReadQLConsoleButton.TabIndex = 5;
            this.stopReadQLConsoleButton.Text = "Stop Reading QL Console";
            this.stopReadQLConsoleButton.UseVisualStyleBackColor = true;
            this.stopReadQLConsoleButton.Click += new System.EventHandler(this.stopReadQLConsoleButton_Click);
            // 
            // clearQlConsoleButton
            // 
            this.clearQlConsoleButton.Location = new System.Drawing.Point(435, 12);
            this.clearQlConsoleButton.Name = "clearQlConsoleButton";
            this.clearQlConsoleButton.Size = new System.Drawing.Size(163, 38);
            this.clearQlConsoleButton.TabIndex = 6;
            this.clearQlConsoleButton.Text = "Clear QL Console";
            this.clearQlConsoleButton.UseVisualStyleBackColor = true;
            this.clearQlConsoleButton.Click += new System.EventHandler(this.clearQlConsoleButton_Click);
            // 
            // serverIdButton
            // 
            this.serverIdButton.Location = new System.Drawing.Point(184, 610);
            this.serverIdButton.Name = "serverIdButton";
            this.serverIdButton.Size = new System.Drawing.Size(110, 28);
            this.serverIdButton.TabIndex = 7;
            this.serverIdButton.Text = "Get ServerId";
            this.serverIdButton.UseVisualStyleBackColor = true;
            this.serverIdButton.Click += new System.EventHandler(this.ServerIdButton_Click);
            // 
            // getPlayersButton
            // 
            this.getPlayersButton.Location = new System.Drawing.Point(305, 610);
            this.getPlayersButton.Name = "getPlayersButton";
            this.getPlayersButton.Size = new System.Drawing.Size(110, 28);
            this.getPlayersButton.TabIndex = 8;
            this.getPlayersButton.Text = "Get Players";
            this.getPlayersButton.UseVisualStyleBackColor = true;
            this.getPlayersButton.Click += new System.EventHandler(this.getPlayersButton_Click);
            // 
            // appendCheckbox
            // 
            this.appendCheckbox.AutoSize = true;
            this.appendCheckbox.Location = new System.Drawing.Point(74, 60);
            this.appendCheckbox.Name = "appendCheckbox";
            this.appendCheckbox.Size = new System.Drawing.Size(149, 17);
            this.appendCheckbox.TabIndex = 9;
            this.appendCheckbox.Text = "Append to console below:";
            this.appendCheckbox.UseVisualStyleBackColor = true;
            this.appendCheckbox.CheckedChanged += new System.EventHandler(this.appendCheckbox_CheckedChanged);
            // 
            // newGuiButton
            // 
            this.newGuiButton.Location = new System.Drawing.Point(728, 610);
            this.newGuiButton.Name = "newGuiButton";
            this.newGuiButton.Size = new System.Drawing.Size(110, 28);
            this.newGuiButton.TabIndex = 10;
            this.newGuiButton.Text = "New GUI Test";
            this.newGuiButton.UseVisualStyleBackColor = true;
            this.newGuiButton.Click += new System.EventHandler(this.newGuiButton_Click);
            // 
            // Gui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(947, 670);
            this.Controls.Add(this.newGuiButton);
            this.Controls.Add(this.appendCheckbox);
            this.Controls.Add(this.getPlayersButton);
            this.Controls.Add(this.serverIdButton);
            this.Controls.Add(this.clearQlConsoleButton);
            this.Controls.Add(this.stopReadQLConsoleButton);
            this.Controls.Add(this.startReadQLConsoleButton);
            this.Controls.Add(this.findQLWindowButton);
            this.Controls.Add(this.commandTextBox);
            this.Controls.Add(this.ConsoleTextBox);
            this.Controls.Add(this.sendButton);
            this.Name = "Gui";
            this.Text = "SSB";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button sendButton;
        private System.Windows.Forms.TextBox ConsoleTextBox;
        private System.Windows.Forms.TextBox commandTextBox;
        private System.Windows.Forms.Button findQLWindowButton;
        private System.Windows.Forms.Button startReadQLConsoleButton;
        private System.Windows.Forms.Button stopReadQLConsoleButton;
        private System.Windows.Forms.Button clearQlConsoleButton;
        private System.Windows.Forms.Button serverIdButton;
        private System.Windows.Forms.Button getPlayersButton;
        private System.Windows.Forms.CheckBox appendCheckbox;
        private System.Windows.Forms.Button newGuiButton;
    }
}

