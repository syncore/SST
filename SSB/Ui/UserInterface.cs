using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSB.Config;
using SSB.Core;
using SSB.Core.Commands.Modules;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
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
        private readonly EarlyQuitValidator _earlyQuitValidator;
        private readonly EloLimitValidator _eloLimitValidator;
        private readonly MotdValidator _motdValidator;
        private readonly ServerListValidator _serverListValidator;
        private readonly SynServerBot _ssb;
        private List<EarlyQuitter> _earlyQuittersFromDb;

        public UserInterface(SynServerBot ssb)
        {
            InitializeComponent();
            titleBarVersionLabel.Text = string.Format("version {0}",
                typeof (EntryPoint).Assembly.GetName().Version);
            _cfgHandler = new ConfigHandler();
            _ssb = ssb;
            _coreOptionsValidator = new CoreOptionsValidator();
            _accountDateLimitValidator = new AccountDateLimitValidator();
            _earlyQuitValidator = new EarlyQuitValidator();
            _eloLimitValidator = new EloLimitValidator();
            _motdValidator = new MotdValidator();
            _serverListValidator = new ServerListValidator();
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

        private async Task HandleEloLimitModActivation(bool isActiveInUi)
        {
            if (isActiveInUi)
            {
                if (!_ssb.IsMonitoringServer) return;
                _ssb.Mod.EloLimit.Active = true;
                await _ssb.Mod.EloLimit.BatchRemoveEloPlayers();
                Debug.WriteLine(
                    "[UI]: Activating Elo limiter module from UI. Updating old values as necessary.");
            }
            else
            {
                _ssb.Mod.EloLimit.Active = false;
                Debug.WriteLine("[UI]: Deactivating Elo limiter module from UI if active.");
            }
        }

        private void HandleStandardModuleActivation(IModule module, bool isActiveInUi)
        {
            if (isActiveInUi)
            {
                module.Active = true;
                Debug.WriteLine(
                    string.Format("[UI]: Activating {0} module from UI. Updating old values as necessary.",
                        module.ModuleName));

                // Certain modules need to be activated/re-loaded on settings save
                if (module == _ssb.Mod.Motd)
                {
                    _ssb.Mod.Motd.Init();
                }
            }
            else
            {
                module.Active = false;
                Debug.WriteLine(string.Format("[UI]: Deactivating {0} module from UI if active.",
                    module.ModuleName));

                // Certain modules need to be deactivated on settings save
                if (module == _ssb.Mod.Motd)
                {
                    _ssb.Mod.Motd.Deactivate();
                }
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
                await HandleAccountDateModActivation(acctDateOptions.isActive, minAccountAge);
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
            HandleStandardModuleActivation(_ssb.Mod.Accuracy, accuracyOptions.isActive);
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
                HandleStandardModuleActivation(_ssb.Mod.Accuracy, accuracyOptions.isActive);
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

            modAutoVoterCurrentVotesBindingSource.Add(new AutoVote(fullVoteText,
                containsParam, intendedResult, addedByAdmin));

            RefreshCurrentVotesDataSource();
            modAutoVoterCurVotesListBox.SelectedIndex = ((modAutoVoterCurrentVotesBindingSource.Count > 0)
                ? 0
                : -1);
            modAutoVoterVoteTypeComboxBox.SelectedIndex = 0;
            modAutoVoterContainingTextBox.Clear();

            Debug.WriteLine("[UI]: Owner {0} added auto {1} vote for: {2}",
                addedByAdmin, ((intendedResult == IntendedVoteResult.No) ? "NO" : "YES"),
                fullVoteText);
        }

        private void modAutoVoterClearVotesButton_Click(object sender, EventArgs e)
        {
            modAutoVoterCurrentVotesBindingSource.Clear();
            modAutoVoterEnableCheckBox.Checked = false;
            HandleStandardModuleActivation(_ssb.Mod.AutoVoter, modAutoVoterEnableCheckBox.Checked);
            modAutoVoterCurVotesListBox.SelectedIndex = ((modAutoVoterCurrentVotesBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentVotesDataSource();

            // Disable auto voter since there are now no votes
            _cfgHandler.Config.AutoVoterOptions.isActive = false;
            _cfgHandler.Config.AutoVoterOptions.autoVotes = new List<AutoVote>();
            _cfgHandler.WriteConfiguration();

            Debug.WriteLine("[UI]: All automatic votes were cleared by owner.");
        }

        private void modAutoVoterDelVoteButton_Click(object sender, EventArgs e)
        {
            if (modAutoVoterCurrentVotesBindingSource.Count == 0 ||
                modAutoVoterCurVotesListBox.SelectedIndex == -1) return;

            var selectedVote = (AutoVote) modAutoVoterCurVotesListBox.SelectedItem;
            modAutoVoterCurrentVotesBindingSource.Remove(selectedVote);
            Debug.WriteLine("[UI]: Owner removed auto {0} vote: {1}",
                ((selectedVote.IntendedResult == IntendedVoteResult.No) ? "NO" : "YES"),
                selectedVote.VoteText);

            // Set appropriate index to prevent weird ArgumentOutOfRangeException when
            //re-binding the datasource (RefreshCurrentVotesDataSource())
            modAutoVoterCurVotesListBox.SelectedIndex = ((modAutoVoterCurrentVotesBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentVotesDataSource();
        }

        private void modAutoVoterLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAutoVoterUi();
            ShowInfoMessage("Auto voter display settings loaded.", "Settings Loaded");
        }

        private void modAutoVoterPassRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        private void modAutoVoterRefreshVotesButton_Click(object sender, EventArgs e)
        {
            RefreshCurrentVotesDataSource();
            Debug.WriteLine("[UI]: Received user request to refresh current auto votes data source.");
        }

        private void modAutoVoterRejectRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        private void modAutoVoterResetSettingsButton_Click(object sender, EventArgs e)
        {
            var autoVoterOptions = _cfgHandler.Config.AutoVoterOptions;
            autoVoterOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModAutoVoterUi();
            HandleStandardModuleActivation(_ssb.Mod.AutoVoter, autoVoterOptions.isActive);
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
                HandleStandardModuleActivation(_ssb.Mod.AutoVoter, autoVoterOptions.isActive);
                ShowInfoMessage("Auto voter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modAutoVoterVoteTypeComboxBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        private async void modEarlyQuitClearQuitsButton_Click(object sender, EventArgs e)
        {
            var earlyQuitDb = new DbQuits();

            // Initial read of database
            RefreshCurrentQuittersDataSource();

            foreach (var p in modEarlyQuitCurrentQuitBindingSource.List)
            {
                var player = (EarlyQuitter) p;
                earlyQuitDb.DeleteUserFromDb(player.Name);
                await earlyQuitDb.RemoveQuitRelatedBan(_ssb, player.Name);
            }

            // ...
            modEarlyQuitCurQuitsListBox.SelectedIndex = ((modEarlyQuitCurrentQuitBindingSource.Count > 0)
                ? 0
                : -1);

            // Update
            RefreshCurrentQuittersDataSource();
            Debug.WriteLine("[UI]: Cleared early quit database.");
        }

        private async void modEarlyQuitDelQuitButton_Click(object sender, EventArgs e)
        {
            if (modEarlyQuitCurQuitsListBox.SelectedIndex == -1) return;
            var player = (EarlyQuitter) modEarlyQuitCurQuitsListBox.SelectedItem;
            var earlyQuitDb = new DbQuits();
            // Might've been removed in-game
            if (!earlyQuitDb.UserExistsInDb(player.Name))
            {
                RefreshCurrentQuittersDataSource();
                return;
            }
            earlyQuitDb.DeleteUserFromDb(player.Name);
            await earlyQuitDb.RemoveQuitRelatedBan(_ssb, player.Name);
            Debug.WriteLine(string.Format("[UI]: Removed {0} from early quit database",
                player.Name));

            // ...
            modEarlyQuitCurQuitsListBox.SelectedIndex = ((modEarlyQuitCurrentQuitBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentQuittersDataSource();
        }

        private async void modEarlyQuitForgiveQuitButton_Click(object sender, EventArgs e)
        {
            if (modEarlyQuitCurQuitsListBox.SelectedIndex == -1) return;
            var player = (EarlyQuitter) modEarlyQuitCurQuitsListBox.SelectedItem;
            var earlyQuitDb = new DbQuits();
            // Might've been removed in-game
            if (!earlyQuitDb.UserExistsInDb(player.Name))
            {
                RefreshCurrentQuittersDataSource();
                return;
            }
            if (player.QuitCount == 1)
            {
                earlyQuitDb.DeleteUserFromDb(player.Name);
                await earlyQuitDb.RemoveQuitRelatedBan(_ssb, player.Name);
                RefreshCurrentQuittersDataSource();
                return;
            }

            earlyQuitDb.DecrementUserQuitCount(player.Name, 1);
            Debug.WriteLine(string.Format("[UI]: Forgave 1 early quit for {0}",
                player.Name));
            RefreshCurrentQuittersDataSource();
        }

        private void modEarlyQuitLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModEarlyQuitUi();
            ShowInfoMessage("Early quit banner settings loaded.", "Settings Loaded");
        }

        private void modEarlyQuitMaxQuitsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEarlyQuitMaxQuitsTextBox, string.Empty);
        }

        private void modEarlyQuitMaxQuitsTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_earlyQuitValidator.IsValidMaximumNumQuits(modEarlyQuitMaxQuitsTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modEarlyQuitMaxQuitsTextBox.Select(0, modEarlyQuitMaxQuitsTextBox.Text.Length);
                errorProvider.SetError(modEarlyQuitMaxQuitsTextBox, errorMsg);
            }
        }

        private void modEarlyQuitRefreshQuitsButton_Click(object sender, EventArgs e)
        {
            RefreshCurrentQuittersDataSource();
            Debug.WriteLine("[UI]: Received user request to refresh current quitters data source.");
        }

        private void modEarlyQuitResetSettingsButton_Click(object sender, EventArgs e)
        {
            var earlyQuitOptions = _cfgHandler.Config.EarlyQuitOptions;
            earlyQuitOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModEarlyQuitUi();
            HandleStandardModuleActivation(_ssb.Mod.EarlyQuit, earlyQuitOptions.isActive);
            ShowInfoMessage("Early quit banner settings were reset to their default values.",
                "Defaults Loaded");
        }

        private void modEarlyQuitSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var earlyQuitOptions = _cfgHandler.Config.EarlyQuitOptions;
                earlyQuitOptions.isActive = modEarlyQuitEnableCheckBox.Checked;
                earlyQuitOptions.maxQuitsAllowed = uint.Parse(modEarlyQuitMaxQuitsTextBox.Text);
                earlyQuitOptions.banTime = double.Parse(modEarlyQuitTimeTextBox.Text);
                earlyQuitOptions.banTimeScale = (string) modEarlyQuitTimeScaleComboxBox.SelectedItem;
                earlyQuitOptions.banTimeScaleIndex = modEarlyQuitTimeScaleComboxBox.SelectedIndex;
                _cfgHandler.WriteConfiguration();
                // Go into effect now
                _ssb.Mod.EarlyQuit.MaxQuitsAllowed = earlyQuitOptions.maxQuitsAllowed;
                _ssb.Mod.EarlyQuit.BanTime = earlyQuitOptions.banTime;
                _ssb.Mod.EarlyQuit.BanTimeScale = earlyQuitOptions.banTimeScale;
                _ssb.Mod.EarlyQuit.BanTimeScaleIndex = earlyQuitOptions.banTimeScaleIndex;
                
                HandleStandardModuleActivation(_ssb.Mod.EarlyQuit, earlyQuitOptions.isActive);
                ShowInfoMessage("Early quit banner settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modEarlyQuitTimeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEarlyQuitTimeTextBox, string.Empty);
        }

        private void modEarlyQuitTimeTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_earlyQuitValidator.IsValidTimeBanNum(modEarlyQuitTimeTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modEarlyQuitTimeTextBox.Select(0, modEarlyQuitTimeTextBox.Text.Length);
                errorProvider.SetError(modEarlyQuitTimeTextBox, errorMsg);
            }
        }

        private void modEloLimiterLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModEloLimiterUi();
            ShowInfoMessage("Elo limiter settings loaded.", "Settings Loaded");
        }

        private void modEloLimiterMaxEloTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEloLimiterMaxEloTextBox, string.Empty);
        }

        private void modEloLimiterMaxEloTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_eloLimitValidator.IsValidMaximumElo(modEloLimiterMaxEloTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modEloLimiterMaxEloTextBox.Select(0, modEloLimiterMaxEloTextBox.Text.Length);
                errorProvider.SetError(modEloLimiterMaxEloTextBox, errorMsg);
            }
        }

        private void modEloLimiterMinEloTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEloLimiterMinEloTextBox, string.Empty);
        }

        private void modEloLimiterMinEloTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_eloLimitValidator.IsValidMinimumElo(modEloLimiterMinEloTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modEloLimiterMinEloTextBox.Select(0, modEloLimiterMinEloTextBox.Text.Length);
                errorProvider.SetError(modEloLimiterMinEloTextBox, errorMsg);
            }
        }

        private async void modEloLimiterResetSettingsButton_Click(object sender, EventArgs e)
        {
            var eloLimitOptions = _cfgHandler.Config.EloLimitOptions;
            eloLimitOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModEloLimiterUi();
            await HandleEloLimitModActivation(eloLimitOptions.isActive);
            ShowInfoMessage("Elo limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        private async void modEloLimiterSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var maxElo = 0;
                var minElo = int.Parse(modEloLimiterMinEloTextBox.Text);
                // if maximum elo is specified, need to do one more check
                if (modEloLimiterMaxEloTextBox.Text.Length != 0)
                {
                    maxElo = int.Parse(modEloLimiterMaxEloTextBox.Text);
                }
                if ((maxElo != 0) && (maxElo < minElo))
                {
                    ShowErrorMessage("The maximum elo value cannot be less than the minimum Elo value.",
                        "Errors Detected");
                    modEloLimiterMaxEloTextBox.Clear();
                    return;
                }

                var eloLimitOptions = _cfgHandler.Config.EloLimitOptions;
                eloLimitOptions.isActive = modEloLimiterEnableCheckBox.Checked;
                eloLimitOptions.minimumRequiredElo = minElo;
                eloLimitOptions.maximumRequiredElo = ((modEloLimiterMaxEloTextBox.Text.Length == 0)
                    ? 0
                    : maxElo);
                _cfgHandler.WriteConfiguration();
                
                // Go into effect now
                EloLimit.MinimumRequiredElo = eloLimitOptions.minimumRequiredElo;
                EloLimit.MaximumRequiredElo = eloLimitOptions.maximumRequiredElo;
                
                await HandleEloLimitModActivation(eloLimitOptions.isActive);
                ShowInfoMessage("Elo limiter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modMOTDLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModMotdUi();
            ShowInfoMessage("MOTD settings loaded.", "Settings Loaded");
        }

        private void modMOTDRepeatMsgTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modMOTDRepeatMsgTextBox, string.Empty);
        }

        private void modMOTDRepeatMsgTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_motdValidator.IsValidRepeatMessage(modMOTDRepeatMsgTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modMOTDRepeatMsgTextBox.Select(0, modMOTDRepeatMsgTextBox.Text.Length);
                errorProvider.SetError(modMOTDRepeatMsgTextBox, errorMsg);
            }
        }

        private void modMOTDRepeatTimeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modMOTDRepeatTimeTextBox, string.Empty);
        }

        private void modMOTDRepeatTimeTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_motdValidator.IsValidRepeatTime(modMOTDRepeatTimeTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modMOTDRepeatTimeTextBox.Select(0, modMOTDRepeatTimeTextBox.Text.Length);
                errorProvider.SetError(modMOTDRepeatTimeTextBox, errorMsg);
            }
        }

        private void modMOTDResetSettingsButton_Click(object sender, EventArgs e)
        {
            var motdOptions = _cfgHandler.Config.MotdOptions;
            motdOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModMotdUi();
            HandleStandardModuleActivation(_ssb.Mod.Motd, motdOptions.isActive);
            ShowInfoMessage("Account date limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        private void modMOTDSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var motdOptions = _cfgHandler.Config.MotdOptions;
                motdOptions.isActive = modMOTDEnableCheckBox.Checked;
                motdOptions.repeatInterval = int.Parse(modMOTDRepeatTimeTextBox.Text);
                motdOptions.message = modMOTDRepeatMsgTextBox.Text;
                _cfgHandler.WriteConfiguration();
                HandleStandardModuleActivation(_ssb.Mod.Motd, motdOptions.isActive);
                ShowInfoMessage("MOTD settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modServerListLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModServerListUi();
            ShowInfoMessage("Server list settings loaded.", "Settings Loaded");
        }

        private void modServerListMaxServersTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modServerListMaxServersTextBox, string.Empty);
        }

        private void modServerListMaxServersTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_serverListValidator.IsValidMaximumServersNum(modServerListMaxServersTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modServerListMaxServersTextBox.Select(0, modServerListMaxServersTextBox.Text.Length);
                errorProvider.SetError(modServerListMaxServersTextBox, errorMsg);
            }
        }

        private void modServerListResetSettingsButton_Click(object sender, EventArgs e)
        {
            var serverListOptions = _cfgHandler.Config.ServersOptions;
            serverListOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateModServerListUi();
            HandleStandardModuleActivation(_ssb.Mod.Servers, serverListOptions.isActive);
            ShowInfoMessage("Server list settings were reset to their default values.",
                "Defaults Loaded");
        }

        private void modServerListSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var serverListOptions = _cfgHandler.Config.ServersOptions;
                serverListOptions.isActive = modServerListEnableCheckBox.Checked;
                serverListOptions.maxServers = int.Parse(modServerListMaxServersTextBox.Text);
                serverListOptions.timeBetweenQueries = double.Parse(modServerListTimeBetweenTextBox.Text);
                _cfgHandler.WriteConfiguration();
                // Go into effect now
                _ssb.Mod.Servers.TimeBetweenQueries = serverListOptions.timeBetweenQueries;
                _ssb.Mod.Servers.MaxServersToDisplay = serverListOptions.maxServers;

                HandleStandardModuleActivation(_ssb.Mod.Servers, serverListOptions.isActive);
                ShowInfoMessage("Server list  settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        private void modServerListTimeBetweenTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modServerListTimeBetweenTextBox, string.Empty);
        }

        private void modServerListTimeBetweenTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_serverListValidator.IsValidTimeBetweenQueries(modServerListTimeBetweenTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modServerListTimeBetweenTextBox.Select(0, modServerListTimeBetweenTextBox.Text.Length);
                errorProvider.SetError(modServerListTimeBetweenTextBox, errorMsg);
            }
        }

        private void moduleTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;
            var currentTabPage = tabControl.SelectedTab;

            // The modules tabs' data are populated on initial load, but might've changed due to
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
                PopulateModEarlyQuitUi();
            }
            else if (currentTabPage == eloLimitTab)
            {
                PopulateModEloLimiterUi();
            }
            else if (currentTabPage == ircTab)
            {
            }
            else if (currentTabPage == motdTab)
            {
                PopulateModMotdUi();
            }
            else if (currentTabPage == pickupTab)
            {
            }
            else if (currentTabPage == serversTab)
            {
                PopulateModServerListUi();
            }
        }

        private void PopulateAllUiTabs()
        {
            PopulateCoreOptionsUi();
            PopulateModAccountDateUi();
            PopulateModAccuracyUi();
            PopulateModAutoVoterUi();
            PopulateModEarlyQuitUi();
            PopulateModEloLimiterUi();
            PopulateModMotdUi();
            PopulateModServerListUi();
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
            modAutoVoterEnableCheckBox.Checked = _cfgHandler.Config.AutoVoterOptions.isActive;

            // Radio buttons
            modAutoVoterPassRadioButton.Checked = true;
            // Vote types combo box
            modAutoVoterVoteTypeComboxBox.DisplayMember = "Name";
            modAutoVoterVoteTypeComboxBox.ValueMember = "Name";
            modAutoVoterVoteTypeComboxBox.DataSource = _ssb.Mod.AutoVoter.ValidCallVotes;
            modAutoVoterVoteTypeComboxBox.SelectedIndex = 0;
            // Current votes listbox
            RefreshCurrentVotesDataSource();
            Debug.WriteLine("Populated auto voter module user interface.");
        }

        private void PopulateModEarlyQuitUi()
        {
            _cfgHandler.ReadConfiguration();
            var earlyQuitOptions = _cfgHandler.Config.EarlyQuitOptions;
            modEarlyQuitEnableCheckBox.Checked = earlyQuitOptions.isActive;
            modEarlyQuitMaxQuitsTextBox.Text = earlyQuitOptions.maxQuitsAllowed.ToString();
            modEarlyQuitTimeTextBox.Text = earlyQuitOptions.banTime.ToString(CultureInfo.InvariantCulture);
            modEarlyQuitTimeScaleComboxBox.DataSource = Helpers.ValidTimeScales;
            modEarlyQuitTimeScaleComboxBox.SelectedIndex = earlyQuitOptions.banTimeScaleIndex;
            // Current early quitters listbox
            RefreshCurrentQuittersDataSource();
            Debug.WriteLine("Populated early quit banner module user interface.");
        }

        private void PopulateModEloLimiterUi()
        {
            _cfgHandler.ReadConfiguration();
            var eloLimitOptions = _cfgHandler.Config.EloLimitOptions;
            modEloLimiterEnableCheckBox.Checked = eloLimitOptions.isActive;
            modEloLimiterMinEloTextBox.Text = eloLimitOptions.minimumRequiredElo.ToString();
            modEloLimiterMaxEloTextBox.Text = ((eloLimitOptions.maximumRequiredElo == 0)
                ? string.Empty
                : eloLimitOptions.maximumRequiredElo.ToString());
            Debug.WriteLine("Populated Elo limiter module user interface.");
        }

        private void PopulateModMotdUi()
        {
            _cfgHandler.ReadConfiguration();
            var motdOptions = _cfgHandler.Config.MotdOptions;
            modMOTDEnableCheckBox.Checked = motdOptions.isActive;
            modMOTDRepeatTimeTextBox.Text = motdOptions.repeatInterval.ToString();
            modMOTDRepeatMsgTextBox.Text = motdOptions.message;
            Debug.WriteLine("Populated MOTD module user interface.");
        }

        private void PopulateModServerListUi()
        {
            _cfgHandler.ReadConfiguration();
            var serverListOptions = _cfgHandler.Config.ServersOptions;
            modServerListEnableCheckBox.Checked = serverListOptions.isActive;
            modServerListMaxServersTextBox.Text = serverListOptions.maxServers.ToString();
            modServerListTimeBetweenTextBox.Text = serverListOptions.timeBetweenQueries.ToString(
                CultureInfo.InvariantCulture);
            Debug.WriteLine("Populated server list module user interface.");
        }

        private void RefreshCurrentQuittersDataSource()
        {
            modEarlyQuitCurQuitsListBox.DataSource = null;
            modEarlyQuitCurQuitsListBox.DisplayMember = "EarlyQuitFormatDisplay";
            modEarlyQuitCurQuitsListBox.ValueMember = "Name";
            var earlyQuitDb = new DbQuits();
            _earlyQuittersFromDb = earlyQuitDb.GetAllQuitters();
            modEarlyQuitCurrentQuitBindingSource.DataSource =
                new BindingList<EarlyQuitter>(_earlyQuittersFromDb);
            if (modEarlyQuitCurrentQuitBindingSource.Count != 0)
            {
                modEarlyQuitCurQuitsListBox.DataSource = modEarlyQuitCurrentQuitBindingSource.DataSource;
            }
        }

        private void RefreshCurrentVotesDataSource()
        {
            modAutoVoterCurVotesListBox.DataSource = null;
            modAutoVoterCurVotesListBox.DisplayMember = "VoteFormatDisplay";
            modAutoVoterCurVotesListBox.ValueMember = "VoteText";
            modAutoVoterCurrentVotesBindingSource.DataSource =
                new BindingList<AutoVote>(_ssb.Mod.AutoVoter.AutoVotes);
            if (modAutoVoterCurrentVotesBindingSource.Count != 0)
            {
                // Only set the listbox's datasource if there are elements
                // otherwise, ArgumentOutOfRange is unfortunately possible
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

        private void SetAutoVoteOptionalText()
        {
            modAutoVoterContainingDescLabel.Text =
                string.Format("If empty then ALL {0} votes will {1}",
                    (modAutoVoterVoteTypeComboxBox.SelectedItem), (string.Format("{0}automatically {1}",
                        Environment.NewLine, ((modAutoVoterPassRadioButton.Checked) ? "PASS." : "FAIL."))));
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
            {
                return;
            }
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

            Debug.WriteLine("Got user request to reset server monitoring; Stopping if exists," +
                            " starting if it doesn't.");

            if (_ssb.IsMonitoringServer)
            {
                _ssb.StopMonitoring();
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

            _ssb.StopMonitoring();
            Debug.WriteLine("Got user request to stop monitoring server. Stopping monitoring.");
        }
    }
}