using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSB.Config;
using SSB.Core;
using SSB.Enum;
using SSB.Model;
using SSB.Ui.Validation;
using SSB.Ui.Validation.Modules;
using SSB.Util;

namespace SSB.Ui
{
    public partial class UserInterface : Form
    {
        private readonly AccountDateLimitValidator _accountDateLimitValidator;
        private readonly ConfigHandler _cfgHandler;
        private readonly CoreOptionsValidator _coreOptionsValidator;
        private readonly SynServerBot _ssb;

        public UserInterface(SynServerBot ssb)
        {
            InitializeComponent();
            titleBarVersionLabel.Text = string.Format("version {0}",
                typeof(EntryPoint).Assembly.GetName().Version);
            _cfgHandler = new ConfigHandler();
            _ssb = ssb;
            _coreOptionsValidator = new CoreOptionsValidator();
            _accountDateLimitValidator = new AccountDateLimitValidator();
            SetAppWideUiControls();
            PopulateAllUiTabs();
            CheckForAutoMonitoring();
        }

        private void CheckForAutoMonitoring()
        {
            _cfgHandler.ReadConfiguration();
            if (!_cfgHandler.Config.CoreOptions.autoMonitorServerOnStart) return;
            Debug.WriteLine("User has auto monitor on start specified. Attempting to start monitoring.");
            
            // ReSharper disable once UnusedVariable
            // Synchronous
            var a = _ssb.AttemptAutoMonitorStart();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void coreAccountNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreAccountNameTextBox, string.Empty);
        }

