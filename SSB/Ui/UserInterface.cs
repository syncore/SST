﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSB.Config;
using SSB.Config.Core;
using SSB.Core;
using SSB.Database;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Ui.Validation;
using SSB.Ui.Validation.Modules;
using SSB.Util;

namespace SSB.Ui
{
    /// <summary>
    ///     The user interface class.
    /// </summary>
    public partial class UserInterface : Form
    {
        private readonly AccountDateLimitValidator _accountDateLimitValidator;
        private readonly ConfigHandler _cfgHandler;
        private readonly CoreOptionsValidator _coreOptionsValidator;
        private readonly EarlyQuitValidator _earlyQuitValidator;
        private readonly EloLimitValidator _eloLimitValidator;
        private readonly IrcValidator _ircValidator;
        private readonly MotdValidator _motdValidator;
        private readonly PickupValidator _pickupValidator;
        private readonly ServerListValidator _serverListValidator;
        private readonly SynServerBot _ssb;
        private List<BanInfo> _bansFromDb;
        private List<EarlyQuitter> _earlyQuittersFromDb;
        private List<User> _usersFromDb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInterface" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        public UserInterface(SynServerBot ssb)
        {
            InitializeComponent();
            titleBarVersionLabel.Text = string.Format("version {0}",
                Helpers.GetVersion());
            sysTrayIcon.Text = string.Format("SSB v{0}",
                Helpers.GetVersion());
            sysTrayIcon.Visible = false;
            _cfgHandler = new ConfigHandler();
            _ssb = ssb;
            _coreOptionsValidator = new CoreOptionsValidator();
            _accountDateLimitValidator = new AccountDateLimitValidator();
            _earlyQuitValidator = new EarlyQuitValidator();
            _eloLimitValidator = new EloLimitValidator();
            _motdValidator = new MotdValidator();
            _serverListValidator = new ServerListValidator();
            _pickupValidator = new PickupValidator();
            _ircValidator = new IrcValidator(_ssb.Mod.Irc.IrcManager.ValidIrcNickRegex);
            SetAppWideUiControls();
            PopulateAllUiTabs();
            _ssb.UserInterface = this;
        }

        /// <summary>
        ///     Populates the ban management UI tab.
        /// </summary>
        public void PopulateBanManagementUi()
        {
            banMBanDurationScaleComboBox.InvokeIfRequired(c =>
            {
                c.DataSource = Helpers.ValidTimeScales;
                c.SelectedIndex = 0;
            });
            // Current bans listbox
            RefreshCurrentBansDataSource();
            Debug.WriteLine("[UI]: Populated ban management user interface.");
        }

        /// <summary>
        ///     Populates the account date module UI tab.
        /// </summary>
        public void PopulateModAccountDateUi()
        {
            _cfgHandler.ReadConfiguration();
            var acctDateOptions = _cfgHandler.Config.AccountDateOptions;
            modAccDateEnableCheckBox.InvokeIfRequired(c => { c.Checked = acctDateOptions.isActive; });
            modAccDateAccAgeTextBox.InvokeIfRequired(
                c => { c.Text = acctDateOptions.minimumDaysRequired.ToString(); });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated account date limiter module user interface.");
        }

        /// <summary>
        ///     Populates the accuracy module UI tab.
        /// </summary>
        public void PopulateModAccuracyUi()
        {
            _cfgHandler.ReadConfiguration();
            modAccuracyEnableCheckBox.InvokeIfRequired(
                c => { c.Checked = _cfgHandler.Config.AccuracyOptions.isActive; });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated accuracy display module user interface.");
        }

        /// <summary>
        ///     Populates the automatic voter module UI tab.
        /// </summary>
        public void PopulateModAutoVoterUi()
        {
            _cfgHandler.ReadConfiguration();
            modAutoVoterEnableCheckBox.InvokeIfRequired(
                c => { c.Checked = _cfgHandler.Config.AutoVoterOptions.isActive; });

            // Radio buttons
            modAutoVoterPassRadioButton.InvokeIfRequired(c => { c.Checked = true; });
            // Vote types combo box
            modAutoVoterVoteTypeComboxBox.InvokeIfRequired(c =>
            {
                c.DisplayMember = "Name";
                c.ValueMember = "Name";
                c.DataSource = _ssb.Mod.AutoVoter.ValidCallVotes;
                c.SelectedIndex = 0;
            });
            // Current votes listbox
            RefreshCurrentVotesDataSource();
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated auto voter module user interface.");
        }

        /// <summary>
        ///     Populates the early quit module UI tab.
        /// </summary>
        public void PopulateModEarlyQuitUi()
        {
            _cfgHandler.ReadConfiguration();
            var earlyQuitOptions = _cfgHandler.Config.EarlyQuitOptions;
            modEarlyQuitEnableCheckBox.InvokeIfRequired(c => { c.Checked = earlyQuitOptions.isActive; });
            modEarlyQuitMaxQuitsTextBox.InvokeIfRequired(
                c => { c.Text = earlyQuitOptions.maxQuitsAllowed.ToString(); });
            modEarlyQuitTimeTextBox.InvokeIfRequired(
                c => { c.Text = earlyQuitOptions.banTime.ToString(CultureInfo.InvariantCulture); });
            modEarlyQuitTimeScaleComboxBox.InvokeIfRequired(c =>
            {
                c.DataSource = Helpers.ValidTimeScales;
                c.SelectedIndex = earlyQuitOptions.banTimeScaleIndex;
            });

            // Current early quitters listbox
            RefreshCurrentQuittersDataSource();
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated early quit banner module user interface.");
        }

        /// <summary>
        ///     Populates the Elo limiter module UI tab.
        /// </summary>
        public void PopulateModEloLimiterUi()
        {
            _cfgHandler.ReadConfiguration();
            var eloLimitOptions = _cfgHandler.Config.EloLimitOptions;
            modEloLimiterEnableCheckBox.InvokeIfRequired(c => { c.Checked = eloLimitOptions.isActive; });
            modEloLimiterMinEloTextBox.InvokeIfRequired(
                c => { c.Text = eloLimitOptions.minimumRequiredElo.ToString(); });
            modEloLimiterMaxEloTextBox.InvokeIfRequired(c =>
            {
                c.Text = ((eloLimitOptions.maximumRequiredElo == 0)
                    ? string.Empty
                    : eloLimitOptions.maximumRequiredElo.ToString());
            });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated Elo limiter module user interface.");
        }

