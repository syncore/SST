namespace SSB.Ui
{
    partial class Main
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.UiTabCtl = new System.Windows.Forms.TabControl();
            this.mainTab = new System.Windows.Forms.TabPage();
            this.appendToMainCheckbox = new System.Windows.Forms.CheckBox();
            this.ConsoleTextBox = new System.Windows.Forms.TextBox();
            this.coreOptionsTab = new System.Windows.Forms.TabPage();
            this.coreOptionsGrpBox = new System.Windows.Forms.GroupBox();
            this.settingsGroupBox = new System.Windows.Forms.GroupBox();
            this.saveSettingsButton = new System.Windows.Forms.Button();
            this.loadSettingsButton = new System.Windows.Forms.Button();
            this.logMainWindowCheckbox = new System.Windows.Forms.CheckBox();
            this.eloCacheTextBox = new System.Windows.Forms.TextBox();
            this.eloCacheLabel = new System.Windows.Forms.Label();
            this.ownerLabel = new System.Windows.Forms.Label();
            this.ownersTextBox = new System.Windows.Forms.TextBox();
            this.accountNameTextBox = new System.Windows.Forms.TextBox();
            this.accountNameLabel = new System.Windows.Forms.Label();
            this.modulesTab = new System.Windows.Forms.TabPage();
            this.moduleTabControl = new System.Windows.Forms.TabControl();
            this.accountDateTab = new System.Windows.Forms.TabPage();
            this.accuracyTab = new System.Windows.Forms.TabPage();
            this.aboutTab = new System.Windows.Forms.TabPage();
            this.aboutGroupBox = new System.Windows.Forms.GroupBox();
            this.aboutLabel3 = new System.Windows.Forms.Label();
            this.aboutSsbWebButton = new System.Windows.Forms.Button();
            this.aboutAuthorLabel = new System.Windows.Forms.Label();
            this.aboutLabelVersion = new System.Windows.Forms.Label();
            this.aboutVersPlaceHolder = new System.Windows.Forms.Label();
            this.aboutLabel2 = new System.Windows.Forms.Label();
            this.aboutLabel1 = new System.Windows.Forms.Label();
            this.ssbAboutPictureBox = new System.Windows.Forms.PictureBox();
            this.startButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.coreToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.autoVoterTab = new System.Windows.Forms.TabPage();
            this.earlyQuitTab = new System.Windows.Forms.TabPage();
            this.eloLimitTab = new System.Windows.Forms.TabPage();
            this.ircTab = new System.Windows.Forms.TabPage();
            this.motdTab = new System.Windows.Forms.TabPage();
            this.pickupTab = new System.Windows.Forms.TabPage();
            this.serversTab = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.UiTabCtl.SuspendLayout();
            this.mainTab.SuspendLayout();
            this.coreOptionsTab.SuspendLayout();
            this.coreOptionsGrpBox.SuspendLayout();
            this.settingsGroupBox.SuspendLayout();
            this.modulesTab.SuspendLayout();
            this.moduleTabControl.SuspendLayout();
            this.aboutTab.SuspendLayout();
            this.aboutGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ssbAboutPictureBox)).BeginInit();
            this.statusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(740, 100);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // UiTabCtl
            // 
            this.UiTabCtl.Appearance = System.Windows.Forms.TabAppearance.Buttons;
            this.UiTabCtl.Controls.Add(this.mainTab);
            this.UiTabCtl.Controls.Add(this.coreOptionsTab);
            this.UiTabCtl.Controls.Add(this.modulesTab);
            this.UiTabCtl.Controls.Add(this.aboutTab);
            this.UiTabCtl.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UiTabCtl.Location = new System.Drawing.Point(0, 102);
            this.UiTabCtl.Name = "UiTabCtl";
            this.UiTabCtl.SelectedIndex = 0;
            this.UiTabCtl.Size = new System.Drawing.Size(740, 500);
            this.UiTabCtl.TabIndex = 1;
            // 
            // mainTab
            // 
            this.mainTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.mainTab.Controls.Add(this.appendToMainCheckbox);
            this.mainTab.Controls.Add(this.ConsoleTextBox);
            this.mainTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainTab.Location = new System.Drawing.Point(4, 26);
            this.mainTab.Name = "mainTab";
            this.mainTab.Padding = new System.Windows.Forms.Padding(3);
            this.mainTab.Size = new System.Drawing.Size(732, 470);
            this.mainTab.TabIndex = 0;
            this.mainTab.Text = "Main";
            // 
            // appendToMainCheckbox
            // 
            this.appendToMainCheckbox.AutoSize = true;
            this.appendToMainCheckbox.ForeColor = System.Drawing.Color.White;
            this.appendToMainCheckbox.Location = new System.Drawing.Point(9, 435);
            this.appendToMainCheckbox.Name = "appendToMainCheckbox";
            this.appendToMainCheckbox.Size = new System.Drawing.Size(123, 18);
            this.appendToMainCheckbox.TabIndex = 1;
            this.appendToMainCheckbox.Text = "Show SSB events";
            this.appendToMainCheckbox.UseVisualStyleBackColor = true;
            // 
            // ConsoleTextBox
            // 
            this.ConsoleTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.ConsoleTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ConsoleTextBox.Location = new System.Drawing.Point(2, 0);
            this.ConsoleTextBox.Multiline = true;
            this.ConsoleTextBox.Name = "ConsoleTextBox";
            this.ConsoleTextBox.ReadOnly = true;
            this.ConsoleTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ConsoleTextBox.Size = new System.Drawing.Size(729, 422);
            this.ConsoleTextBox.TabIndex = 0;
            // 
            // coreOptionsTab
            // 
            this.coreOptionsTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.coreOptionsTab.Controls.Add(this.coreOptionsGrpBox);
            this.coreOptionsTab.Location = new System.Drawing.Point(4, 26);
            this.coreOptionsTab.Name = "coreOptionsTab";
            this.coreOptionsTab.Padding = new System.Windows.Forms.Padding(3);
            this.coreOptionsTab.Size = new System.Drawing.Size(732, 470);
            this.coreOptionsTab.TabIndex = 1;
            this.coreOptionsTab.Text = "Core Options";
            // 
            // coreOptionsGrpBox
            // 
            this.coreOptionsGrpBox.Controls.Add(this.settingsGroupBox);
            this.coreOptionsGrpBox.Controls.Add(this.logMainWindowCheckbox);
            this.coreOptionsGrpBox.Controls.Add(this.eloCacheTextBox);
            this.coreOptionsGrpBox.Controls.Add(this.eloCacheLabel);
            this.coreOptionsGrpBox.Controls.Add(this.ownerLabel);
            this.coreOptionsGrpBox.Controls.Add(this.ownersTextBox);
            this.coreOptionsGrpBox.Controls.Add(this.accountNameTextBox);
            this.coreOptionsGrpBox.Controls.Add(this.accountNameLabel);
            this.coreOptionsGrpBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.coreOptionsGrpBox.ForeColor = System.Drawing.Color.White;
            this.coreOptionsGrpBox.Location = new System.Drawing.Point(25, 16);
            this.coreOptionsGrpBox.Name = "coreOptionsGrpBox";
            this.coreOptionsGrpBox.Size = new System.Drawing.Size(679, 441);
            this.coreOptionsGrpBox.TabIndex = 0;
            this.coreOptionsGrpBox.TabStop = false;
            this.coreOptionsGrpBox.Text = "Core Options";
            // 
            // settingsGroupBox
            // 
            this.settingsGroupBox.Controls.Add(this.saveSettingsButton);
            this.settingsGroupBox.Controls.Add(this.loadSettingsButton);
            this.settingsGroupBox.ForeColor = System.Drawing.Color.White;
            this.settingsGroupBox.Location = new System.Drawing.Point(473, 385);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Size = new System.Drawing.Size(200, 50);
            this.settingsGroupBox.TabIndex = 9;
            this.settingsGroupBox.TabStop = false;
            this.settingsGroupBox.Text = "Settings";
            // 
            // saveSettingsButton
            // 
            this.saveSettingsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(111)))));
            this.saveSettingsButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.saveSettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveSettingsButton.ForeColor = System.Drawing.Color.White;
            this.saveSettingsButton.Location = new System.Drawing.Point(115, 21);
            this.saveSettingsButton.Name = "saveSettingsButton";
            this.saveSettingsButton.Size = new System.Drawing.Size(79, 23);
            this.saveSettingsButton.TabIndex = 1;
            this.saveSettingsButton.Text = "Save";
            this.saveSettingsButton.UseVisualStyleBackColor = false;
            // 
            // loadSettingsButton
            // 
            this.loadSettingsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(111)))));
            this.loadSettingsButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.loadSettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.loadSettingsButton.ForeColor = System.Drawing.Color.White;
            this.loadSettingsButton.Location = new System.Drawing.Point(19, 21);
            this.loadSettingsButton.Name = "loadSettingsButton";
            this.loadSettingsButton.Size = new System.Drawing.Size(79, 23);
            this.loadSettingsButton.TabIndex = 8;
            this.loadSettingsButton.Text = "Load";
            this.loadSettingsButton.UseVisualStyleBackColor = false;
            // 
            // logMainWindowCheckbox
            // 
            this.logMainWindowCheckbox.AutoSize = true;
            this.logMainWindowCheckbox.Location = new System.Drawing.Point(78, 240);
            this.logMainWindowCheckbox.Name = "logMainWindowCheckbox";
            this.logMainWindowCheckbox.Size = new System.Drawing.Size(153, 18);
            this.logMainWindowCheckbox.TabIndex = 7;
            this.logMainWindowCheckbox.Text = "Log SSB Events to Disk";
            this.coreToolTip.SetToolTip(this.logMainWindowCheckbox, "Select whether you want key SSB events to be logged to\r\na log file on the disk (m" +
        "ainly for debugging purposes).");
            this.logMainWindowCheckbox.UseVisualStyleBackColor = true;
            // 
            // eloCacheTextBox
            // 
            this.eloCacheTextBox.BackColor = System.Drawing.Color.Black;
            this.eloCacheTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.eloCacheTextBox.ForeColor = System.Drawing.Color.White;
            this.eloCacheTextBox.Location = new System.Drawing.Point(78, 185);
            this.eloCacheTextBox.Name = "eloCacheTextBox";
            this.eloCacheTextBox.Size = new System.Drawing.Size(205, 22);
            this.eloCacheTextBox.TabIndex = 5;
            this.coreToolTip.SetToolTip(this.eloCacheTextBox, "Enter the time in minutes to cache QLRanks Elo results.\r\nThis will save the Elo d" +
        "ata so SSB does not have to\r\nconstantly access the QLRanks website.");
            // 
            // eloCacheLabel
            // 
            this.eloCacheLabel.AutoSize = true;
            this.eloCacheLabel.Location = new System.Drawing.Point(75, 155);
            this.eloCacheLabel.Name = "eloCacheLabel";
            this.eloCacheLabel.Size = new System.Drawing.Size(187, 14);
            this.eloCacheLabel.TabIndex = 4;
            this.eloCacheLabel.Text = "Elo Cache Expiration [in minutes]";
            this.coreToolTip.SetToolTip(this.eloCacheLabel, "Enter the time in minutes to cache QLRanks Elo results.\r\nThis will save the Elo d" +
        "ata so SSB does not have to\r\nconstantly access the QLRanks website.");
            // 
            // ownerLabel
            // 
            this.ownerLabel.AutoSize = true;
            this.ownerLabel.Location = new System.Drawing.Point(404, 76);
            this.ownerLabel.Name = "ownerLabel";
            this.ownerLabel.Size = new System.Drawing.Size(138, 14);
            this.ownerLabel.TabIndex = 3;
            this.ownerLabel.Text = "Owner(s) [one per line]";
            this.coreToolTip.SetToolTip(this.ownerLabel, resources.GetString("ownerLabel.ToolTip"));
            // 
            // ownersTextBox
            // 
            this.ownersTextBox.BackColor = System.Drawing.Color.Black;
            this.ownersTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ownersTextBox.ForeColor = System.Drawing.Color.White;
            this.ownersTextBox.Location = new System.Drawing.Point(407, 104);
            this.ownersTextBox.Multiline = true;
            this.ownersTextBox.Name = "ownersTextBox";
            this.ownersTextBox.Size = new System.Drawing.Size(205, 163);
            this.ownersTextBox.TabIndex = 2;
            this.coreToolTip.SetToolTip(this.ownersTextBox, resources.GetString("ownersTextBox.ToolTip"));
            // 
            // accountNameTextBox
            // 
            this.accountNameTextBox.BackColor = System.Drawing.Color.Black;
            this.accountNameTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.accountNameTextBox.ForeColor = System.Drawing.Color.White;
            this.accountNameTextBox.Location = new System.Drawing.Point(78, 104);
            this.accountNameTextBox.MaxLength = 15;
            this.accountNameTextBox.Name = "accountNameTextBox";
            this.accountNameTextBox.Size = new System.Drawing.Size(205, 22);
            this.accountNameTextBox.TabIndex = 1;
            this.coreToolTip.SetToolTip(this.accountNameTextBox, "Enter the name of the QL account that\r\nwill be running the bot. Do not include th" +
        "e clan tag.\r\n");
            // 
            // accountNameLabel
            // 
            this.accountNameLabel.AutoSize = true;
            this.accountNameLabel.Location = new System.Drawing.Point(75, 76);
            this.accountNameLabel.Name = "accountNameLabel";
            this.accountNameLabel.Size = new System.Drawing.Size(107, 14);
            this.accountNameLabel.TabIndex = 0;
            this.accountNameLabel.Text = "QL Account Name";
            // 
            // modulesTab
            // 
            this.modulesTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.modulesTab.Controls.Add(this.moduleTabControl);
            this.modulesTab.Location = new System.Drawing.Point(4, 26);
            this.modulesTab.Name = "modulesTab";
            this.modulesTab.Size = new System.Drawing.Size(732, 470);
            this.modulesTab.TabIndex = 2;
            this.modulesTab.Text = "Modules";
            // 
            // moduleTabControl
            // 
            this.moduleTabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.moduleTabControl.Controls.Add(this.accountDateTab);
            this.moduleTabControl.Controls.Add(this.accuracyTab);
            this.moduleTabControl.Controls.Add(this.autoVoterTab);
            this.moduleTabControl.Controls.Add(this.earlyQuitTab);
            this.moduleTabControl.Controls.Add(this.eloLimitTab);
            this.moduleTabControl.Controls.Add(this.ircTab);
            this.moduleTabControl.Controls.Add(this.motdTab);
            this.moduleTabControl.Controls.Add(this.pickupTab);
            this.moduleTabControl.Controls.Add(this.serversTab);
            this.moduleTabControl.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.moduleTabControl.Location = new System.Drawing.Point(0, 0);
            this.moduleTabControl.Multiline = true;
            this.moduleTabControl.Name = "moduleTabControl";
            this.moduleTabControl.SelectedIndex = 0;
            this.moduleTabControl.Size = new System.Drawing.Size(736, 458);
            this.moduleTabControl.TabIndex = 0;
            // 
            // accountDateTab
            // 
            this.accountDateTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.accountDateTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.accountDateTab.ForeColor = System.Drawing.Color.White;
            this.accountDateTab.Location = new System.Drawing.Point(4, 25);
            this.accountDateTab.Name = "accountDateTab";
            this.accountDateTab.Padding = new System.Windows.Forms.Padding(3);
            this.accountDateTab.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.accountDateTab.Size = new System.Drawing.Size(728, 429);
            this.accountDateTab.TabIndex = 0;
            this.accountDateTab.Text = "Account Date Limiter";
            // 
            // accuracyTab
            // 
            this.accuracyTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.accuracyTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.accuracyTab.ForeColor = System.Drawing.Color.White;
            this.accuracyTab.Location = new System.Drawing.Point(4, 25);
            this.accuracyTab.Name = "accuracyTab";
            this.accuracyTab.Padding = new System.Windows.Forms.Padding(3);
            this.accuracyTab.Size = new System.Drawing.Size(728, 429);
            this.accuracyTab.TabIndex = 1;
            this.accuracyTab.Text = "Accuracy";
            // 
            // aboutTab
            // 
            this.aboutTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.aboutTab.Controls.Add(this.aboutGroupBox);
            this.aboutTab.ForeColor = System.Drawing.Color.White;
            this.aboutTab.Location = new System.Drawing.Point(4, 26);
            this.aboutTab.Name = "aboutTab";
            this.aboutTab.Size = new System.Drawing.Size(732, 470);
            this.aboutTab.TabIndex = 3;
            this.aboutTab.Text = "About";
            // 
            // aboutGroupBox
            // 
            this.aboutGroupBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.aboutGroupBox.Controls.Add(this.aboutLabel3);
            this.aboutGroupBox.Controls.Add(this.aboutSsbWebButton);
            this.aboutGroupBox.Controls.Add(this.aboutAuthorLabel);
            this.aboutGroupBox.Controls.Add(this.aboutLabelVersion);
            this.aboutGroupBox.Controls.Add(this.aboutVersPlaceHolder);
            this.aboutGroupBox.Controls.Add(this.aboutLabel2);
            this.aboutGroupBox.Controls.Add(this.aboutLabel1);
            this.aboutGroupBox.Controls.Add(this.ssbAboutPictureBox);
            this.aboutGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.aboutGroupBox.ForeColor = System.Drawing.Color.White;
            this.aboutGroupBox.Location = new System.Drawing.Point(27, 16);
            this.aboutGroupBox.Name = "aboutGroupBox";
            this.aboutGroupBox.Size = new System.Drawing.Size(679, 441);
            this.aboutGroupBox.TabIndex = 1;
            this.aboutGroupBox.TabStop = false;
            this.aboutGroupBox.Text = "About SSB";
            // 
            // aboutLabel3
            // 
            this.aboutLabel3.AutoSize = true;
            this.aboutLabel3.Location = new System.Drawing.Point(82, 223);
            this.aboutLabel3.Name = "aboutLabel3";
            this.aboutLabel3.Size = new System.Drawing.Size(161, 14);
            this.aboutLabel3.TabIndex = 7;
            this.aboutLabel3.Text = "#ssb_ql on irc.quakenet.org";
            // 
            // aboutSsbWebButton
            // 
            this.aboutSsbWebButton.ForeColor = System.Drawing.Color.Black;
            this.aboutSsbWebButton.Location = new System.Drawing.Point(85, 306);
            this.aboutSsbWebButton.Name = "aboutSsbWebButton";
            this.aboutSsbWebButton.Size = new System.Drawing.Size(149, 23);
            this.aboutSsbWebButton.TabIndex = 6;
            this.aboutSsbWebButton.Text = "SSB Website";
            this.aboutSsbWebButton.UseVisualStyleBackColor = true;
            // 
            // aboutAuthorLabel
            // 
            this.aboutAuthorLabel.AutoSize = true;
            this.aboutAuthorLabel.Location = new System.Drawing.Point(82, 194);
            this.aboutAuthorLabel.Name = "aboutAuthorLabel";
            this.aboutAuthorLabel.Size = new System.Drawing.Size(246, 14);
            this.aboutAuthorLabel.TabIndex = 5;
            this.aboutAuthorLabel.Text = "Copyright 2015 syncore. All rights reserved.";
            // 
            // aboutLabelVersion
            // 
            this.aboutLabelVersion.AutoSize = true;
            this.aboutLabelVersion.Location = new System.Drawing.Point(135, 153);
            this.aboutLabelVersion.Name = "aboutLabelVersion";
            this.aboutLabelVersion.Size = new System.Drawing.Size(47, 14);
            this.aboutLabelVersion.TabIndex = 4;
            this.aboutLabelVersion.Text = "0.0.0.0";
            // 
            // aboutVersPlaceHolder
            // 
            this.aboutVersPlaceHolder.AutoSize = true;
            this.aboutVersPlaceHolder.Location = new System.Drawing.Point(82, 153);
            this.aboutVersPlaceHolder.Name = "aboutVersPlaceHolder";
            this.aboutVersPlaceHolder.Size = new System.Drawing.Size(47, 14);
            this.aboutVersPlaceHolder.TabIndex = 3;
            this.aboutVersPlaceHolder.Text = "Version";
            // 
            // aboutLabel2
            // 
            this.aboutLabel2.AutoSize = true;
            this.aboutLabel2.Location = new System.Drawing.Point(164, 102);
            this.aboutLabel2.Name = "aboutLabel2";
            this.aboutLabel2.Size = new System.Drawing.Size(227, 14);
            this.aboutLabel2.TabIndex = 2;
            this.aboutLabel2.Text = "A Quake Live Server Administration Tool";
            // 
            // aboutLabel1
            // 
            this.aboutLabel1.AutoSize = true;
            this.aboutLabel1.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.aboutLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(163)))), ((int)(((byte)(185)))));
            this.aboutLabel1.Location = new System.Drawing.Point(162, 67);
            this.aboutLabel1.Name = "aboutLabel1";
            this.aboutLabel1.Size = new System.Drawing.Size(52, 25);
            this.aboutLabel1.TabIndex = 1;
            this.aboutLabel1.Text = "SSB";
            // 
            // ssbAboutPictureBox
            // 
            this.ssbAboutPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("ssbAboutPictureBox.Image")));
            this.ssbAboutPictureBox.Location = new System.Drawing.Point(81, 67);
            this.ssbAboutPictureBox.Name = "ssbAboutPictureBox";
            this.ssbAboutPictureBox.Size = new System.Drawing.Size(64, 64);
            this.ssbAboutPictureBox.TabIndex = 0;
            this.ssbAboutPictureBox.TabStop = false;
            // 
            // startButton
            // 
            this.startButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(111)))));
            this.startButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.startButton.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.startButton.ForeColor = System.Drawing.Color.White;
            this.startButton.Location = new System.Drawing.Point(12, 611);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(110, 28);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = false;
            // 
            // exitButton
            // 
            this.exitButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(111)))));
            this.exitButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.exitButton.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.exitButton.ForeColor = System.Drawing.Color.White;
            this.exitButton.Location = new System.Drawing.Point(132, 611);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(110, 28);
            this.exitButton.TabIndex = 2;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = false;
            // 
            // coreToolTip
            // 
            this.coreToolTip.IsBalloon = true;
            this.coreToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            // 
            // statusBar
            // 
            this.statusBar.BackColor = System.Drawing.Color.Black;
            this.statusBar.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusBar.Location = new System.Drawing.Point(0, 648);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(742, 22);
            this.statusBar.TabIndex = 3;
            this.statusBar.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.ForeColor = System.Drawing.Color.White;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(85, 17);
            this.toolStripStatusLabel1.Text = "statusBarLabel";
            this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // autoVoterTab
            // 
            this.autoVoterTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.autoVoterTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.autoVoterTab.ForeColor = System.Drawing.Color.White;
            this.autoVoterTab.Location = new System.Drawing.Point(4, 25);
            this.autoVoterTab.Name = "autoVoterTab";
            this.autoVoterTab.Size = new System.Drawing.Size(728, 429);
            this.autoVoterTab.TabIndex = 2;
            this.autoVoterTab.Text = "Auto Voter";
            // 
            // earlyQuitTab
            // 
            this.earlyQuitTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.earlyQuitTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.earlyQuitTab.ForeColor = System.Drawing.Color.White;
            this.earlyQuitTab.Location = new System.Drawing.Point(4, 25);
            this.earlyQuitTab.Name = "earlyQuitTab";
            this.earlyQuitTab.Size = new System.Drawing.Size(728, 429);
            this.earlyQuitTab.TabIndex = 3;
            this.earlyQuitTab.Text = "Early Quit Banner";
            // 
            // eloLimitTab
            // 
            this.eloLimitTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.eloLimitTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.eloLimitTab.ForeColor = System.Drawing.Color.White;
            this.eloLimitTab.Location = new System.Drawing.Point(4, 25);
            this.eloLimitTab.Name = "eloLimitTab";
            this.eloLimitTab.Size = new System.Drawing.Size(728, 429);
            this.eloLimitTab.TabIndex = 4;
            this.eloLimitTab.Text = "Elo Limiter";
            // 
            // ircTab
            // 
            this.ircTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.ircTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ircTab.ForeColor = System.Drawing.Color.White;
            this.ircTab.Location = new System.Drawing.Point(4, 25);
            this.ircTab.Name = "ircTab";
            this.ircTab.Size = new System.Drawing.Size(728, 429);
            this.ircTab.TabIndex = 5;
            this.ircTab.Text = "IRC";
            // 
            // motdTab
            // 
            this.motdTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.motdTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.motdTab.ForeColor = System.Drawing.Color.White;
            this.motdTab.Location = new System.Drawing.Point(4, 25);
            this.motdTab.Name = "motdTab";
            this.motdTab.Size = new System.Drawing.Size(728, 429);
            this.motdTab.TabIndex = 6;
            this.motdTab.Text = "MOTD";
            // 
            // pickupTab
            // 
            this.pickupTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pickupTab.Location = new System.Drawing.Point(4, 25);
            this.pickupTab.Name = "pickupTab";
            this.pickupTab.Size = new System.Drawing.Size(728, 429);
            this.pickupTab.TabIndex = 7;
            this.pickupTab.Text = "Pickup";
            this.pickupTab.UseVisualStyleBackColor = true;
            // 
            // serversTab
            // 
            this.serversTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.serversTab.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.serversTab.ForeColor = System.Drawing.Color.White;
            this.serversTab.Location = new System.Drawing.Point(4, 25);
            this.serversTab.Name = "serversTab";
            this.serversTab.Size = new System.Drawing.Size(728, 429);
            this.serversTab.TabIndex = 8;
            this.serversTab.Text = "Server List";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.ClientSize = new System.Drawing.Size(742, 670);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.UiTabCtl);
            this.Controls.Add(this.pictureBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Main";
            this.Text = "Main";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.UiTabCtl.ResumeLayout(false);
            this.mainTab.ResumeLayout(false);
            this.mainTab.PerformLayout();
            this.coreOptionsTab.ResumeLayout(false);
            this.coreOptionsGrpBox.ResumeLayout(false);
            this.coreOptionsGrpBox.PerformLayout();
            this.settingsGroupBox.ResumeLayout(false);
            this.modulesTab.ResumeLayout(false);
            this.moduleTabControl.ResumeLayout(false);
            this.aboutTab.ResumeLayout(false);
            this.aboutGroupBox.ResumeLayout(false);
            this.aboutGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ssbAboutPictureBox)).EndInit();
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TabControl UiTabCtl;
        private System.Windows.Forms.TabPage mainTab;
        private System.Windows.Forms.TabPage coreOptionsTab;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.TextBox ConsoleTextBox;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.GroupBox coreOptionsGrpBox;
        private System.Windows.Forms.Label accountNameLabel;
        private System.Windows.Forms.Label ownerLabel;
        private System.Windows.Forms.TextBox ownersTextBox;
        private System.Windows.Forms.TextBox accountNameTextBox;
        private System.Windows.Forms.TextBox eloCacheTextBox;
        private System.Windows.Forms.Label eloCacheLabel;
        private System.Windows.Forms.CheckBox logMainWindowCheckbox;
        private System.Windows.Forms.ToolTip coreToolTip;
        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.CheckBox appendToMainCheckbox;
        private System.Windows.Forms.TabPage modulesTab;
        private System.Windows.Forms.TabPage aboutTab;
        private System.Windows.Forms.GroupBox aboutGroupBox;
        private System.Windows.Forms.PictureBox ssbAboutPictureBox;
        private System.Windows.Forms.Label aboutLabel1;
        private System.Windows.Forms.Label aboutLabel2;
        private System.Windows.Forms.Label aboutVersPlaceHolder;
        private System.Windows.Forms.Label aboutAuthorLabel;
        private System.Windows.Forms.Label aboutLabelVersion;
        private System.Windows.Forms.Label aboutLabel3;
        private System.Windows.Forms.Button aboutSsbWebButton;
        private System.Windows.Forms.Button loadSettingsButton;
        private System.Windows.Forms.Button saveSettingsButton;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.TabControl moduleTabControl;
        private System.Windows.Forms.TabPage accountDateTab;
        private System.Windows.Forms.TabPage accuracyTab;
        private System.Windows.Forms.TabPage autoVoterTab;
        private System.Windows.Forms.TabPage earlyQuitTab;
        private System.Windows.Forms.TabPage eloLimitTab;
        private System.Windows.Forms.TabPage ircTab;
        private System.Windows.Forms.TabPage motdTab;
        private System.Windows.Forms.TabPage pickupTab;
        private System.Windows.Forms.TabPage serversTab;
    }
}