        private void coreAccountNameTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_coreOptionsValidator.IsValidQuakeLiveName(coreAccountNameTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                coreAccountNameTextBox.Select(0, coreAccountNameTextBox.Text.Length);
                errorProvider.SetError(coreAccountNameTextBox, errorMsg);
            }
        }

        private void coreEloCacheTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreEloCacheTextBox, string.Empty);
        }

        private void coreEloCacheTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_coreOptionsValidator.IsValidEloCacheExpiration(coreEloCacheTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                coreEloCacheTextBox.Select(0, coreEloCacheTextBox.Text.Length);
                errorProvider.SetError(coreEloCacheTextBox, errorMsg);
            }
        }

        private void coreLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateCoreOptionsUi();
            ShowInfoMessage("Core options settings loaded.", "Settings Loaded");
        }

        private void coreOwnerNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreOwnerNameTextBox, string.Empty);
        }

        private void coreOwnerNameTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_coreOptionsValidator.IsValidQuakeLiveName(coreOwnerNameTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                coreOwnerNameTextBox.Select(0, coreOwnerNameTextBox.Text.Length);
                errorProvider.SetError(coreOwnerNameTextBox, errorMsg);
            }
        }

        private void coreResetDefaultsCheckBox_Click(object sender, EventArgs e)
        {
            _cfgHandler.RestoreDefaultConfiguration();
            PopulateCoreOptionsUi();
            ShowInfoMessage("All SSB settings were reset to their default values.",
                "Defaults Loaded");
        }

        private void coreSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var coreOptions = _cfgHandler.Config.CoreOptions;
                coreOptions.accountName = coreAccountNameTextBox.Text;
                coreOptions.appendToActivityLog = coreAppendEventsCheckBox.Checked;
                coreOptions.autoMonitorServerOnStart = coreAutoMonitorStartCheckBox.Checked;
                coreOptions.eloCacheExpiration = uint.Parse(coreEloCacheTextBox.Text);
                coreOptions.hideAllQlConsoleText = coreHideQlConsoleCheckBox.Checked;
                coreOptions.logSsbEventsToDisk = coreLogEventsDiskCheckBox.Checked;
                coreOptions.owner = coreOwnerNameTextBox.Text;
                _cfgHandler.WriteConfiguration();
                ShowInfoMessage("Core options settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private async Task HandleAccountDateModActivation(bool isActiveInUi, uint minAccountAge)
        {
            if (isActiveInUi)
            {
                await _ssb.Mod.AccountDateLimit.EnableAccountDateLimiter(minAccountAge);
                Debug.WriteLine(
                    "[UI]: Activating account date limiter module from UI. Updating old values as necessary.");
            }
            else
            {
                _ssb.Mod.AccountDateLimit.Active = false;
                Debug.WriteLine("[UI]: Deactivating account date limiter module from UI if active.");
            }
        }

        private void HandleAccuracyModActivation(bool isActiveInUi)
        {
            if (isActiveInUi)
            {
                _ssb.Mod.Accuracy.Active = true;
                Debug.WriteLine(
                    "[UI]: Activating accuracy display module from UI. Updating old values as necessary.");
            }
            else
            {
                _ssb.Mod.Accuracy.Active = false;
                Debug.WriteLine("[UI]: Deactivating accuracy display module from UI if active.");
            }
        }

        private void HandleAutoVoterModActivation(bool isActiveInUi)
        {
            if (isActiveInUi)
            {
                _ssb.Mod.AutoVoter.Active = true;
                Debug.WriteLine(
                    "[UI]: Activating auto voter module from UI. Updating old values as necessary.");
            }
            else
            {
                _ssb.Mod.AutoVoter.Active = false;
                Debug.WriteLine("[UI]: Deactivating auto voter module from UI if active.");
            }
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void modAccDateAccAgeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modAccDateAccAgeTextBox, string.Empty);
        }

        private void modAccDateAccAgeTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_accountDateLimitValidator.IsValidMinimumAccountAge(modAccDateAccAgeTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modAccDateAccAgeTextBox.Select(0, modAccDateAccAgeTextBox.Text.Length);
                errorProvider.SetError(modAccDateAccAgeTextBox, errorMsg);
            }
        }

        private void modAccDateLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAccountDateUi();
            ShowInfoMessage("Account date limiter settings loaded.", "Settings Loaded");
        }

        private async void modAccDateResetSettingsButton_Click(object sender, EventArgs e)
        {
            var accountDateOptions = _cfgHandler.Config.AccountDateOptions;
            accountDateOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModAccountDateUi();
            await
                HandleAccountDateModActivation(accountDateOptions.isActive,
                    accountDateOptions.minimumDaysRequired);
            ShowInfoMessage("Account date limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        private async void modAccDateSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var acctDateOptions = _cfgHandler.Config.AccountDateOptions;
                var minAccountAge = uint.Parse(modAccDateAccAgeTextBox.Text);
                acctDateOptions.isActive = modAccDateEnableCheckBox.Checked;
                acctDateOptions.minimumDaysRequired = minAccountAge;
                _cfgHandler.WriteConfiguration();
                await HandleAccountDateModActivation(modAccDateEnableCheckBox.Checked, minAccountAge);
                ShowInfoMessage("Account date limiter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modAccuracyLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAccuracyUi();
            ShowInfoMessage("Accuracy display settings loaded.", "Settings Loaded");
        }

        private void modAccuracyResetSettingsButton_Click(object sender, EventArgs e)
        {
            var accuracyOptions = _cfgHandler.Config.AccuracyOptions;
            accuracyOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModAccuracyUi();
            HandleAccuracyModActivation(accuracyOptions.isActive);
            ShowInfoMessage("Accuracy display settings were reset to their default values.",
                "Defaults Loaded");
        }

        private void modAccuracySaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var accuracyOptions = _cfgHandler.Config.AccuracyOptions;
                accuracyOptions.isActive = modAccuracyEnableCheckBox.Checked;
                _cfgHandler.WriteConfiguration();
                HandleAccuracyModActivation(modAccuracyEnableCheckBox.Checked);
                ShowInfoMessage("Accuracy display settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modAutoVoterAddVoteButton_Click(object sender, EventArgs e)
        {
            var containsParam = (!string.IsNullOrEmpty(modAutoVoterContainingTextBox.Text));
            var fullVoteText = string.Format("{0} {1}",
                modAutoVoterVoteTypeComboxBox.SelectedValue,
                (containsParam) ? modAutoVoterContainingTextBox.Text : string.Empty).ToLowerInvariant().Trim();

            if (_ssb.Mod.AutoVoter.AutoVotes.Any(v => v.VoteText.Equals(fullVoteText,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                ShowErrorMessage("There is already an existing vote that matches that type!",
                    "Vote already exists");
                return;
            }

            var intendedResult = IntendedVoteResult.Yes;
            if (modAutoVoterRejectRadioButton.Checked)
            {
                intendedResult = IntendedVoteResult.No;
            }
            else if (modAutoVoterPassRadioButton.Checked)
            {
                intendedResult = IntendedVoteResult.Yes;
            }

            _cfgHandler.ReadConfiguration();
            var addedByAdmin = _cfgHandler.Config.CoreOptions.owner;

            _ssb.Mod.AutoVoter.AutoVotes.Add(new AutoVote(fullVoteText, containsParam, intendedResult,
                addedByAdmin));

            RefreshCurrentVotesDataSource();
            modAutoVoterVoteTypeComboxBox.SelectedIndex = 0;
            modAutoVoterContainingTextBox.Clear();

            Debug.WriteLine("[UI]: Owner {0} added auto {1} vote for: {2}",
                addedByAdmin, ((intendedResult == IntendedVoteResult.No) ? "NO" : "YES"),
                fullVoteText);
        }

        private void modAutoVoterClearVotesButton_Click(object sender, EventArgs e)
        {
            _ssb.Mod.AutoVoter.AutoVotes.Clear();
            // Disable auto voter since there are now no votes
            _cfgHandler.Config.AutoVoterOptions.isActive = false;
            _cfgHandler.WriteConfiguration();
            modAutoVoterEnableCheckBox.Checked = false;
            HandleAutoVoterModActivation(modAutoVoterEnableCheckBox.Checked);
            RefreshCurrentVotesDataSource();

            Debug.WriteLine("[UI]: All automatic votes were cleared by owner.");
        }

        private void modAutoVoterDelVoteButton_Click(object sender, EventArgs e)
        {
            if (_ssb.Mod.AutoVoter.AutoVotes.Count == 0 ||
                modAutoVoterCurVotesListBox.SelectedIndex == -1) return;
            
            Debug.WriteLine("[UI]: Owner removed auto {0} vote: {1}",
                ((_ssb.Mod.AutoVoter.AutoVotes[modAutoVoterCurVotesListBox.SelectedIndex].
                IntendedResult == IntendedVoteResult.No) ? "NO" : "YES"),
                _ssb.Mod.AutoVoter.AutoVotes[modAutoVoterCurVotesListBox.SelectedIndex].VoteText);


            try
            {
                _ssb.Mod.AutoVoter.AutoVotes.RemoveAt(modAutoVoterCurVotesListBox.SelectedIndex);
            }
            catch
            {
                // ignored
            }
            RefreshCurrentVotesDataSource();
        }

        private void modAutoVoterLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAutoVoterUi();
            ShowInfoMessage("Auto voter display settings loaded.", "Settings Loaded");
        }

        private void modAutoVoterResetSettingsButton_Click(object sender, EventArgs e)
        {
            var autoVoterOptions = _cfgHandler.Config.AutoVoterOptions;
            autoVoterOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModAutoVoterUi();
            HandleAutoVoterModActivation(autoVoterOptions.isActive);
            ShowInfoMessage("Auto voter settings were reset to their default values.",
                "Defaults Loaded");
        }

        private void modAutoVoterSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var autoVoterOptions = _cfgHandler.Config.AutoVoterOptions;
                autoVoterOptions.isActive = modAutoVoterEnableCheckBox.Checked;
                autoVoterOptions.autoVotes = _ssb.Mod.AutoVoter.AutoVotes;
                _cfgHandler.WriteConfiguration();
                HandleAutoVoterModActivation(modAutoVoterEnableCheckBox.Checked);
                ShowInfoMessage("Auto voter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void moduleTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;
            var currentTabPage = tabControl.SelectedTab;

            // The moduels tabs' data are populated on initial load, but might've changed due to
            // in-game or IRC commands; so re-populate on tab switch.
            if (currentTabPage == accountDateTab)
            {
                PopulateModAccountDateUi();
            }
            else if (currentTabPage == accuracyTab)
            {
                PopulateModAccuracyUi();
            }
            else if (currentTabPage == autoVoterTab)
            {
                PopulateModAutoVoterUi();
            }
            else if (currentTabPage == earlyQuitTab)
            {
            }
            else if (currentTabPage == eloLimitTab)
            {
            }
            else if (currentTabPage == ircTab)
            {
            }
            else if (currentTabPage == motdTab)
            {
            }
            else if (currentTabPage == pickupTab)
            {
            }
            else if (currentTabPage == serversTab)
            {
            }
        }

        private void PopulateAllUiTabs()
        {
            PopulateCoreOptionsUi();
            PopulateModAccountDateUi();
            PopulateModAccuracyUi();
            PopulateModAutoVoterUi();
        }

        private void PopulateCoreOptionsUi()
        {
            _cfgHandler.ReadConfiguration();
            var coreOptions = _cfgHandler.Config.CoreOptions;
            coreAccountNameTextBox.Text = coreOptions.accountName;
            coreAppendEventsCheckBox.Checked = coreOptions.appendToActivityLog;
            coreAutoMonitorStartCheckBox.Checked = coreOptions.autoMonitorServerOnStart;
            coreEloCacheTextBox.Text = coreOptions.eloCacheExpiration.ToString();
            coreHideQlConsoleCheckBox.Checked = coreOptions.hideAllQlConsoleText;
            coreLogEventsDiskCheckBox.Checked = coreOptions.logSsbEventsToDisk;
            coreOwnerNameTextBox.Text = coreOptions.owner;
            Debug.WriteLine("Populated core options user interface.");
        }

        private void PopulateModAccountDateUi()
        {
            _cfgHandler.ReadConfiguration();
            var acctDateOptions = _cfgHandler.Config.AccountDateOptions;
            modAccDateEnableCheckBox.Checked = acctDateOptions.isActive;
            modAccDateAccAgeTextBox.Text = acctDateOptions.minimumDaysRequired.ToString();
            Debug.WriteLine("Populated account date limiter module user interface.");
        }

        private void PopulateModAccuracyUi()
        {
            _cfgHandler.ReadConfiguration();
            modAccuracyEnableCheckBox.Checked = _cfgHandler.Config.AccuracyOptions.isActive;
            Debug.WriteLine("Populated accuracy display module user interface.");
        }

        private void PopulateModAutoVoterUi()
        {
            _cfgHandler.ReadConfiguration();
            var autoVoterOptions = _cfgHandler.Config.AutoVoterOptions;
            modAutoVoterEnableCheckBox.Checked = autoVoterOptions.isActive;
            autoVoterOptions.autoVotes = _ssb.Mod.AutoVoter.AutoVotes;
            
            // Radio buttons
            modAutoVoterPassRadioButton.Checked = true;
            // Vote types combo box
            modAutoVoterVoteTypeComboxBox.DisplayMember = "Name";
            modAutoVoterVoteTypeComboxBox.ValueMember = "Type";
            modAutoVoterVoteTypeBindingSource.DataSource = _ssb.Mod.AutoVoter.ValidCallVotes;
            modAutoVoterVoteTypeComboxBox.DataSource = modAutoVoterVoteTypeBindingSource.DataSource;
            modAutoVoterVoteTypeComboxBox.SelectedIndex = 0;
            // Current votes listbox
            RefreshCurrentVotesDataSource();
            Debug.WriteLine("Populated auto voter module user interface.");
        }

        private void RefreshCurrentVotesDataSource()
        {
            modAutoVoterCurVotesListBox.DataSource = null;
            modAutoVoterCurVotesListBox.DisplayMember = "VoteFormatDisplay";
            modAutoVoterCurVotesListBox.ValueMember = "VoteText";
            modAutoVoterCurrentVotesBindingSource.DataSource = _ssb.Mod.AutoVoter.AutoVotes;
            if (_ssb.Mod.AutoVoter.AutoVotes.Count != 0)
            {
                // Only set the listbox's datasource if there are elements
                // see: http://stackoverflow.com/a/26762624
                
                modAutoVoterCurVotesListBox.DataSource = modAutoVoterCurrentVotesBindingSource.DataSource;
            }
        }

        /// <summary>
        ///     Sets references to various UI controls that need to be accessed
        ///     from other classes.
        /// </summary>
        private void SetAppWideUiControls()
        {
            _ssb.AppWideUiControls.LogConsoleTextBox = logConsoleTextBox;
            _ssb.AppWideUiControls.StatusBar = statusLabel;
            _ssb.AppWideUiControls.StartMonitoringButton = ssbStartButton;
            _ssb.AppWideUiControls.StopMonitoringButton = ssbStopButton;
        }

        private void ShowErrorMessage(string text, string title)
        {
            MessageBox.Show(text, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowInfoMessage(string text, string title)
        {
            MessageBox.Show(text, title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ssbExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ssbLogo_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == 0)
                return;
            Win32Api.ReleaseCapture();
            Win32Api.SendMessage(Handle, Win32Api.WM_NCLBUTTONDOWN, Win32Api.HT_CAPTION, 0);
        }

        private async void ssbResetButton_Click(object sender, EventArgs e)
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Debug.WriteLine(
                    "User attempted to reset monitoring of server but QL window not found. Won't allow.");
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (_ssb.IsMonitoringServer)
            {
                _ssb.IsMonitoringServer = false;
                _ssb.ServerInfo.Reset();
            }
            else
            {
                await _ssb.BeginMonitoring();
            }
        }

        private async void ssbStartButton_Click(object sender, EventArgs e)
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Debug.WriteLine(
                    "User attempted to start monitoring of server but QL window not found. Won't allow.");
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_ssb.IsMonitoringServer)
            {
                // Do nothing if we're already monitoring
                Debug.WriteLine(
                    "Got user's request to start monitoring, but we're already monitoring the server. Ignoring.");
                MessageBox.Show(@"Already monitoring a Quake Live server!", @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await _ssb.BeginMonitoring();
        }

        private void ssbStopButton_Click(object sender, EventArgs e)
        {
            if (!_ssb.IsMonitoringServer)
            {
                Debug.WriteLine(
                    "SSB was not previously monitoring server; ignoring user's request to stop monitoring.");
                return;
            }
            _ssb.IsMonitoringServer = false;
            _ssb.ServerInfo.Reset();
            Debug.WriteLine("Got user request to stop monitoring server. Stopping monitoring.");
        }

        private void SetAutoVoteOptionalText()
        {
            modAutoVoterContainingDescLabel.Text =
                string.Format("If empty then ALL {0} votes will {1}",
                (modAutoVoterVoteTypeComboxBox.SelectedItem), (string.Format("{0}automatically {1}",
                Environment.NewLine, ((modAutoVoterPassRadioButton.Checked) ? "PASS." : "FAIL."))));
        }
        
        private void modAutoVoterVoteTypeComboxBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        private void modAutoVoterPassRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        private void modAutoVoterRejectRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }
    }
}