        /// <summary>
        ///     Populates the IRC module UI tab.
        /// </summary>
        public void PopulateModIrcUi()
        {
            var ircOptions = _cfgHandler.Config.IrcOptions;
            modIRCEnableCheckBox.InvokeIfRequired(c => { c.Checked = ircOptions.isActive; });
            modIRCAdminNameTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircAdminNickname; });
            modIRCBotNickNameTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircNickName; });
            modIRCBotUserNameTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircUserName; });
            modIRCQNetUserNameTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircNickServiceUsername; });
            modIRCQNetPassTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircNickServicePassword; });
            modIRCQNetAutoAuthCheckBox.InvokeIfRequired(
                c => { c.Checked = ircOptions.autoAuthWithNickService; });
            modIRCQNetHideHostCheckBox.InvokeIfRequired(
                c => { c.Checked = ircOptions.hideHostnameOnQuakeNet; });
            modIRCServerAddressTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircServerAddress; });
            modIRCServerPortTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircServerPort.ToString(); });
            modIRCServerPassTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircServerPassword; });
            modIRCChannelTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircChannel; });
            modIRCChannelKeyTextBox.InvokeIfRequired(c => { c.Text = ircOptions.ircChannelKey; });
            modIRCAutoConnectCheckBox.InvokeIfRequired(c => { c.Checked = ircOptions.autoConnectOnStart; });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated IRC module user interface.");
        }

        /// <summary>
        ///     Populates the MOTD module UI tab.
        /// </summary>
        public void PopulateModMotdUi()
        {
            _cfgHandler.ReadConfiguration();
            var motdOptions = _cfgHandler.Config.MotdOptions;
            modMOTDEnableCheckBox.InvokeIfRequired(c => { c.Checked = motdOptions.isActive; });
            modMOTDRepeatTimeTextBox.InvokeIfRequired(c => { c.Text = motdOptions.repeatInterval.ToString(); });
            modMOTDRepeatMsgTextBox.InvokeIfRequired(c => { c.Text = motdOptions.message; });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated MOTD module user interface.");
        }

        /// <summary>
        ///     Populates the pickup module UI tab.
        /// </summary>
        public void PopulateModPickupUi()
        {
            _cfgHandler.ReadConfiguration();
            var pickupOptions = _cfgHandler.Config.PickupOptions;
            modPickupEnableCheckBox.InvokeIfRequired(c => { c.Checked = pickupOptions.isActive; });
            modPickupMaxSubsTextBox.InvokeIfRequired(
                c => { c.Text = pickupOptions.maxSubsPerPlayer.ToString(); });
            modPickupMaxNoShowsTextBox.InvokeIfRequired(
                c => { c.Text = pickupOptions.maxNoShowsPerPlayer.ToString(); });
            modPickupPlayersPerTeamTextBox.InvokeIfRequired(
                c => { c.Text = pickupOptions.teamSize.ToString(); });
            modPickupNoShowsTimeBanTextBox.InvokeIfRequired(c =>
            {
                c.Text = pickupOptions.excessiveNoShowBanTime.
                    ToString(CultureInfo.InvariantCulture);
            });
            modPickupSubsTimeBanTextBox.InvokeIfRequired(c =>
            {
                c.Text = pickupOptions.excessiveSubUseBanTime.
                    ToString(CultureInfo.InvariantCulture);
            });
            modPickupSubsTimeBanScaleComboBox.InvokeIfRequired(
                c => { c.DataSource = Helpers.ValidTimeScales; });
            modPickupNoShowsTimeBanScaleComboBox.InvokeIfRequired(
                c => { c.DataSource = Helpers.ValidTimeScales; });
            modPickupSubsTimeBanScaleComboBox.InvokeIfRequired(
                c => { c.SelectedIndex = pickupOptions.excessiveSubUseBanTimeScaleIndex; });
            modPickupNoShowsTimeBanScaleComboBox.InvokeIfRequired(c =>
            {
                c.SelectedIndex =
                    pickupOptions.excessiveNoShowBanTimeScaleIndex;
            });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated pickup module user interface.");
        }

        /// <summary>
        ///     Populates the server list module UI tab.
        /// </summary>
        public void PopulateModServerListUi()
        {
            _cfgHandler.ReadConfiguration();
            var serverListOptions = _cfgHandler.Config.ServersOptions;
            modServerListEnableCheckBox.InvokeIfRequired(c => { c.Checked = serverListOptions.isActive; });
            modServerListMaxServersTextBox.InvokeIfRequired(
                c => { c.Text = serverListOptions.maxServers.ToString(); });
            modServerListTimeBetweenTextBox.InvokeIfRequired(c =>
            {
                c.Text = serverListOptions.timeBetweenQueries.ToString(
                    CultureInfo.InvariantCulture);
            });
            UpdateActiveModulesStatusBarText();
            Debug.WriteLine("[UI]: Populated server list module user interface.");
        }

        /// <summary>
        ///     Populates the user management UI tab.
        /// </summary>
        public void PopulateUserManagementUi()
        {
            // Specfically leave out user levels of None and Owner type.
            UserLevel[] levels = { UserLevel.User, UserLevel.SuperUser, UserLevel.Admin };
            usrMUserAccessComboBox.InvokeIfRequired(c => { c.DataSource = levels; });
            usrMUserAccessComboBox.InvokeIfRequired(c => { c.SelectedIndex = 0; });
            // Current SSB users listbox
            RefreshCurrentSsbUsersDataSource();
            Debug.WriteLine("[UI]: Populated user management user interface.");
        }

        /// <summary>
        ///     Refreshes the current bans data source.
        /// </summary>
        public void RefreshCurrentBansDataSource()
        {
            banMCurBansListBox.InvokeIfRequired(c =>
            {
                c.DataSource = null;
                c.DisplayMember = "BanFormatDisplay";
                c.ValueMember = "PlayerName";
            });
            var bansDb = new DbBans();
            bansDb.RemoveExpiredBans();
            _bansFromDb = bansDb.GetAllBans();
            banMCurrentBanBindingSource.DataSource =
                new BindingList<BanInfo>(_bansFromDb);
            if (banMCurrentBanBindingSource.Count != 0)
            {
                banMCurBansListBox.InvokeIfRequired(
                    c => { c.DataSource = banMCurrentBanBindingSource.DataSource; });
            }
        }

        /// <summary>
        ///     Refreshes the current quitters data source.
        /// </summary>
        public void RefreshCurrentQuittersDataSource()
        {
            modEarlyQuitCurQuitsListBox.InvokeIfRequired(c =>
            {
                c.DataSource = null;
                c.DisplayMember = "EarlyQuitFormatDisplay";
                c.ValueMember = "Name";
            });

            var earlyQuitDb = new DbQuits();
            _earlyQuittersFromDb = earlyQuitDb.GetAllQuitters();
            modEarlyQuitCurrentQuitBindingSource.DataSource =
                new BindingList<EarlyQuitter>(_earlyQuittersFromDb);
            if (modEarlyQuitCurrentQuitBindingSource.Count != 0)
            {
                modEarlyQuitCurQuitsListBox.InvokeIfRequired(
                    c => { c.DataSource = modEarlyQuitCurrentQuitBindingSource.DataSource; });
            }
        }

        /// <summary>
        ///     Refreshes the current SSB users data source.
        /// </summary>
        public void RefreshCurrentSsbUsersDataSource()
        {
            usrMCurUsersListBox.InvokeIfRequired(c =>
            {
                c.DataSource = null;
                c.DisplayMember = "UserFormatDisplay";
                c.ValueMember = "Name";
            });
            var userDb = new DbUsers();
            _usersFromDb = userDb.GetAllUsers();
            usrMCurrentUserBindingSource.DataSource =
                new BindingList<User>(_usersFromDb);
            if (usrMCurrentUserBindingSource.Count != 0)
            {
                usrMCurUsersListBox.InvokeIfRequired(
                    c => { c.DataSource = usrMCurrentUserBindingSource.DataSource; });
            }
        }

        /// <summary>
        ///     Refreshes the current votes data source.
        /// </summary>
        public void RefreshCurrentVotesDataSource()
        {
            modAutoVoterCurVotesListBox.InvokeIfRequired(c =>
            {
                c.DataSource = null;
                c.DisplayMember = "VoteFormatDisplay";
                c.ValueMember = "VoteText";
            });

            modAutoVoterCurrentVotesBindingSource.DataSource =
                new BindingList<AutoVote>(_ssb.Mod.AutoVoter.AutoVotes);
            if (modAutoVoterCurrentVotesBindingSource.Count != 0)
            {
                // Only set the listbox's datasource if there are elements
                // otherwise, ArgumentOutOfRange is unfortunately possible
                // see: http://stackoverflow.com/a/26762624
                modAutoVoterCurVotesListBox.InvokeIfRequired(
                    c => { c.DataSource = modAutoVoterCurrentVotesBindingSource.DataSource; });
            }
        }

        /// <summary>
        ///     Handles the Click event of the banMAddBanButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void banMAddBanButton_Click(object sender, EventArgs e)
        {
            // Validate here
            if (banMUserQlNameTextBox.Text.Length == 0 ||
                !Helpers.IsValidQlUsernameFormat(banMUserQlNameTextBox.Text, false))
            {
                ShowErrorMessage("You must specify a valid Quake Live name.",
                    "Invalid QL name");
                return;
            }

            double duration;
            if (banMBanDurationTextBox.Text.Length == 0 ||
                !double.TryParse(banMBanDurationTextBox.Text, out duration) ||
                Math.Abs(duration) <= 0)
            {
                ShowErrorMessage("You must specify a valid duration number.",
                    "Invalid duration");
                return;
            }

            // See if the ban exists
            var banDb = new DbBans();
            var user = banMUserQlNameTextBox.Text;
            if (banDb.UserAlreadyBanned(user))
            {
                ShowErrorMessage(
                    string.Format(
                        "User {0} already exists in the ban database. Remove the user then re-add.",
                        user), "User exists");
                return;
            }
            _cfgHandler.ReadConfiguration();
            var owner = _cfgHandler.Config.CoreOptions.owner;
            var scale = (string)banMBanDurationScaleComboBox.SelectedItem;
            var expiration = ExpirationDateGenerator.GenerateExpirationDate(duration, scale);
            banDb.AddUserToDb(user, owner, DateTime.Now, expiration, BanType.AddedByAdmin);

            // Kickban using QL internal system immediately
            if (_ssb.IsMonitoringServer)
            {
                await _ssb.QlCommands.CustCmdKickban(user);
            }

            RefreshCurrentBansDataSource();
            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);
            banMBanDurationScaleComboBox.SelectedIndex = 0;
            banMBanDurationTextBox.Clear();
            banMUserQlNameTextBox.Clear();

            Debug.WriteLine(
                "[UI]: Owner {0} added ban of length {1} {2} for user: {3} expires on {4} to ban database.",
                owner, duration, scale, user, expiration.ToString("G", DateTimeFormatInfo.InvariantInfo));
        }

        /// <summary>
        ///     Handles the Click event of the banMDelAllBansButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void banMDelAllBansButton_Click(object sender, EventArgs e)
        {
            _cfgHandler.ReadConfiguration();

            var banDb = new DbBans();
            var allBans = banDb.GetAllBans();
            var owner = _cfgHandler.Config.CoreOptions.owner;

            if (allBans.Count == 0)
            {
                Debug.WriteLine(string.Format("[UI]: Owner {0} attempted to clear all bans from ban" +
                                              " database, but no bans exist.", owner));
                ShowErrorMessage("There are no expired bans to remove.",
                    "No expired bans");
                return;
            }

            banMCurrentBanBindingSource.Clear();
            foreach (var ban in allBans)
            {
                banDb.DeleteUserFromDb(ban.PlayerName);
                if (_ssb.IsMonitoringServer)
                {
                    // remove from QL's internal temporary ban system
                    await _ssb.QlCommands.SendToQlAsync(string.Format("unban {0}",
                        ban.PlayerName), false);
                }
            }

            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentBansDataSource();

            Debug.WriteLine(string.Format("[UI]: Owner {0} cleared all bans from the ban database.",
                owner));
        }

        /// <summary>
        ///     Handles the Click event of the banMDelBanButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void banMDelBanButton_Click(object sender, EventArgs e)
        {
            if (banMCurrentBanBindingSource.Count == 0 ||
                banMCurBansListBox.SelectedIndex == -1) return;

            _cfgHandler.ReadConfiguration();

            var banDb = new DbBans();
            var owner = _cfgHandler.Config.CoreOptions.owner;
            var selectedUser = (BanInfo)banMCurBansListBox.SelectedItem;

            banMCurrentBanBindingSource.Remove(selectedUser);
            banDb.DeleteUserFromDb(selectedUser.PlayerName);

            if (_ssb.IsMonitoringServer)
            {
                // remove from QL's internal temporary ban system
                await _ssb.QlCommands.SendToQlAsync(string.Format("unban {0}",
                    selectedUser.PlayerName), false);
            }

            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentBansDataSource();

            Debug.WriteLine(
                "[UI]: Owner {0} removed ban for user: {1} from ban database. Ban originally added: {2} " +
                "was set to expire on {3}",
                owner, selectedUser.PlayerName,
                selectedUser.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                selectedUser.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo));
        }

        /// <summary>
        ///     Handles the Click event of the banMDelExpiredBansButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void banMDelExpiredBansButton_Click(object sender, EventArgs e)
        {
            _cfgHandler.ReadConfiguration();

            var owner = _cfgHandler.Config.CoreOptions.owner;
            var banDb = new DbBans();
            var expiredBans = banDb.GetAllBans().Where
                (b => (b.BanExpirationDate != default(DateTime) &&
                       DateTime.Now > b.BanExpirationDate)).ToList();

            if (expiredBans.Count == 0)
            {
                Debug.WriteLine(
                    string.Format("[UI]: Owner {0} attempted to remove all expired bans from ban" +
                                  " database, but no expired bans exist.", owner));
                ShowErrorMessage("There are no expired bans to remove.",
                    "No expired bans");
                return;
            }

            var bManager = new BanManager(_ssb);

            foreach (var ban in expiredBans)
            {
                await bManager.RemoveBan(ban, false);
            }

            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentBansDataSource();

            Debug.WriteLine("[UI]: Owner {0} cleared all {1} expired bans from the ban database.", owner,
                expiredBans.Count);
        }

        /// <summary>
        ///     Handles the Click event of the closeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Handles the Validated event of the coreAccountNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void coreAccountNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreAccountNameTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the coreAccountNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Validated event of the coreEloCacheTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void coreEloCacheTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreEloCacheTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the coreEloCacheTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the coreLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void coreLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateCoreOptionsUi();
            ShowInfoMessage("Core options settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the coreOwnerNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void coreOwnerNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreOwnerNameTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the coreOwnerNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the coreResetAllButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void coreResetAllButton_Click(object sender, EventArgs e)
        {
            _cfgHandler.RestoreDefaultConfiguration();
            _cfgHandler.ReadConfiguration();

            await HandleAccountDateModActivation(_cfgHandler.Config.AccountDateOptions.isActive,
                _cfgHandler.Config.AccountDateOptions.minimumDaysRequired);
            await HandleEloLimitModActivation(_cfgHandler.Config.EloLimitOptions.isActive);
            await HandlePickupModActivation(_cfgHandler.Config.PickupOptions.isActive);

            HandleMotdModActivation(_cfgHandler.Config.MotdOptions.isActive);
            HandleIrcModActivation(_cfgHandler.Config.IrcOptions.isActive);

            HandleStandardModuleActivation(_ssb.Mod.Accuracy,
                _cfgHandler.Config.AccuracyOptions.isActive);
            HandleStandardModuleActivation(_ssb.Mod.AutoVoter,
                _cfgHandler.Config.AutoVoterOptions.isActive);
            HandleStandardModuleActivation(_ssb.Mod.EarlyQuit,
                _cfgHandler.Config.EarlyQuitOptions.isActive);
            HandleStandardModuleActivation(_ssb.Mod.Servers,
                _cfgHandler.Config.ServersOptions.isActive);

            PopulateAllUiTabs();

            ShowInfoMessage("All SSB settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the coreResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void coreResetSettingsButton_Click(object sender, EventArgs e)
        {
            var coreOptions = _cfgHandler.Config.CoreOptions;
            coreOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            PopulateCoreOptionsUi();
            HandleCoreSettingsUpdate(coreOptions);
            ShowInfoMessage("Core settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the coreSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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
                coreOptions.minimizeToTray = coreMinimizeToTrayCheckBox.Checked;
                coreOptions.owner = coreOwnerNameTextBox.Text;
                _cfgHandler.WriteConfiguration();
                HandleCoreSettingsUpdate(coreOptions);
                UpdateAccountInfoStatusBarText();
                ShowInfoMessage("Core settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the account date module activation.
        /// </summary>
        /// <param name="isActiveInUi">
        ///     if set to <c>true</c> then the module
        ///     is enabled in the UI.
        /// </param>
        /// <param name="minAccountAge">The minimum account age.</param>
        private async Task HandleAccountDateModActivation(bool isActiveInUi, uint minAccountAge)
        {
            _ssb.Mod.AccountDateLimit.Active = isActiveInUi;
            Debug.WriteLine(string.Format("{0}", (isActiveInUi)
                ? "[UI]: Activating account date limiter module from UI. Updating old values as necessary."
                : "[UI]: Deactivating account date limiter module from UI if active."));

            if (!_ssb.IsMonitoringServer) return;
            await _ssb.Mod.AccountDateLimit.EnableAccountDateLimiter(minAccountAge);
        }

        /// <summary>
        ///     Handles the core settings update.
        /// </summary>
        /// <param name="coreOptions">The core options.</param>
        private void HandleCoreSettingsUpdate(CoreOptions coreOptions)
        {
            // Go into effect now
            _ssb.AccountName = coreOptions.accountName;
            // ReSharper disable once UnusedVariable
            // Add the owner (via constructor)
            var userDb = new DbUsers();
        }

        /// <summary>
        ///     Handles the Elo limiter module activation.
        /// </summary>
        /// <param name="isActiveInUi">
        ///     if set to <c>true</c> then the module
        ///     is enabled in the UI.
        /// </param>
        private async Task HandleEloLimitModActivation(bool isActiveInUi)
        {
            _ssb.Mod.EloLimit.Active = isActiveInUi;
            Debug.WriteLine(string.Format("{0}", (isActiveInUi)
                ? "[UI]: Activating Elo limiter module from UI. Updating old values as necessary."
                : "[UI]: Deactivating Elo limiter module from UI if active."));

            if (!_ssb.IsMonitoringServer) return;
            await _ssb.Mod.EloLimit.BatchRemoveEloPlayers();
        }

        /// <summary>
        ///     Handles the IRC module activation.
        /// </summary>
        /// <param name="isActiveInUi">
        ///     if set to <c>true</c> then the module
        ///     is enabled in the UI.
        /// </param>
        private void HandleIrcModActivation(bool isActiveInUi)
        {
            _ssb.Mod.Irc.Active = isActiveInUi;
            Debug.WriteLine(string.Format("{0}", (isActiveInUi)
                ? "[UI]: Activating IRC module from UI. Updating old values as necessary."
                : "[UI]: Deactivating IRC module from UI if active."));

            if (isActiveInUi)
            {
                _ssb.Mod.Irc.Init();
            }
            else
            {
                _ssb.Mod.Irc.Deactivate();
            }
        }

        /// <summary>
        ///     Handles the MOTD module activation.
        /// </summary>
        /// ///
        /// <param name="isActiveInUi">
        ///     if set to <c>true</c> then the module
        ///     is enabled in the UI.
        /// </param>
        private void HandleMotdModActivation(bool isActiveInUi)
        {
            _ssb.Mod.Motd.Active = isActiveInUi;
            Debug.WriteLine(string.Format("{0}", (isActiveInUi)
                ? "[UI]: Activating MOTD module from UI. Updating old values as necessary."
                : "[UI]: Deactivating MOTD module from UI if active."));

            if (isActiveInUi)
            {
                _ssb.Mod.Motd.Init();
            }
            else
            {
                _ssb.Mod.Motd.Deactivate();
            }
        }

        /// <summary>
        ///     Handles the pickup module activation.
        /// </summary>
        /// ///
        /// <param name="isActiveInUi">
        ///     if set to <c>true</c> then the module
        ///     is enabled in the UI.
        /// </param>
        private async Task HandlePickupModActivation(bool isActiveInUi)
        {
            _ssb.Mod.Pickup.Active = isActiveInUi;
            Debug.WriteLine(string.Format("{0}", (isActiveInUi)
                ? "[UI]: Activating pickup module from UI. Updating old values as necessary."
                : "[UI]: Deactivating pickup module from UI if active."));

            if (!isActiveInUi)
            {
                _ssb.Mod.Pickup.Active = false;
                if (_ssb.IsMonitoringServer)
                {
                    _ssb.Mod.Pickup.Manager.ResetPickupStatus();
                    await _ssb.QlCommands.SendToQlAsync("unlock", false);
                }
            }
        }

        /// <summary>
        ///     Handles the standard module activation.
        /// </summary>
        /// <param name="module">The module.</param>
        /// ///
        /// <param name="isActiveInUi">
        ///     if set to <c>true</c> then the module
        ///     is enabled in the UI.
        /// </param>
        /// <remarks>
        /// This is the module activation method used for modules that do not
        /// require any special actions (i.e. initilization methods)
        /// to occur when being enabled or and do not require async.
        /// Currently these are: acc, autovoter, earlyquit, servers.
        /// </remarks>
        private void HandleStandardModuleActivation(IModule module, bool isActiveInUi)
        {
            module.Active = isActiveInUi;
            Debug.WriteLine(string.Format("{0}", (isActiveInUi)
                ? string.Format("[UI]: Activating {0} module from UI. Updating old values as necessary.",
                    module.ModuleName)
                : string.Format("[UI]: Deactivating {0} module from UI if active.",
                    module.ModuleName)));
        }

        /// <summary>
        ///     Handles the Click event of the minimizeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void minimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            _cfgHandler.ReadConfiguration();
            if (_cfgHandler.Config.CoreOptions.minimizeToTray)
            {
                Hide();
                sysTrayIcon.Visible = true;
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modAccDateAccAgeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAccDateAccAgeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modAccDateAccAgeTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modAccDateAccAgeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modAccDateLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAccDateLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAccountDateUi();
            ShowInfoMessage("Account date limiter settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modAccDateResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modAccDateResetSettingsButton_Click(object sender, EventArgs e)
        {
            var accountDateOptions = _cfgHandler.Config.AccountDateOptions;
            accountDateOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            await
                HandleAccountDateModActivation(accountDateOptions.isActive,
                    accountDateOptions.minimumDaysRequired);
            PopulateModAccountDateUi();
            ShowInfoMessage("Account date limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modAccDateSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Account date limiter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Click event of the modAccuracyLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAccuracyLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAccuracyUi();
            ShowInfoMessage("Accuracy display settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modAccuracyResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAccuracyResetSettingsButton_Click(object sender, EventArgs e)
        {
            var accuracyOptions = _cfgHandler.Config.AccuracyOptions;
            accuracyOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            HandleStandardModuleActivation(_ssb.Mod.Accuracy, accuracyOptions.isActive);
            PopulateModAccuracyUi();
            ShowInfoMessage("Accuracy display settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modAccuracySaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAccuracySaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var accuracyOptions = _cfgHandler.Config.AccuracyOptions;
                accuracyOptions.isActive = modAccuracyEnableCheckBox.Checked;
                _cfgHandler.WriteConfiguration();
                HandleStandardModuleActivation(_ssb.Mod.Accuracy, accuracyOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Accuracy display settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Click event of the modAutoVoterAddVoteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modAutoVoterClearVotesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modAutoVoterDelVoteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterDelVoteButton_Click(object sender, EventArgs e)
        {
            if (modAutoVoterCurrentVotesBindingSource.Count == 0 ||
                modAutoVoterCurVotesListBox.SelectedIndex == -1) return;

            var selectedVote = (AutoVote)modAutoVoterCurVotesListBox.SelectedItem;
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

        /// <summary>
        ///     Handles the Click event of the modAutoVoterLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModAutoVoterUi();
            ShowInfoMessage("Auto voter display settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the CheckedChanged event of the modAutoVoterPassRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterPassRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        /// <summary>
        ///     Handles the CheckedChanged event of the modAutoVoterRejectRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterRejectRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        /// <summary>
        ///     Handles the Click event of the modAutoVoterResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterResetSettingsButton_Click(object sender, EventArgs e)
        {
            var autoVoterOptions = _cfgHandler.Config.AutoVoterOptions;
            autoVoterOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            HandleStandardModuleActivation(_ssb.Mod.AutoVoter, autoVoterOptions.isActive);
            PopulateModAutoVoterUi();
            ShowInfoMessage("Auto voter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modAutoVoterSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var autoVoterOptions = _cfgHandler.Config.AutoVoterOptions;
                autoVoterOptions.isActive = modAutoVoterEnableCheckBox.Checked;
                autoVoterOptions.autoVotes = _ssb.Mod.AutoVoter.AutoVotes;
                _cfgHandler.WriteConfiguration();
                HandleStandardModuleActivation(_ssb.Mod.AutoVoter, autoVoterOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Auto voter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the SelectedIndexChanged event of the modAutoVoterVoteTypeComboxBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modAutoVoterVoteTypeComboxBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        /// <summary>
        ///     Handles the Click event of the modEarlyQuitClearQuitsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modEarlyQuitClearQuitsButton_Click(object sender, EventArgs e)
        {
            var earlyQuitDb = new DbQuits();

            // Initial read of database
            RefreshCurrentQuittersDataSource();

            foreach (var p in modEarlyQuitCurrentQuitBindingSource.List)
            {
                var player = (EarlyQuitter)p;
                earlyQuitDb.DeleteUserFromDb(player.Name);
                await earlyQuitDb.RemoveQuitRelatedBan(_ssb, player.Name);
            }

            modEarlyQuitCurQuitsListBox.SelectedIndex = ((modEarlyQuitCurrentQuitBindingSource.Count > 0)
                ? 0
                : -1);

            // Update
            RefreshCurrentQuittersDataSource();
            Debug.WriteLine("[UI]: Cleared early quit database.");
        }

        /// <summary>
        ///     Handles the Click event of the modEarlyQuitDelQuitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modEarlyQuitDelQuitButton_Click(object sender, EventArgs e)
        {
            if (modEarlyQuitCurQuitsListBox.SelectedIndex == -1) return;
            var player = (EarlyQuitter)modEarlyQuitCurQuitsListBox.SelectedItem;
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

            modEarlyQuitCurQuitsListBox.SelectedIndex = ((modEarlyQuitCurrentQuitBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentQuittersDataSource();
        }

        /// <summary>
        ///     Handles the Click event of the modEarlyQuitForgiveQuitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modEarlyQuitForgiveQuitButton_Click(object sender, EventArgs e)
        {
            if (modEarlyQuitCurQuitsListBox.SelectedIndex == -1) return;
            var player = (EarlyQuitter)modEarlyQuitCurQuitsListBox.SelectedItem;
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

        /// <summary>
        ///     Handles the Click event of the modEarlyQuitLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEarlyQuitLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModEarlyQuitUi();
            ShowInfoMessage("Early quit banner settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the modEarlyQuitMaxQuitsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEarlyQuitMaxQuitsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEarlyQuitMaxQuitsTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modEarlyQuitMaxQuitsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modEarlyQuitResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEarlyQuitResetSettingsButton_Click(object sender, EventArgs e)
        {
            var earlyQuitOptions = _cfgHandler.Config.EarlyQuitOptions;
            earlyQuitOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            HandleStandardModuleActivation(_ssb.Mod.EarlyQuit, earlyQuitOptions.isActive);
            PopulateModEarlyQuitUi();
            ShowInfoMessage("Early quit banner settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modEarlyQuitSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEarlyQuitSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var earlyQuitOptions = _cfgHandler.Config.EarlyQuitOptions;
                earlyQuitOptions.isActive = modEarlyQuitEnableCheckBox.Checked;
                earlyQuitOptions.maxQuitsAllowed = uint.Parse(modEarlyQuitMaxQuitsTextBox.Text);
                earlyQuitOptions.banTime = double.Parse(modEarlyQuitTimeTextBox.Text);
                earlyQuitOptions.banTimeScale = (string)modEarlyQuitTimeScaleComboxBox.SelectedItem;
                earlyQuitOptions.banTimeScaleIndex = modEarlyQuitTimeScaleComboxBox.SelectedIndex;
                _cfgHandler.WriteConfiguration();

                // Go into effect now
                _ssb.Mod.EarlyQuit.MaxQuitsAllowed = earlyQuitOptions.maxQuitsAllowed;
                _ssb.Mod.EarlyQuit.BanTime = earlyQuitOptions.banTime;
                _ssb.Mod.EarlyQuit.BanTimeScale = earlyQuitOptions.banTimeScale;
                _ssb.Mod.EarlyQuit.BanTimeScaleIndex = earlyQuitOptions.banTimeScaleIndex;

                HandleStandardModuleActivation(_ssb.Mod.EarlyQuit, earlyQuitOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Early quit banner settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modEarlyQuitTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEarlyQuitTimeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEarlyQuitTimeTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modEarlyQuitTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modEloLimiterLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEloLimiterLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModEloLimiterUi();
            ShowInfoMessage("Elo limiter settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the modEloLimiterMaxEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEloLimiterMaxEloTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEloLimiterMaxEloTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modEloLimiterMaxEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Validated event of the modEloLimiterMinEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modEloLimiterMinEloTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEloLimiterMinEloTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modEloLimiterMinEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modEloLimiterResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modEloLimiterResetSettingsButton_Click(object sender, EventArgs e)
        {
            var eloLimitOptions = _cfgHandler.Config.EloLimitOptions;
            eloLimitOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            await HandleEloLimitModActivation(eloLimitOptions.isActive);
            PopulateModEloLimiterUi();
            ShowInfoMessage("Elo limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modEloLimiterSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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
                _ssb.Mod.EloLimit.MinimumRequiredElo = eloLimitOptions.minimumRequiredElo;
                _ssb.Mod.EloLimit.MaximumRequiredElo = eloLimitOptions.maximumRequiredElo;

                await HandleEloLimitModActivation(eloLimitOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Elo limiter settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCAdminNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCAdminNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCAdminNameTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCAdminNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCAdminNameTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcNickname(modIRCAdminNameTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCAdminNameTextBox.Select(0, modIRCAdminNameTextBox.Text.Length);
                errorProvider.SetError(modIRCAdminNameTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCBotNickNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCBotNickNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCBotNickNameTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCBotNickNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCBotNickNameTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcNickname(modIRCBotNickNameTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCBotNickNameTextBox.Select(0, modIRCBotNickNameTextBox.Text.Length);
                errorProvider.SetError(modIRCBotNickNameTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCBotUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCBotUserNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCBotUserNameTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCBotUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCBotUserNameTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcNickname(modIRCBotUserNameTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCBotUserNameTextBox.Select(0, modIRCBotUserNameTextBox.Text.Length);
                errorProvider.SetError(modIRCBotUserNameTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCChannelKeyTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCChannelKeyTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCChannelKeyTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCChannelKeyTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCChannelKeyTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcChannelKey(modIRCChannelKeyTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCChannelKeyTextBox.Select(0, modIRCChannelKeyTextBox.Text.Length);
                errorProvider.SetError(modIRCChannelKeyTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCChannelTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCChannelTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCChannelTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCChannelTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCChannelTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcChannel(modIRCChannelTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCChannelTextBox.Select(0, modIRCChannelTextBox.Text.Length);
                errorProvider.SetError(modIRCChannelTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Click event of the modIRCGenerateRandomNamesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCGenerateRandomNamesButton_Click(object sender, EventArgs e)
        {
            modIRCBotNickNameTextBox.Text = string.Format("SSB|QLive-{0}",
                _cfgHandler.Config.IrcOptions.GenerateRandomIdentifier());
            modIRCBotUserNameTextBox.Text = string.Format("ssbQL{0}",
                _cfgHandler.Config.IrcOptions.GenerateRandomIdentifier());
        }

        /// <summary>
        ///     Handles the Click event of the modIRCLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModIrcUi();
            ShowInfoMessage("IRC settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCQNetPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCQNetPassTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCQNetPassTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCQNetPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCQNetPassTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidQuakeNetPassword(modIRCQNetPassTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCQNetPassTextBox.Select(0, modIRCQNetPassTextBox.Text.Length);
                errorProvider.SetError(modIRCQNetPassTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCQNetUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCQNetUserNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCQNetUserNameTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCQNetUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCQNetUserNameTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidQuakeNetUsername(modIRCQNetUserNameTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCQNetUserNameTextBox.Select(0, modIRCQNetUserNameTextBox.Text.Length);
                errorProvider.SetError(modIRCQNetUserNameTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Click event of the modIRCResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCResetSettingsButton_Click(object sender, EventArgs e)
        {
            var ircOptions = _cfgHandler.Config.IrcOptions;
            ircOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            HandleIrcModActivation(ircOptions.isActive);
            PopulateModIrcUi();
            ShowInfoMessage("IRC settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modIRCSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                // Cannot auto-auth with Q or auto-hide host if Q user/pass not provided.
                if (modIRCQNetAutoAuthCheckBox.Checked ||
                    modIRCQNetHideHostCheckBox.Checked)
                {
                    if (modIRCQNetUserNameTextBox.Text.Length == 0 ||
                        modIRCQNetPassTextBox.Text.Length == 0)
                    {
                        ShowErrorMessage(
                            "You must specify a QuakeNet Q username & password to auto-auth with Q.",
                            "Error");
                        modIRCQNetUserNameTextBox.Clear();
                        modIRCQNetPassTextBox.Clear();
                        return;
                    }
                }

                var ircOptions = _cfgHandler.Config.IrcOptions;
                ircOptions.isActive = modIRCEnableCheckBox.Checked;
                ircOptions.ircAdminNickname = modIRCAdminNameTextBox.Text;
                ircOptions.ircNickName = modIRCBotNickNameTextBox.Text;
                ircOptions.ircUserName = modIRCBotUserNameTextBox.Text;
                ircOptions.ircServerAddress = modIRCServerAddressTextBox.Text;
                ircOptions.ircServerPort = uint.Parse(modIRCServerPortTextBox.Text);
                ircOptions.ircServerPassword = modIRCServerPassTextBox.Text;
                ircOptions.ircChannel = modIRCChannelTextBox.Text;
                ircOptions.ircChannelKey = modIRCChannelKeyTextBox.Text;
                ircOptions.autoConnectOnStart = modIRCAutoConnectCheckBox.Checked;
                ircOptions.ircNickServiceUsername = modIRCQNetUserNameTextBox.Text;
                ircOptions.ircNickServicePassword = modIRCQNetPassTextBox.Text;
                ircOptions.autoAuthWithNickService = modIRCQNetAutoAuthCheckBox.Checked;
                ircOptions.hideHostnameOnQuakeNet = modIRCQNetHideHostCheckBox.Checked;
                _cfgHandler.WriteConfiguration();

                // Go into effect now
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircAdminNickname = ircOptions.ircAdminNickname;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircNickName = ircOptions.ircNickName;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircUserName = ircOptions.ircUserName;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircServerAddress = ircOptions.ircServerAddress;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircServerPort = ircOptions.ircServerPort;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircServerPassword = ircOptions.ircServerPassword;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircChannel = ircOptions.ircChannel;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircChannelKey = ircOptions.ircChannelKey;
                _ssb.Mod.Irc.IrcManager.IrcSettings.autoConnectOnStart = ircOptions.autoConnectOnStart;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircNickServiceUsername = ircOptions.ircNickServiceUsername;
                _ssb.Mod.Irc.IrcManager.IrcSettings.ircNickServicePassword = ircOptions.ircNickServicePassword;
                _ssb.Mod.Irc.IrcManager.IrcSettings.autoAuthWithNickService =
                    ircOptions.autoAuthWithNickService;
                _ssb.Mod.Irc.IrcManager.IrcSettings.hideHostnameOnQuakeNet = ircOptions.hideHostnameOnQuakeNet;

                HandleIrcModActivation(ircOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("IRC settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCServerAddressTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCServerAddressTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCServerAddressTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCServerAddressTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCServerAddressTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcServerAddress(modIRCServerAddressTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCServerAddressTextBox.Select(0, modIRCServerAddressTextBox.Text.Length);
                errorProvider.SetError(modIRCServerAddressTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCServerPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCServerPassTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCServerPassTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCServerPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCServerPassTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcServerPassword(modIRCServerPassTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCServerPassTextBox.Select(0, modIRCServerPassTextBox.Text.Length);
                errorProvider.SetError(modIRCServerPassTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modIRCServerPortTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modIRCServerPortTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCServerPortTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modIRCServerPortTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modIRCServerPortTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_ircValidator.IsValidIrcServerPort(modIRCServerPortTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                modIRCServerPortTextBox.Select(0, modIRCServerPortTextBox.Text.Length);
                errorProvider.SetError(modIRCServerPortTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Click event of the modMOTDLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modMOTDLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModMotdUi();
            ShowInfoMessage("MOTD settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the modMOTDRepeatMsgTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modMOTDRepeatMsgTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modMOTDRepeatMsgTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modMOTDRepeatMsgTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Validated event of the modMOTDRepeatTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modMOTDRepeatTimeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modMOTDRepeatTimeTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modMOTDRepeatTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modMOTDResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modMOTDResetSettingsButton_Click(object sender, EventArgs e)
        {
            var motdOptions = _cfgHandler.Config.MotdOptions;
            motdOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            HandleMotdModActivation(motdOptions.isActive);
            PopulateModMotdUi();
            ShowInfoMessage("Account date limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modMOTDSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modMOTDSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var motdOptions = _cfgHandler.Config.MotdOptions;
                motdOptions.isActive = modMOTDEnableCheckBox.Checked;
                motdOptions.repeatInterval = int.Parse(modMOTDRepeatTimeTextBox.Text);
                motdOptions.message = modMOTDRepeatMsgTextBox.Text;
                _cfgHandler.WriteConfiguration();
                HandleMotdModActivation(motdOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("MOTD settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Click event of the modPickupLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modPickupLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModPickupUi();
            ShowInfoMessage("Pickup settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the modPickupMaxNoShowsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modPickupMaxNoShowsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupMaxNoShowsTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modPickupMaxNoShowsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modPickupMaxNoShowsTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_pickupValidator.IsValidMaximumNoShowNum(modPickupMaxNoShowsTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modPickupMaxNoShowsTextBox.Select(0, modPickupMaxNoShowsTextBox.Text.Length);
                errorProvider.SetError(modPickupMaxNoShowsTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modPickupMaxSubsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modPickupMaxSubsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupMaxSubsTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modPickupMaxSubsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modPickupMaxSubsTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_pickupValidator.IsValidMaximumSubsNum(modPickupMaxSubsTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modPickupMaxSubsTextBox.Select(0, modPickupMaxSubsTextBox.Text.Length);
                errorProvider.SetError(modPickupMaxSubsTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modPickupNoShowsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modPickupNoShowsTimeBanTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupNoShowsTimeBanTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modPickupNoShowsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modPickupNoShowsTimeBanTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_pickupValidator.IsValidTimeBanNum(modPickupNoShowsTimeBanTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modPickupNoShowsTimeBanTextBox.Select(0, modPickupNoShowsTimeBanTextBox.Text.Length);
                errorProvider.SetError(modPickupNoShowsTimeBanTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modPickupPlayersPerTeamTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modPickupPlayersPerTeamTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupPlayersPerTeamTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modPickupPlayersPerTeamTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modPickupPlayersPerTeamTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_pickupValidator.IsValidPickupTeamSize(modPickupPlayersPerTeamTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modPickupPlayersPerTeamTextBox.Select(0, modPickupPlayersPerTeamTextBox.Text.Length);
                errorProvider.SetError(modPickupPlayersPerTeamTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Click event of the modPickupResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modPickupResetSettingsButton_Click(object sender, EventArgs e)
        {
            var pickupOptions = _cfgHandler.Config.PickupOptions;
            pickupOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            await HandlePickupModActivation(pickupOptions.isActive);
            PopulateModPickupUi();
            ShowInfoMessage("Pickup settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modPickupSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void modPickupSaveSettingsButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var pickupOptions = _cfgHandler.Config.PickupOptions;
                pickupOptions.isActive = modPickupEnableCheckBox.Checked;
                pickupOptions.maxSubsPerPlayer = int.Parse(modPickupMaxSubsTextBox.Text);
                pickupOptions.maxNoShowsPerPlayer = int.Parse(modPickupMaxNoShowsTextBox.Text);
                pickupOptions.excessiveSubUseBanTime = double.Parse(modPickupSubsTimeBanTextBox.Text);
                pickupOptions.excessiveNoShowBanTime = double.Parse(modPickupNoShowsTimeBanTextBox.Text);
                pickupOptions.excessiveSubUseBanTimeScale =
                    (string)modPickupSubsTimeBanScaleComboBox.SelectedItem;
                pickupOptions.excessiveNoShowBanTimeScale =
                    (string)modPickupNoShowsTimeBanScaleComboBox.SelectedItem;
                pickupOptions.excessiveSubUseBanTimeScaleIndex =
                    modPickupSubsTimeBanScaleComboBox.SelectedIndex;
                pickupOptions.excessiveNoShowBanTimeScaleIndex =
                    modPickupNoShowsTimeBanScaleComboBox.SelectedIndex;
                pickupOptions.teamSize = int.Parse(modPickupPlayersPerTeamTextBox.Text);
                _cfgHandler.WriteConfiguration();

                // Go into effect now
                _ssb.Mod.Pickup.MaxSubsPerPlayer = pickupOptions.maxSubsPerPlayer;
                _ssb.Mod.Pickup.MaxNoShowsPerPlayer = pickupOptions.maxNoShowsPerPlayer;
                _ssb.Mod.Pickup.ExcessiveSubUseBanTime = pickupOptions.excessiveSubUseBanTime;
                _ssb.Mod.Pickup.ExcessiveNoShowBanTime = pickupOptions.excessiveNoShowBanTime;
                _ssb.Mod.Pickup.ExcessiveSubUseBanTimeScale = pickupOptions.excessiveSubUseBanTimeScale;
                _ssb.Mod.Pickup.ExcessiveNoShowBanTimeScale = pickupOptions.excessiveNoShowBanTimeScale;
                _ssb.Mod.Pickup.ExcessiveSubUseBanTimeScaleIndex =
                    pickupOptions.excessiveSubUseBanTimeScaleIndex;
                _ssb.Mod.Pickup.ExcessiveNoShowBanTimeScaleIndex =
                    pickupOptions.excessiveNoShowBanTimeScaleIndex;
                _ssb.Mod.Pickup.Teamsize = pickupOptions.teamSize;

                await HandlePickupModActivation(pickupOptions.isActive);
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Pickup settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modPickupSubsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modPickupSubsTimeBanTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupSubsTimeBanTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modPickupSubsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void modPickupSubsTimeBanTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (
                !_pickupValidator.IsValidTimeBanNum(modPickupSubsTimeBanTextBox.Text,
                    out errorMsg))
            {
                e.Cancel = true;
                modPickupSubsTimeBanTextBox.Select(0, modPickupSubsTimeBanTextBox.Text.Length);
                errorProvider.SetError(modPickupSubsTimeBanTextBox, errorMsg);
            }
        }

        /// <summary>
        ///     Handles the Click event of the modServerListLoadSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modServerListLoadSettingsButton_Click(object sender, EventArgs e)
        {
            PopulateModServerListUi();
            ShowInfoMessage("Server list settings loaded.", "Settings Loaded");
        }

        /// <summary>
        ///     Handles the Validated event of the modServerListMaxServersTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modServerListMaxServersTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modServerListMaxServersTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modServerListMaxServersTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the Click event of the modServerListResetSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modServerListResetSettingsButton_Click(object sender, EventArgs e)
        {
            var serverListOptions = _cfgHandler.Config.ServersOptions;
            serverListOptions.SetDefaults();
            _cfgHandler.WriteConfiguration();
            HandleStandardModuleActivation(_ssb.Mod.Servers, serverListOptions.isActive);
            PopulateModServerListUi();
            ShowInfoMessage("Server list settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        ///     Handles the Click event of the modServerListSaveSettingsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
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
                UpdateActiveModulesStatusBarText();
                ShowInfoMessage("Server list settings saved.", "Settings Saved");
            }
            else
            {
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        ///     Handles the Validated event of the modServerListTimeBetweenTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void modServerListTimeBetweenTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modServerListTimeBetweenTextBox, string.Empty);
        }

        /// <summary>
        ///     Handles the Validating event of the modServerListTimeBetweenTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
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

        /// <summary>
        ///     Handles the SelectedIndexChanged event of the moduleTabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void moduleTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;
            var currentTabPage = tabControl.SelectedTab;

            // The modules tabs' data are populated on initial load, but might've changed due to
            // in-game or IRC commands; so re-populate on tab switch.
            if (currentTabPage == accountDateTab)
                PopulateModAccountDateUi();
            else if (currentTabPage == accuracyTab)
                PopulateModAccuracyUi();
            else if (currentTabPage == autoVoterTab)
                PopulateModAutoVoterUi();
            else if (currentTabPage == earlyQuitTab)
                PopulateModEarlyQuitUi();
            else if (currentTabPage == eloLimitTab)
                PopulateModEloLimiterUi();
            else if (currentTabPage == ircTab)
                PopulateModIrcUi();
            else if (currentTabPage == motdTab)
                PopulateModMotdUi();
            else if (currentTabPage == pickupTab)
                PopulateModPickupUi();
            else if (currentTabPage == serversTab)
                PopulateModServerListUi();
        }

        /// <summary>
        ///     Populates all UI tabs.
        /// </summary>
        private void PopulateAllUiTabs()
        {
            PopulateCoreOptionsUi();
            PopulateModAccountDateUi();
            PopulateModAccuracyUi();
            PopulateModAutoVoterUi();
            PopulateModEarlyQuitUi();
            PopulateModEloLimiterUi();
            PopulateModIrcUi();
            PopulateModMotdUi();
            PopulateModPickupUi();
            PopulateModServerListUi();
            PopulateUserManagementUi();
            PopulateBanManagementUi();
        }

        /// <summary>
        ///     Populates the core options UI tab.
        /// </summary>
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
            coreMinimizeToTrayCheckBox.Checked = coreOptions.minimizeToTray;
            coreOwnerNameTextBox.Text = coreOptions.owner;
            UpdateAccountInfoStatusBarText();
            Debug.WriteLine("[UI]: Populated core options user interface.");
        }

        /// <summary>
        ///     Sets references to various UI controls that need to be accessed
        ///     from other classes.
        /// </summary>
        private void SetAppWideUiControls()
        {
            _ssb.AppWideUiControls.LogConsoleTextBox = logConsoleTextBox;
            _ssb.AppWideUiControls.MonitoringStatusBar = monitoringStatusLabel;
            _ssb.AppWideUiControls.StartMonitoringButton = ssbStartButton;
            _ssb.AppWideUiControls.StopMonitoringButton = ssbStopButton;
        }

        /// <summary>
        ///     Sets the automatic vote optional text.
        /// </summary>
        private void SetAutoVoteOptionalText()
        {
            modAutoVoterContainingDescLabel.Text =
                string.Format("If empty then ALL {0} votes will {1}",
                    (modAutoVoterVoteTypeComboxBox.SelectedItem), (string.Format("{0}automatically {1}",
                        Environment.NewLine, ((modAutoVoterPassRadioButton.Checked) ? "PASS." : "FAIL."))));
        }

        /// <summary>
        ///     Shows an error message box.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="title">The title.</param>
        private void ShowErrorMessage(string text, string title)
        {
            MessageBox.Show(text, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        ///     Shows the information message box.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="title">The title.</param>
        private void ShowInfoMessage(string text, string title)
        {
            MessageBox.Show(text, title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        ///     Handles the Click event of the ssbExitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void ssbExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Handles the MouseMove event of the ssbLogo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private void ssbLogo_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == 0)
            {
                return;
            }
            Win32Api.ReleaseCapture();
            Win32Api.SendMessage(Handle, Win32Api.WM_NCLBUTTONDOWN, Win32Api.HT_CAPTION, 0);
        }

        /// <summary>
        ///     Handles the Click event of the ssbResetButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void ssbResetButton_Click(object sender, EventArgs e)
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Debug.WriteLine(
                    "[UI]: User attempted to reset monitoring of server but QL window not found. Won't allow.");
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Debug.WriteLine("[UI]: Got user request to reset server monitoring; Stopping if exists," +
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

        /// <summary>
        ///     Handles the Click event of the ssbStartButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private async void ssbStartButton_Click(object sender, EventArgs e)
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Debug.WriteLine(
                    "[UI]: User attempted to start monitoring of server but QL window not found. Won't allow.");
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_ssb.IsMonitoringServer)
            {
                // Do nothing if we're already monitoring
                Debug.WriteLine(
                    "[UI]: Got user's request to start monitoring, but we're already monitoring the server. Ignoring.");
                MessageBox.Show(@"Already monitoring a Quake Live server!", @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await _ssb.BeginMonitoring();
        }

        /// <summary>
        ///     Handles the Click event of the ssbStopButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void ssbStopButton_Click(object sender, EventArgs e)
        {
            if (!_ssb.IsMonitoringServer)
            {
                Debug.WriteLine(
                    "[UI]: SSB was not previously monitoring server; ignoring user's request to stop monitoring.");
                return;
            }

            _ssb.StopMonitoring();
            Debug.WriteLine("[UI]: Got user request to stop monitoring server. Stopping monitoring.");
        }

        /// <summary>
        ///     Handles the Paint event of the statusBar control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs" /> instance containing the event data.</param>
        private void statusBar_Paint(object sender, PaintEventArgs e)
        {
            // left, top, right, bottom
            // draw a light border at the top of the status bar
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                Color.Black, 0, ButtonBorderStyle.Solid,
                Color.FromArgb(62, 234, 246, 255), 1, ButtonBorderStyle.Solid,
                Color.Black, 0, ButtonBorderStyle.Solid,
                Color.Black, 0, ButtonBorderStyle.Solid);
        }

        /// <summary>
        ///     Handles the Click event of the sysTrayExitMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void sysTrayExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Handles the MouseClick event of the sysTrayIcon control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        private void sysTrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Show();
                sysTrayIcon.Visible = false;
                WindowState = FormWindowState.Normal;
            }
            else if (e.Button == MouseButtons.Right)
            {
                sysTrayContextMenuStrip.Show();
            }
        }

        /// <summary>
        ///     Handles the SelectedIndexChanged event of the UiTabCtl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void UiTabCtl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;
            var currentTabPage = tabControl.SelectedTab;

            // For certain tabs, re-populate the tab on switch
            // modules have their own event handler, see: moduleTabControl_SelectedIndexChanged
            if (currentTabPage == coreOptionsTab)
                PopulateCoreOptionsUi();
            else if (currentTabPage == usersTab)
                PopulateUserManagementUi();
            else if (currentTabPage == banTab)
                PopulateBanManagementUi();
        }

        /// <summary>
        /// Updates the account information status bar text.
        /// </summary>
        private void UpdateAccountInfoStatusBarText()
        {
            _cfgHandler.ReadConfiguration();
            activeAccountsLabel.Text = string.Format("SSB Account: {0} \t Owner Account: {1}",
                _cfgHandler.Config.CoreOptions.owner, _cfgHandler.Config.CoreOptions.accountName);
        }

        /// <summary>
        ///     Updates the active modules status bar text.
        /// </summary>
        private void UpdateActiveModulesStatusBarText()
        {
            activeModulesLabel.Text = _ssb.Mod.ActiveModuleCount == 0
                ? @"No active modules"
                : string.Format("Active modules: {0}", _ssb.Mod.GetActiveModules());
        }

        /// <summary>
        ///     Handles the Click event of the usrMAddUserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void usrMAddUserButton_Click(object sender, EventArgs e)
        {
            if (usrMUserQlNameTextBox.Text.Length == 0 ||
                !Helpers.IsValidQlUsernameFormat(usrMUserQlNameTextBox.Text, false))
            {
                ShowErrorMessage("You must specify a valid Quake Live name.",
                    "Invalid QL name");
                return;
            }

            // See if the user exists
            var userDb = new DbUsers();
            var user = usrMUserQlNameTextBox.Text;
            if (userDb.UserExists(user))
            {
                ShowErrorMessage(
                    string.Format(
                        "User {0} already exists in the user database. Remove the user then re-add.",
                        user), "User exists");
                return;
            }
            _cfgHandler.ReadConfiguration();
            var owner = _cfgHandler.Config.CoreOptions.owner;
            var accessLevel = (UserLevel)usrMUserAccessComboBox.SelectedItem;
            userDb.AddUserToDb(user, accessLevel, owner,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            RefreshCurrentSsbUsersDataSource();
            usrMCurUsersListBox.SelectedIndex = ((usrMCurrentUserBindingSource.Count > 0)
                ? 0
                : -1);
            usrMUserAccessComboBox.SelectedIndex = 0;
            usrMUserQlNameTextBox.Clear();

            Debug.WriteLine("[UI]: Owner {0} added user {1} with access level {2} to user database.",
                owner, user, accessLevel);
        }

        /// <summary>
        ///     Handles the Click event of the usrMDelAllUsersButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void usrMDelAllUsersButton_Click(object sender, EventArgs e)
        {
            _cfgHandler.ReadConfiguration();

            var owner = _cfgHandler.Config.CoreOptions.owner;
            var userDb = new DbUsers();
            var allUsers = userDb.GetAllUsers();

            if (allUsers.Count == 0)
            {
                Debug.WriteLine(string.Format("[UI]: Owner {0} attempted to remove all users from user" +
                                              " database, but no users exist.", owner));

                ShowErrorMessage("There are no users to remove.",
                    "No users");
                return;
            }

            foreach (var user in allUsers)
            {
                userDb.DeleteUserFromDb(user.Name, owner, UserLevel.Owner);
            }

            usrMCurUsersListBox.SelectedIndex = ((usrMCurrentUserBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentSsbUsersDataSource();

            Debug.WriteLine("[UI]: Owner {0} removed all {1} users from the user database",
                owner, allUsers.Count);
        }

        /// <summary>
        ///     Handles the Click event of the usrMDelUserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void usrMDelUserButton_Click(object sender, EventArgs e)
        {
            if (usrMCurrentUserBindingSource.Count == 0 ||
                usrMCurUsersListBox.SelectedIndex == -1) return;

            _cfgHandler.ReadConfiguration();

            var userDb = new DbUsers();
            var owner = _cfgHandler.Config.CoreOptions.owner;
            var selectedUser = (User)usrMCurUsersListBox.SelectedItem;

            usrMCurrentUserBindingSource.Remove(selectedUser);
            userDb.DeleteUserFromDb(selectedUser.Name, owner, UserLevel.Owner);

            usrMCurUsersListBox.SelectedIndex = ((usrMCurrentUserBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentSsbUsersDataSource();

            Debug.WriteLine("[UI]: Owner {0} removed user {1} with access level {2} from user database.",
                owner, selectedUser.Name, selectedUser.AccessLevel);
        }
    }
}