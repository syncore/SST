using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSB.Config;
using SSB.Core;
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
            _cfgHandler = new ConfigHandler();
            _ssb = ssb;
            _coreOptionsValidator = new CoreOptionsValidator();
            _accountDateLimitValidator = new AccountDateLimitValidator();
            SetAppWideUiControls();
            PopulateAllUiTabs();
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
            // TODO: only allow if we're monitoring the server
            if (isActiveInUi)
            {
                await _ssb.Mod.AccountDateLimit.EnableAccountDateLimiter(minAccountAge);
                Debug.WriteLine("[UI]: Activating account date limiter module from UI. Updating old values as necessary.");
            }
            else
            {
                _ssb.Mod.AccountDateLimit.Active = false;
                Debug.WriteLine("[UI]: Deactivating account date limiter module from UI if active.");
            }
        }

        private void HandleAccuracyModActivation(bool isActiveInUi)
        {
            // TODO: only allow if we're monitoring the server
            if (isActiveInUi)
            {
                _ssb.Mod.Accuracy.Active = true;
                Debug.WriteLine("[UI]: Activating accuracy display module from UI. Updating old values as necessary.");
            }
            else
            {
                _ssb.Mod.Accuracy.Active = false;
                Debug.WriteLine("[UI]: Deactivating accuracy display module from UI if active.");
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
            if (!_accountDateLimitValidator.IsValidMinimumAccountAge(modAccDateAccAgeTextBox.Text, out errorMsg))
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
            modAutoVoterVoteTypeBindingSource.DataSource = _ssb.Mod.AutoVoter.ValidCallVotes;
            modAutoVoterVoteTypeComboxBox.DataSource = modAutoVoterVoteTypeBindingSource.DataSource;
            modAutoVoterVoteTypeComboxBox.DisplayMember = "Name";
            modAutoVoterVoteTypeComboxBox.ValueMember = "Type";
        }

        /// <summary>
        ///     Sets references to GUI controls so they can be used from other classes.
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
                Debug.WriteLine("User attempted to reset monitoring of server but QL window not found. Won't allow.");
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
                Debug.WriteLine("User attempted to start monitoring of server but QL window not found. Won't allow.");
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (_ssb.IsMonitoringServer)
            {
                // Do nothing if we're already monitoring
                Debug.WriteLine("Got user's request to start monitoring, but we're already monitoring the server. Ignoring.");
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
                Debug.WriteLine("SSB was not previously monitoring server; ignoring user's request to stop monitoring.");
                return;
            }
            _ssb.IsMonitoringServer = false;
            _ssb.ServerInfo.Reset();
            Debug.WriteLine("Got user request to stop monitoring server. Stopping monitoring.");
        }
    }
}