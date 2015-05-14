using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using SST.Config;
using SST.Config.Core;
using SST.Core;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Ui.Validation;
using SST.Ui.Validation.Modules;
using SST.Util;

namespace SST.Ui
{
    /// <summary>
    /// The user interface class.
    /// </summary>
    /// <remarks>
    /// This class is large because the UI is essentially a single form with a tab control (two,
    /// when counting the modules tab). In Winforms, each tab does not receive its own separate
    /// class unless a user control is used, which did not make sense for this project as it is a
    /// one-off tool.
    /// </remarks>
    public partial class UserInterface : Form
    {
        private readonly AccountDateLimitValidator _accountDateLimitValidator;
        private readonly ConfigHandler _cfgHandler;
        private readonly CoreOptionsValidator _coreOptionsValidator;
        private readonly EarlyQuitValidator _earlyQuitValidator;
        private readonly EloLimitValidator _eloLimitValidator;
        private readonly IrcValidator _ircValidator;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[UI]";
        private readonly MotdValidator _motdValidator;
        private readonly PickupValidator _pickupValidator;
        private readonly ServerListValidator _serverListValidator;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterface"/> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        public UserInterface(SynServerTool sst)
        {
            InitializeComponent();

            _cfgHandler = new ConfigHandler();
            var cfg = _cfgHandler.ReadConfiguration();
            Log.LogToSstConsole = cfg.CoreOptions.appendToActivityLog;
            Log.LogUiConsole = logConsoleTextBox;
            Log.ShowDeferredMessages();

            titleBarVersionLabel.Text = string.Format("version {0}",
                Helpers.GetVersion());
            sysTrayIcon.Text = string.Format("SST v{0}",
                Helpers.GetVersion());
            abtVersLabel.Text = Helpers.GetVersion();
            sysTrayIcon.Visible = false;

            _sst = sst;
            _coreOptionsValidator = new CoreOptionsValidator();
            _accountDateLimitValidator = new AccountDateLimitValidator();
            _earlyQuitValidator = new EarlyQuitValidator();
            _eloLimitValidator = new EloLimitValidator();
            _motdValidator = new MotdValidator();
            _serverListValidator = new ServerListValidator();
            _pickupValidator = new PickupValidator();
            _ircValidator = new IrcValidator(_sst.Mod.Irc.IrcManager.ValidIrcNickRegex);
            PopulateAllUiTabs();
            _sst.UserInterface = this;

            AutoCheckForUpdates();
        }

        /// <summary>
        /// Populates the ban management UI tab.
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
        }

        /// <summary>
        /// Populates the account date module UI tab.
        /// </summary>
        public void PopulateModAccountDateUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modAccDateEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.AccountDateOptions.isActive; });
            modAccDateAccAgeTextBox.InvokeIfRequired(
                c => { c.Text = cfg.AccountDateOptions.minimumDaysRequired.ToString(); });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the accuracy module UI tab.
        /// </summary>
        public void PopulateModAccuracyUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modAccuracyEnableCheckBox.InvokeIfRequired(
                c => { c.Checked = cfg.AccuracyOptions.isActive; });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the automatic voter module UI tab.
        /// </summary>
        public void PopulateModAutoVoterUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modAutoVoterEnableCheckBox.InvokeIfRequired(
                c => { c.Checked = cfg.AutoVoterOptions.isActive; });

            // Radio buttons
            modAutoVoterPassRadioButton.InvokeIfRequired(c => { c.Checked = true; });
            // Vote types combo box
            modAutoVoterVoteTypeComboxBox.InvokeIfRequired(c =>
            {
                c.DisplayMember = "Name";
                c.ValueMember = "Name";
                c.DataSource = _sst.Mod.AutoVoter.ValidCallVotes;
                c.SelectedIndex = 0;
            });
            // Current votes listbox
            RefreshCurrentVotesDataSource();
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the early quit module UI tab.
        /// </summary>
        public void PopulateModEarlyQuitUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modEarlyQuitEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.EarlyQuitOptions.isActive; });
            modEarlyQuitMaxQuitsTextBox.InvokeIfRequired(
                c => { c.Text = cfg.EarlyQuitOptions.maxQuitsAllowed.ToString(); });
            modEarlyQuitTimeTextBox.InvokeIfRequired(
                c => { c.Text = cfg.EarlyQuitOptions.banTime.ToString(CultureInfo.InvariantCulture); });
            modEarlyQuitTimeScaleComboxBox.InvokeIfRequired(c =>
            {
                c.DataSource = Helpers.ValidTimeScales;
                c.SelectedIndex = cfg.EarlyQuitOptions.banTimeScaleIndex;
            });

            // Current early quitters listbox
            RefreshCurrentQuittersDataSource();
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the Elo limiter module UI tab.
        /// </summary>
        public void PopulateModEloLimiterUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modEloLimiterEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.EloLimitOptions.isActive; });
            modEloLimiterMinEloTextBox.InvokeIfRequired(
                c => { c.Text = cfg.EloLimitOptions.minimumRequiredElo.ToString(); });
            modEloLimiterMaxEloTextBox.InvokeIfRequired(c =>
            {
                c.Text = ((cfg.EloLimitOptions.maximumRequiredElo == 0)
                    ? string.Empty
                    : cfg.EloLimitOptions.maximumRequiredElo.ToString());
            });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the IRC module UI tab.
        /// </summary>
        public void PopulateModIrcUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modIRCEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.IrcOptions.isActive; });
            modIRCAdminNameTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircAdminNickname; });
            modIRCBotNickNameTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircNickName; });
            modIRCBotUserNameTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircUserName; });
            modIRCQNetUserNameTextBox.InvokeIfRequired(
                c => { c.Text = cfg.IrcOptions.ircNickServiceUsername; });
            modIRCQNetPassTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircNickServicePassword; });
            modIRCQNetAutoAuthCheckBox.InvokeIfRequired(
                c => { c.Checked = cfg.IrcOptions.autoAuthWithNickService; });
            modIRCQNetHideHostCheckBox.InvokeIfRequired(
                c => { c.Checked = cfg.IrcOptions.hideHostnameOnQuakeNet; });
            modIRCServerAddressTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircServerAddress; });
            modIRCServerPortTextBox.InvokeIfRequired(
                c => { c.Text = cfg.IrcOptions.ircServerPort.ToString(); });
            modIRCServerPassTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircServerPassword; });
            modIRCChannelTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircChannel; });
            modIRCChannelKeyTextBox.InvokeIfRequired(c => { c.Text = cfg.IrcOptions.ircChannelKey; });
            modIRCAutoConnectCheckBox.InvokeIfRequired(c => { c.Checked = cfg.IrcOptions.autoConnectOnStart; });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the MOTD module UI tab.
        /// </summary>
        public void PopulateModMotdUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modMOTDEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.MotdOptions.isActive; });
            modMOTDRepeatTimeTextBox.InvokeIfRequired(
                c => { c.Text = cfg.MotdOptions.repeatInterval.ToString(); });
            modMOTDRepeatMsgTextBox.InvokeIfRequired(c => { c.Text = cfg.MotdOptions.message; });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the pickup module UI tab.
        /// </summary>
        public void PopulateModPickupUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modPickupEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.PickupOptions.isActive; });
            modPickupMaxSubsTextBox.InvokeIfRequired(
                c => { c.Text = cfg.PickupOptions.maxSubsPerPlayer.ToString(); });
            modPickupMaxNoShowsTextBox.InvokeIfRequired(
                c => { c.Text = cfg.PickupOptions.maxNoShowsPerPlayer.ToString(); });
            modPickupPlayersPerTeamTextBox.InvokeIfRequired(
                c => { c.Text = cfg.PickupOptions.teamSize.ToString(); });
            modPickupNoShowsTimeBanTextBox.InvokeIfRequired(c =>
            {
                c.Text = cfg.PickupOptions.excessiveNoShowBanTime.
                    ToString(CultureInfo.InvariantCulture);
            });
            modPickupSubsTimeBanTextBox.InvokeIfRequired(c =>
            {
                c.Text = cfg.PickupOptions.excessiveSubUseBanTime.
                    ToString(CultureInfo.InvariantCulture);
            });
            modPickupSubsTimeBanScaleComboBox.InvokeIfRequired(
                c => { c.DataSource = Helpers.ValidTimeScales; });
            modPickupNoShowsTimeBanScaleComboBox.InvokeIfRequired(
                c => { c.DataSource = Helpers.ValidTimeScales; });
            modPickupSubsTimeBanScaleComboBox.InvokeIfRequired(
                c => { c.SelectedIndex = cfg.PickupOptions.excessiveSubUseBanTimeScaleIndex; });
            modPickupNoShowsTimeBanScaleComboBox.InvokeIfRequired(c =>
            {
                c.SelectedIndex =
                    cfg.PickupOptions.excessiveNoShowBanTimeScaleIndex;
            });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the server list module UI tab.
        /// </summary>
        public void PopulateModServerListUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modServerListEnableCheckBox.InvokeIfRequired(c => { c.Checked = cfg.ServersOptions.isActive; });
            modServerListMaxServersTextBox.InvokeIfRequired(
                c => { c.Text = cfg.ServersOptions.maxServers.ToString(); });
            modServerListTimeBetweenTextBox.InvokeIfRequired(c =>
            {
                c.Text = cfg.ServersOptions.timeBetweenQueries.ToString(
                    CultureInfo.InvariantCulture);
            });
            UpdateActiveModulesStatusText();
        }

        /// <summary>
        /// Populates the user management UI tab.
        /// </summary>
        public void PopulateUserManagementUi()
        {
            // Specfically leave out user levels of None and Owner type.
            UserLevel[] levels = { UserLevel.User, UserLevel.SuperUser, UserLevel.Admin };
            usrMUserAccessComboBox.InvokeIfRequired(c => { c.DataSource = levels; });
            usrMUserAccessComboBox.InvokeIfRequired(c => { c.SelectedIndex = 0; });
            // Current SST users listbox
            RefreshCurrentSstUsersDataSource();
        }

        /// <summary>
        /// Refreshes the current bans data source.
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
            banMCurrentBanBindingSource.DataSource =
                new BindingList<BanInfo>(bansDb.GetAllBans());
            if (banMCurrentBanBindingSource.Count != 0)
            {
                banMCurBansListBox.InvokeIfRequired(
                    c => { c.DataSource = banMCurrentBanBindingSource.DataSource; });
            }
        }

        /// <summary>
        /// Refreshes the current quitters data source.
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
            modEarlyQuitCurrentQuitBindingSource.DataSource =
                new BindingList<EarlyQuitter>(earlyQuitDb.GetAllQuitters());
            if (modEarlyQuitCurrentQuitBindingSource.Count != 0)
            {
                modEarlyQuitCurQuitsListBox.InvokeIfRequired(
                    c => { c.DataSource = modEarlyQuitCurrentQuitBindingSource.DataSource; });
            }
        }

        /// <summary>
        /// Refreshes the current SST users data source.
        /// </summary>
        public void RefreshCurrentSstUsersDataSource()
        {
            usrMCurUsersListBox.InvokeIfRequired(c =>
            {
                c.DataSource = null;
                c.DisplayMember = "UserFormatDisplay";
                c.ValueMember = "Name";
            });
            var userDb = new DbUsers();
            usrMCurrentUserBindingSource.DataSource =
                new BindingList<User>(userDb.GetAllUsers());

            if (usrMCurrentUserBindingSource.Count != 0)
            {
                usrMCurUsersListBox.InvokeIfRequired(
                    c =>
                    {
                        // necessary to prevent ArgumentOutOfRangeException with !adduser from in-game
                        c.SelectedIndex = c.Items.Count != 0 ? 0 : -1;
                        c.DataSource = usrMCurrentUserBindingSource.DataSource;
                    });
            }
        }

        /// <summary>
        /// Refreshes the current votes data source.
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
                new BindingList<AutoVote>(_sst.Mod.AutoVoter.AutoVotes);
            if (modAutoVoterCurrentVotesBindingSource.Count != 0)
            {
                // Only set the listbox's datasource if there are elements otherwise,
                // ArgumentOutOfRange is unfortunately possible
                // see: http://stackoverflow.com/a/26762624
                modAutoVoterCurVotesListBox.InvokeIfRequired(
                    c => { c.DataSource = modAutoVoterCurrentVotesBindingSource.DataSource; });
            }
        }

        /// <summary>
        /// Updates the monitoring status UI elements.
        /// </summary>
        /// <param name="isMonitoring">if set to <c>true</c> then server monitoring is active.</param>
        /// <param name="address">The server address.</param>
        public void UpdateMonitoringStatusUi(bool isMonitoring, string address)
        {
            sstStartButton.InvokeIfRequired(c => { c.Enabled = !isMonitoring; });
            sstStopButton.InvokeIfRequired(c => { c.Enabled = isMonitoring; });

            monitorStatusLabel.InvokeIfRequired(c =>
            {
                c.Text = string.Format("{0}", (isMonitoring)
                    ? string.Format(
                        "Monitoring server at {0}",
                        (string.IsNullOrEmpty(address)
                            ? "..."
                            : address))
                    : "Not monitoring a server");
            });
        }

        /// <summary>
        /// Handles the Click event of the abtCheckUpdateButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void abtCheckUpdateButton_Click(object sender, EventArgs e)
        {
            var checker = new VersionChecker();
            await checker.CheckForUpdates(false);
        }

        /// <summary>
        /// Handles the Click event of the abtCommandList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void abtCommandList_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sst-commands.txt"));
            }
            catch (Exception)
            {
                Log.Write("Unable to open command text file.", _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Handles the Click event of the abtWebsiteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void abtWebsiteButton_Click(object sender, EventArgs e)
        {
            Helpers.LaunchUrlInBrowser("http://sst.syncore.org");
        }

        /// <summary>
        /// Automatically checks for updates if specified by user.
        /// </summary>
        private void AutoCheckForUpdates()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            if (!cfg.CoreOptions.checkForUpdatesOnStart) return;
            var checker = new VersionChecker();
            // ReSharper disable once UnusedVariable (synchronous)
            var c = checker.CheckForUpdates(true);
        }

        /// <summary>
        /// Handles the Click event of the banMAddBanButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void banMAddBanButton_Click(object sender, EventArgs e)
        {
            // Validate here
            if (banMUserQlNameTextBox.Text.Length == 0 ||
                !Helpers.IsValidQlUsernameFormat(banMUserQlNameTextBox.Text, false))
            {
                Log.Write(
                    "Invalid QL name entered when attempting to add ban.", _logClassType, _logPrefix);

                ShowErrorMessage("You must specify a valid Quake Live name.",
                    "Invalid QL name");

                return;
            }

            double duration;
            if (banMBanDurationTextBox.Text.Length == 0 ||
                !double.TryParse(banMBanDurationTextBox.Text, out duration) ||
                Math.Abs(duration) <= 0)
            {
                Log.Write(
                    "Invalid duration entered when attempting to add ban.",
                    _logClassType, _logPrefix);

                ShowErrorMessage("You must specify a valid duration number.",
                    "Invalid duration");

                return;
            }

            // See if the ban exists
            var banDb = new DbBans();
            var user = banMUserQlNameTextBox.Text;
            var cfg = _cfgHandler.ReadConfiguration();

            if (banDb.UserAlreadyBanned(user))
            {
                Log.Write(string.Format(
                    "Owner {0} attempted to add ban for user {1} but {1} is already banned.",
                    cfg.CoreOptions.owner, user), _logClassType, _logPrefix);

                ShowErrorMessage(
                    string.Format(
                        "User {0} already exists in the ban database. Remove the user then re-add.",
                        user), "User exists");

                return;
            }

            var scale = (string)banMBanDurationScaleComboBox.SelectedItem;
            var expiration = ExpirationDateGenerator.GenerateExpirationDate(duration, scale);
            banDb.AddUserToDb(user, cfg.CoreOptions.owner, DateTime.Now, expiration, BanType.AddedByAdmin);

            // Kickban using QL internal system immediately
            if (_sst.IsMonitoringServer)
            {
                await _sst.QlCommands.CustCmdKickban(user);
            }

            RefreshCurrentBansDataSource();
            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);
            banMBanDurationScaleComboBox.SelectedIndex = 0;
            banMBanDurationTextBox.Clear();
            banMUserQlNameTextBox.Clear();

            Log.Write(string.Format(
                "Owner {0} added ban of length {1} {2} for user: {3} expires on {4} to ban database.",
                cfg.CoreOptions.owner, duration, scale, user,
                expiration.ToString("G", DateTimeFormatInfo.InvariantInfo)),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the banMDelAllBansButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void banMDelAllBansButton_Click(object sender, EventArgs e)
        {
            _cfgHandler.ReadConfiguration();

            var banDb = new DbBans();
            var allBans = banDb.GetAllBans();
            var cfg = _cfgHandler.ReadConfiguration();

            if (allBans.Count == 0)
            {
                Log.Write(
                    string.Format(
                        "Owner {0} attempted to clear all bans from ban database, but no bans exist.",
                        cfg.CoreOptions.owner), _logClassType, _logPrefix);

                ShowErrorMessage("There are no bans to remove.",
                    "No expired bans");
                return;
            }

            banMCurrentBanBindingSource.Clear();
            foreach (var ban in allBans)
            {
                banDb.DeleteUserFromDb(ban.PlayerName);
                if (_sst.IsMonitoringServer)
                {
                    // remove from QL's internal temporary ban system
                    await _sst.QlCommands.CmdUnban(ban.PlayerName);
                }
            }

            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentBansDataSource();

            Log.Write(string.Format("Owner {0} cleared all bans from the ban database.",
                cfg.CoreOptions.owner), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the banMDelBanButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void banMDelBanButton_Click(object sender, EventArgs e)
        {
            if (banMCurrentBanBindingSource.Count == 0 ||
                banMCurBansListBox.SelectedIndex == -1) return;

            var cfg = _cfgHandler.ReadConfiguration();
            var banDb = new DbBans();
            var selectedUser = (BanInfo)banMCurBansListBox.SelectedItem;

            banMCurrentBanBindingSource.Remove(selectedUser);
            banDb.DeleteUserFromDb(selectedUser.PlayerName);

            if (_sst.IsMonitoringServer)
            {
                // remove from QL's internal temporary ban system
                await _sst.QlCommands.CmdUnban(selectedUser.PlayerName);
            }

            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentBansDataSource();

            Log.Write(
                string.Format(
                    "Owner {0} removed ban for user: {1} from ban database. Ban originally added: {2}" +
                    " was set to expire on {3}",
                    cfg.CoreOptions.owner, selectedUser.PlayerName,
                    selectedUser.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                    selectedUser.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the banMDelExpiredBansButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void banMDelExpiredBansButton_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            var banDb = new DbBans();
            var expiredBans = banDb.GetAllBans().Where
                (b => (b.BanExpirationDate != default(DateTime) &&
                       DateTime.Now > b.BanExpirationDate)).ToList();

            if (expiredBans.Count == 0)
            {
                Log.Write(
                    string.Format("Owner {0} attempted to remove all expired bans from ban" +
                                  " database, but no expired bans exist.", cfg.CoreOptions.owner),
                    _logClassType, _logPrefix);
                ShowErrorMessage("There are no expired bans to remove.",
                    "No expired bans");
                return;
            }

            var bManager = new BanManager(_sst);

            foreach (var ban in expiredBans)
            {
                await bManager.RemoveBan(ban, false);
            }

            banMCurBansListBox.SelectedIndex = ((banMCurrentBanBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentBansDataSource();

            Log.Write(string.Format(
                "Owner {0} cleared all {1} expired bans from the ban database.",
                cfg.CoreOptions.owner, expiredBans.Count), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the clearLogEventsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void clearLogEventsButton_Click(object sender, EventArgs e)
        {
            logConsoleTextBox.Clear();
        }

        /// <summary>
        /// Handles the Click event of the closeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void closeButton_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            _cfgHandler.WriteConfiguration(cfg);
            Close();
        }

        /// <summary>
        /// Handles the Click event of the copyLogEventsClipboardButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void copyLogEventsClipboardButton_Click(object sender, EventArgs e)
        {
            if (logConsoleTextBox.TextLength != 0)
            {
                Clipboard.SetText(logConsoleTextBox.Text);
                ShowInfoMessage("Copied SST event log to clipboard.", "Copied");
            }
        }

        /// <summary>
        /// Handles the Validated event of the coreAccountNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void coreAccountNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreAccountNameTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the coreAccountNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the coreEloCacheTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void coreEloCacheTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreEloCacheTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the coreEloCacheTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the coreLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void coreLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateCoreOptionsUi();
            Log.Write("Core options settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Core options settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the coreOwnerNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void coreOwnerNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreOwnerNameTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the coreOwnerNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the coreResetAllButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void coreResetAllButton_Click(object sender, EventArgs e)
        {
            _cfgHandler.VerifyConfigLocation();
            _cfgHandler.RestoreDefaultConfiguration();
            var cfg = _cfgHandler.ReadConfiguration();

            await HandleAccountDateModActivation(cfg.AccountDateOptions.isActive,
                cfg.AccountDateOptions.minimumDaysRequired);
            await HandleEloLimitModActivation(cfg.EloLimitOptions.isActive);
            await HandlePickupModActivation(cfg.PickupOptions.isActive);

            await HandleCoreSettingsUpdate(cfg.CoreOptions);

            HandleMotdModActivation(cfg.MotdOptions.isActive);
            HandleIrcModActivation(cfg.IrcOptions.isActive);

            HandleStandardModuleActivation(_sst.Mod.Accuracy,
                cfg.AccuracyOptions.isActive);
            HandleStandardModuleActivation(_sst.Mod.AutoVoter,
                cfg.AutoVoterOptions.isActive);
            HandleStandardModuleActivation(_sst.Mod.EarlyQuit,
                cfg.EarlyQuitOptions.isActive);
            HandleStandardModuleActivation(_sst.Mod.Servers,
                cfg.ServersOptions.isActive);

            PopulateAllUiTabs();

            Log.Write(
                "ALL SST settings (including modules) were reset to their default values.",
                _logClassType, _logPrefix);
            ShowInfoMessage("All SST settings (including modules) were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the coreResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void coreResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.CoreOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            PopulateCoreOptionsUi();
            await HandleCoreSettingsUpdate(cfg.CoreOptions);
            Log.Write("Core settings were reset to their default values",
                _logClassType, _logPrefix);
            ShowInfoMessage("Core settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the coreSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void coreSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.CoreOptions.accountName = coreAccountNameTextBox.Text;
                cfg.CoreOptions.appendToActivityLog = coreAppendEventsCheckBox.Checked;
                cfg.CoreOptions.autoOpAdmins = coreAutoOpAdminsCheckBox.Checked;
                cfg.CoreOptions.autoMonitorServerOnStart = coreAutoMonitorStartCheckBox.Checked;
                cfg.CoreOptions.checkForUpdatesOnStart = coreCheckForUpdatesCheckBox.Checked;
                cfg.CoreOptions.eloCacheExpiration = uint.Parse(coreEloCacheTextBox.Text);
                cfg.CoreOptions.requiredTimeBetweenCommands = double.Parse(coreTimeCommandTextBox.Text);
                cfg.CoreOptions.hideAllQlConsoleText = coreHideQlConsoleCheckBox.Checked;
                cfg.CoreOptions.logSstEventsToDisk = coreLogEventsDiskCheckBox.Checked;
                cfg.CoreOptions.minimizeToTray = coreMinimizeToTrayCheckBox.Checked;
                cfg.CoreOptions.owner = coreOwnerNameTextBox.Text;
                _cfgHandler.WriteConfiguration(cfg);
                await HandleCoreSettingsUpdate(cfg.CoreOptions);
                Log.Write("Core settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Core settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write("Validation error prevented core settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Validated event of the coreTimeCommandTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void coreTimeCommandTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(coreTimeCommandTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the coreTimeCommandTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        private void coreTimeCommandTextBox_Validating(object sender, CancelEventArgs e)
        {
            string errorMsg;
            if (!_coreOptionsValidator.IsValidTimeBetweenCommands(coreTimeCommandTextBox.Text, out errorMsg))
            {
                e.Cancel = true;
                coreAccountNameTextBox.Select(0, coreTimeCommandTextBox.Text.Length);
                errorProvider.SetError(coreTimeCommandTextBox, errorMsg);
            }
        }

        /// <summary>
        /// Handles the account date module activation.
        /// </summary>
        /// <param name="isActiveInUi">
        /// if set to <c>true</c> then the module is enabled in the UI.
        /// </param>
        /// <param name="minAccountAge">The minimum account age.</param>
        private async Task HandleAccountDateModActivation(bool isActiveInUi, uint minAccountAge)
        {
            _sst.Mod.AccountDateLimit.Active = isActiveInUi;
            Log.Write(string.Format("{0}", (isActiveInUi)
                ? "Activating account date limiter module from UI. Updating old values as necessary."
                : "Deactivating account date limiter module from UI if active."),
                _logClassType, _logPrefix);

            if (!_sst.IsMonitoringServer) return;
            await _sst.Mod.AccountDateLimit.EnableAccountDateLimiter(minAccountAge);
        }

        /// <summary>
        /// Handles the core settings update.
        /// </summary>
        /// <param name="coreOptions">The core options.</param>
        private async Task HandleCoreSettingsUpdate(CoreOptions coreOptions)
        {
            // Go into effect now
            _sst.AccountName = coreOptions.accountName;
            Log.LogToDisk = coreOptions.logSstEventsToDisk;
            Log.LogToSstConsole = coreOptions.appendToActivityLog;
            var qlw = new QlWindowUtils();
            if (qlw.QuakeLiveConsoleWindowExists())
            {
                if (coreOptions.hideAllQlConsoleText)
                {
                    _sst.QlCommands.DisableConsolePrinting();
                }
                else
                {
                    _sst.QlCommands.EnableConsolePrinting();
                }
            }
            // Add the owner (via constructor)
            var userDb = new DbUsers();
            // Also add bot account as owner
            if (!coreOptions.accountName.Equals(CoreOptions.defaultUnsetOwnerName,
                StringComparison.InvariantCultureIgnoreCase))
            {
                userDb.AddUserToDb(coreOptions.accountName, UserLevel.Owner, "AUTO",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            // Auto-op admins
            await _sst.ServerEventProcessor.AutoOpActiveAdmins();
        }

        /// <summary>
        /// Handles the Elo limiter module activation.
        /// </summary>
        /// <param name="isActiveInUi">
        /// if set to <c>true</c> then the module is enabled in the UI.
        /// </param>
        private async Task HandleEloLimitModActivation(bool isActiveInUi)
        {
            _sst.Mod.EloLimit.Active = isActiveInUi;
            Log.Write(string.Format("{0}", (isActiveInUi)
                ? "Activating Elo limiter module from UI. Updating old values as necessary."
                : "Deactivating Elo limiter module from UI if active."),
                _logClassType, _logPrefix);

            if (!_sst.IsMonitoringServer) return;
            await _sst.Mod.EloLimit.BatchRemoveEloPlayers();
        }

        /// <summary>
        /// Handles the IRC module activation.
        /// </summary>
        /// <param name="isActiveInUi">
        /// if set to <c>true</c> then the module is enabled in the UI.
        /// </param>
        private void HandleIrcModActivation(bool isActiveInUi)
        {
            _sst.Mod.Irc.Active = isActiveInUi;
            Log.Write(string.Format("{0}", (isActiveInUi)
                ? "Activating IRC module from UI. Updating old values as necessary."
                : "Deactivating IRC module from UI if active."),
                _logClassType, _logPrefix);

            if (isActiveInUi)
            {
                _sst.Mod.Irc.Init();
            }
            else
            {
                _sst.Mod.Irc.Deactivate();
            }
        }

        /// <summary>
        /// Handles the MOTD module activation.
        /// </summary>
        /// ///
        /// <param name="isActiveInUi">
        /// if set to <c>true</c> then the module is enabled in the UI.
        /// </param>
        private void HandleMotdModActivation(bool isActiveInUi)
        {
            _sst.Mod.Motd.Active = isActiveInUi;
            Log.Write(string.Format("{0}", (isActiveInUi)
                ? "Activating MOTD module from UI. Updating old values as necessary."
                : "Deactivating MOTD module from UI if active."), _logClassType, _logPrefix);

            if (isActiveInUi)
            {
                _sst.Mod.Motd.Init();
            }
            else
            {
                _sst.Mod.Motd.Deactivate();
            }
        }

        /// <summary>
        /// Handles the pickup module activation.
        /// </summary>
        /// ///
        /// <param name="isActiveInUi">
        /// if set to <c>true</c> then the module is enabled in the UI.
        /// </param>
        private async Task HandlePickupModActivation(bool isActiveInUi)
        {
            _sst.Mod.Pickup.Active = isActiveInUi;
            Log.Write(string.Format("{0}", (isActiveInUi)
                ? "Activating pickup module from UI. Updating old values as necessary."
                : "Deactivating pickup module from UI if active."), _logClassType, _logPrefix);

            if (!isActiveInUi)
            {
                _sst.Mod.Pickup.Active = false;
                if (_sst.IsMonitoringServer)
                {
                    _sst.Mod.Pickup.Manager.ResetPickupStatus();
                    await _sst.QlCommands.SendToQlAsync("unlock", false);
                }
            }
        }

        /// <summary>
        /// Handles the standard module activation.
        /// </summary>
        /// <param name="module">The module.</param>
        /// ///
        /// <param name="isActiveInUi">
        /// if set to <c>true</c> then the module is enabled in the UI.
        /// </param>
        /// <remarks>
        /// This is the module activation method used for modules that do not require any special
        /// actions (i.e. initilization methods) to occur when being enabled or and do not require
        /// async. Currently these are: acc, autovoter, earlyquit, servers.
        /// </remarks>
        private void HandleStandardModuleActivation(IModule module, bool isActiveInUi)
        {
            module.Active = isActiveInUi;
            Log.Write(string.Format("{0}", (isActiveInUi)
                ? string.Format("Activating {0} module from UI. Updating old values as necessary.",
                    module.ModuleName)
                : string.Format("Deactivating {0} module from UI if active.",
                    module.ModuleName)), _logClassType, _logPrefix);
        }

        private void logConsoleTextBox_VisibleChanged(object sender, EventArgs e)
        {
            // Auto scroll to bottom, even when 'Log' tab loses focus.
            if (logConsoleTextBox.Visible)
            {
                logConsoleTextBox.InvokeIfRequired(c =>
                {
                    c.SelectionStart = logConsoleTextBox.TextLength;
                    c.ScrollToCaret();
                });
            }
        }

        /// <summary>
        /// Handles the Click event of the minimizeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void minimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            var cfg = _cfgHandler.ReadConfiguration();
            if (cfg.CoreOptions.minimizeToTray)
            {
                Hide();
                sysTrayIcon.Visible = true;
            }
        }

        /// <summary>
        /// Handles the Validated event of the modAccDateAccAgeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAccDateAccAgeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modAccDateAccAgeTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modAccDateAccAgeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modAccDateLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAccDateLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModAccountDateUi();
            Log.Write(
                "Account date limiter options settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Account date limiter settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modAccDateResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modAccDateResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.AccountDateOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            await
                HandleAccountDateModActivation(cfg.AccountDateOptions.isActive,
                    cfg.AccountDateOptions.minimumDaysRequired);
            PopulateModAccountDateUi();
            Log.Write(
                "Account date limiter settings were reset to their default values",
                _logClassType, _logPrefix);
            ShowInfoMessage("Account date limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modAccDateSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modAccDateSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.AccountDateOptions.isActive = modAccDateEnableCheckBox.Checked;
                cfg.AccountDateOptions.minimumDaysRequired = uint.Parse(modAccDateAccAgeTextBox.Text);
                _cfgHandler.WriteConfiguration(cfg);
                await
                    HandleAccountDateModActivation(cfg.AccountDateOptions.isActive,
                        cfg.AccountDateOptions.minimumDaysRequired);
                UpdateActiveModulesStatusText();
                Log.Write("Account date limiter settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Account date limiter settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write(
                    "Validation error prevented account date limiter settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Click event of the modAccuracyLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAccuracyLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModAccuracyUi();
            Log.Write("Accuracy display settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Accuracy display settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modAccuracyResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAccuracyResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.AccuracyOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            HandleStandardModuleActivation(_sst.Mod.Accuracy, cfg.AccuracyOptions.isActive);
            PopulateModAccuracyUi();
            Log.Write(
                "Accuracy display settings were reset to their default values",
                _logClassType, _logPrefix);
            ShowInfoMessage("Accuracy display settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modAccuracySaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAccuracySaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.AccuracyOptions.isActive = modAccuracyEnableCheckBox.Checked;
                _cfgHandler.WriteConfiguration(cfg);
                HandleStandardModuleActivation(_sst.Mod.Accuracy, cfg.AccuracyOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("Accuracy display settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Accuracy display settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write("Validation error prevented accuracy settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Click event of the modAutoVoterAddVoteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterAddVoteButton_Click(object sender, EventArgs e)
        {
            var containsParam = (!string.IsNullOrEmpty(modAutoVoterContainingTextBox.Text));
            var fullVoteText = string.Format("{0} {1}",
                modAutoVoterVoteTypeComboxBox.SelectedValue,
                (containsParam) ? modAutoVoterContainingTextBox.Text : string.Empty).ToLowerInvariant().Trim();

            if (_sst.Mod.AutoVoter.AutoVotes.Any(v => v.VoteText.Equals(fullVoteText,
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

            var cfg = _cfgHandler.ReadConfiguration();

            modAutoVoterCurrentVotesBindingSource.Add(new AutoVote(fullVoteText,
                containsParam, intendedResult, cfg.CoreOptions.owner));

            RefreshCurrentVotesDataSource();
            modAutoVoterCurVotesListBox.SelectedIndex = ((modAutoVoterCurrentVotesBindingSource.Count > 0)
                ? 0
                : -1);
            modAutoVoterVoteTypeComboxBox.SelectedIndex = 0;
            modAutoVoterContainingTextBox.Clear();

            Log.Write(string.Format("Owner {0} added auto {1} vote for: {2}",
                cfg.CoreOptions.owner, ((intendedResult == IntendedVoteResult.No) ? "NO" : "YES"),
                fullVoteText), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the modAutoVoterClearVotesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterClearVotesButton_Click(object sender, EventArgs e)
        {
            modAutoVoterCurrentVotesBindingSource.Clear();
            modAutoVoterEnableCheckBox.Checked = false;
            HandleStandardModuleActivation(_sst.Mod.AutoVoter, modAutoVoterEnableCheckBox.Checked);
            modAutoVoterCurVotesListBox.SelectedIndex = ((modAutoVoterCurrentVotesBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentVotesDataSource();

            // Disable auto voter since there are now no votes
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.AutoVoterOptions.isActive = false;
            cfg.AutoVoterOptions.autoVotes = new List<AutoVote>();
            _cfgHandler.WriteConfiguration(cfg);

            Log.Write("All automatic votes were cleared by owner.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the modAutoVoterDelVoteButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterDelVoteButton_Click(object sender, EventArgs e)
        {
            if (modAutoVoterCurrentVotesBindingSource.Count == 0 ||
                modAutoVoterCurVotesListBox.SelectedIndex == -1) return;

            var selectedVote = (AutoVote)modAutoVoterCurVotesListBox.SelectedItem;
            modAutoVoterCurrentVotesBindingSource.Remove(selectedVote);
            Log.Write(string.Format("Owner removed auto {0} vote: {1}",
                ((selectedVote.IntendedResult == IntendedVoteResult.No) ? "NO" : "YES"),
                selectedVote.VoteText), _logClassType, _logPrefix);

            // Set appropriate index to prevent weird ArgumentOutOfRangeException when
            //re-binding the datasource (RefreshCurrentVotesDataSource())
            modAutoVoterCurVotesListBox.SelectedIndex = ((modAutoVoterCurrentVotesBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentVotesDataSource();
        }

        /// <summary>
        /// Handles the Click event of the modAutoVoterLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModAutoVoterUi();
            Log.Write("Auto voter display settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Auto voter display settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the CheckedChanged event of the modAutoVoterPassRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterPassRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the modAutoVoterRejectRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterRejectRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        /// <summary>
        /// Handles the Click event of the modAutoVoterResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.AutoVoterOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            HandleStandardModuleActivation(_sst.Mod.AutoVoter, cfg.AutoVoterOptions.isActive);
            PopulateModAutoVoterUi();
            Log.Write(
                "Auto voter settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("Auto voter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modAutoVoterSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.AutoVoterOptions.isActive = modAutoVoterEnableCheckBox.Checked;
                cfg.AutoVoterOptions.autoVotes = _sst.Mod.AutoVoter.AutoVotes;
                _cfgHandler.WriteConfiguration(cfg);
                HandleStandardModuleActivation(_sst.Mod.AutoVoter, cfg.AutoVoterOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("Auto voter settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Auto voter settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write(
                    "Validation error prevented auto voter settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the modAutoVoterVoteTypeComboxBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modAutoVoterVoteTypeComboxBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetAutoVoteOptionalText();
        }

        /// <summary>
        /// Handles the Click event of the modEarlyQuitClearQuitsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modEarlyQuitClearQuitsButton_Click(object sender, EventArgs e)
        {
            var earlyQuitDb = new DbQuits();

            // Initial read of database
            RefreshCurrentQuittersDataSource();

            foreach (var p in modEarlyQuitCurrentQuitBindingSource.List)
            {
                var player = (EarlyQuitter)p;
                earlyQuitDb.DeleteUserFromDb(player.Name);
                await earlyQuitDb.RemoveQuitRelatedBan(_sst, player.Name);
            }

            modEarlyQuitCurQuitsListBox.SelectedIndex = ((modEarlyQuitCurrentQuitBindingSource.Count > 0)
                ? 0
                : -1);

            // Update
            RefreshCurrentQuittersDataSource();
            Log.Write("Cleared early quit database.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the modEarlyQuitDelQuitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
            await earlyQuitDb.RemoveQuitRelatedBan(_sst, player.Name);
            Log.Write(string.Format("Removed {0} from early quit database",
                player.Name), _logClassType, _logPrefix);

            modEarlyQuitCurQuitsListBox.SelectedIndex = ((modEarlyQuitCurrentQuitBindingSource.Count > 0)
                ? 0
                : -1);

            RefreshCurrentQuittersDataSource();
        }

        /// <summary>
        /// Handles the Click event of the modEarlyQuitForgiveQuitButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
                await earlyQuitDb.RemoveQuitRelatedBan(_sst, player.Name);
                RefreshCurrentQuittersDataSource();
                return;
            }

            earlyQuitDb.DecrementUserQuitCount(player.Name, 1);
            Log.Write(string.Format("Forgave 1 early quit for {0}",
                player.Name), _logClassType, _logPrefix);
            RefreshCurrentQuittersDataSource();
        }

        /// <summary>
        /// Handles the Click event of the modEarlyQuitLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEarlyQuitLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModEarlyQuitUi();
            Log.Write("Early quit banner settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Early quit banner settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the modEarlyQuitMaxQuitsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEarlyQuitMaxQuitsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEarlyQuitMaxQuitsTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modEarlyQuitMaxQuitsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modEarlyQuitResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEarlyQuitResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.EarlyQuitOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            HandleStandardModuleActivation(_sst.Mod.EarlyQuit, cfg.EarlyQuitOptions.isActive);
            PopulateModEarlyQuitUi();
            Log.Write(
                "Early quit banner settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("Early quit banner settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modEarlyQuitSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEarlyQuitSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.EarlyQuitOptions.isActive = modEarlyQuitEnableCheckBox.Checked;
                cfg.EarlyQuitOptions.maxQuitsAllowed = uint.Parse(modEarlyQuitMaxQuitsTextBox.Text);
                cfg.EarlyQuitOptions.banTime = double.Parse(modEarlyQuitTimeTextBox.Text);
                cfg.EarlyQuitOptions.banTimeScale = (string)modEarlyQuitTimeScaleComboxBox.SelectedItem;
                cfg.EarlyQuitOptions.banTimeScaleIndex = modEarlyQuitTimeScaleComboxBox.SelectedIndex;
                _cfgHandler.WriteConfiguration(cfg);

                // Go into effect now
                _sst.Mod.EarlyQuit.MaxQuitsAllowed = cfg.EarlyQuitOptions.maxQuitsAllowed;
                _sst.Mod.EarlyQuit.BanTime = cfg.EarlyQuitOptions.banTime;
                _sst.Mod.EarlyQuit.BanTimeScale = cfg.EarlyQuitOptions.banTimeScale;
                _sst.Mod.EarlyQuit.BanTimeScaleIndex = cfg.EarlyQuitOptions.banTimeScaleIndex;

                HandleStandardModuleActivation(_sst.Mod.EarlyQuit, cfg.EarlyQuitOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("Early quit banner settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Early quit banner settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write(
                    "Validation error prevented early quit banner settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Validated event of the modEarlyQuitTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEarlyQuitTimeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEarlyQuitTimeTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modEarlyQuitTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modEloLimiterLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEloLimiterLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModEloLimiterUi();
            Log.Write("Elo limiter settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Elo limiter settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the modEloLimiterMaxEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEloLimiterMaxEloTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEloLimiterMaxEloTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modEloLimiterMaxEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modEloLimiterMinEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modEloLimiterMinEloTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modEloLimiterMinEloTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modEloLimiterMinEloTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modEloLimiterResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modEloLimiterResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.EloLimitOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            _cfgHandler.ReadConfiguration();
            await HandleEloLimitModActivation(cfg.EloLimitOptions.isActive);
            PopulateModEloLimiterUi();
            Log.Write(
                "Elo limiter settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("Elo limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modEloLimiterSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modEloLimiterSaveSettingsPictureBox_Click(object sender, EventArgs e)
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
                    Log.Write(
                        "Maximum Elo was less than minimum Elo when attempting to save Elo" +
                        " limiter settings. Cannot save.", _logClassType, _logPrefix);
                    ShowErrorMessage("The maximum elo value cannot be less than the minimum Elo value.",
                        "Errors Detected");
                    modEloLimiterMaxEloTextBox.Clear();
                    return;
                }

                var cfg = _cfgHandler.ReadConfiguration();
                cfg.EloLimitOptions.isActive = modEloLimiterEnableCheckBox.Checked;
                cfg.EloLimitOptions.minimumRequiredElo = minElo;
                cfg.EloLimitOptions.maximumRequiredElo = ((modEloLimiterMaxEloTextBox.Text.Length == 0)
                    ? 0
                    : maxElo);
                _cfgHandler.WriteConfiguration(cfg);

                // Go into effect now
                _sst.Mod.EloLimit.MinimumRequiredElo = cfg.EloLimitOptions.minimumRequiredElo;
                _sst.Mod.EloLimit.MaximumRequiredElo = cfg.EloLimitOptions.maximumRequiredElo;

                await HandleEloLimitModActivation(cfg.EloLimitOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("Elo limiter settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Elo limiter settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write("Validation error prevented Elo limiter settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Validated event of the modIRCAdminNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCAdminNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCAdminNameTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCAdminNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCBotNickNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCBotNickNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCBotNickNameTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCBotNickNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCBotUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCBotUserNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCBotUserNameTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCBotUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCChannelKeyTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCChannelKeyTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCChannelKeyTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCChannelKeyTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCChannelTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCChannelTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCChannelTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCChannelTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modIRCGenerateRandomNamesButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCGenerateRandomNamesButton_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            modIRCBotNickNameTextBox.Text = string.Format("SST|QLive-{0}",
                cfg.IrcOptions.GenerateRandomIdentifier());
            modIRCBotUserNameTextBox.Text = string.Format("sstQL{0}",
                cfg.IrcOptions.GenerateRandomIdentifier());
        }

        /// <summary>
        /// Handles the Click event of the modIRCLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModIrcUi();
            Log.Write("IRC settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("IRC settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the modIRCQNetPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCQNetPassTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCQNetPassTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCQNetPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCQNetUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCQNetUserNameTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCQNetUserNameTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCQNetUserNameTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modIRCResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.IrcOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            HandleIrcModActivation(cfg.IrcOptions.isActive);
            PopulateModIrcUi();
            Log.Write("IRC settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("IRC settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modIRCSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCSaveSettingsPictureBox_Click(object sender, EventArgs e)
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
                        Log.Write("Auto-Q auth or auto hide hostname was specified in IRC" +
                                  " settings, but Q user/pass not specified. Cannot save.",
                            _logClassType, _logPrefix);
                        ShowErrorMessage(
                            "You must specify a QuakeNet Q username & password to auto-auth with Q.",
                            "Error");
                        modIRCQNetUserNameTextBox.Clear();
                        modIRCQNetPassTextBox.Clear();
                        return;
                    }
                }

                var cfg = _cfgHandler.ReadConfiguration();
                cfg.IrcOptions.isActive = modIRCEnableCheckBox.Checked;
                cfg.IrcOptions.ircAdminNickname = modIRCAdminNameTextBox.Text;
                cfg.IrcOptions.ircNickName = modIRCBotNickNameTextBox.Text;
                cfg.IrcOptions.ircUserName = modIRCBotUserNameTextBox.Text;
                cfg.IrcOptions.ircServerAddress = modIRCServerAddressTextBox.Text;
                cfg.IrcOptions.ircServerPort = uint.Parse(modIRCServerPortTextBox.Text);
                cfg.IrcOptions.ircServerPassword = modIRCServerPassTextBox.Text;
                cfg.IrcOptions.ircChannel = modIRCChannelTextBox.Text;
                cfg.IrcOptions.ircChannelKey = modIRCChannelKeyTextBox.Text;
                cfg.IrcOptions.autoConnectOnStart = modIRCAutoConnectCheckBox.Checked;
                cfg.IrcOptions.ircNickServiceUsername = modIRCQNetUserNameTextBox.Text;
                cfg.IrcOptions.ircNickServicePassword = modIRCQNetPassTextBox.Text;
                cfg.IrcOptions.autoAuthWithNickService = modIRCQNetAutoAuthCheckBox.Checked;
                cfg.IrcOptions.hideHostnameOnQuakeNet = modIRCQNetHideHostCheckBox.Checked;
                _cfgHandler.WriteConfiguration(cfg);

                // Go into effect now
                _sst.Mod.Irc.IrcManager.IrcSettings.ircAdminNickname = cfg.IrcOptions.ircAdminNickname;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircNickName = cfg.IrcOptions.ircNickName;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircUserName = cfg.IrcOptions.ircUserName;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircServerAddress = cfg.IrcOptions.ircServerAddress;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircServerPort = cfg.IrcOptions.ircServerPort;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircServerPassword = cfg.IrcOptions.ircServerPassword;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircChannel = cfg.IrcOptions.ircChannel;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircChannelKey = cfg.IrcOptions.ircChannelKey;
                _sst.Mod.Irc.IrcManager.IrcSettings.autoConnectOnStart = cfg.IrcOptions.autoConnectOnStart;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircNickServiceUsername =
                    cfg.IrcOptions.ircNickServiceUsername;
                _sst.Mod.Irc.IrcManager.IrcSettings.ircNickServicePassword =
                    cfg.IrcOptions.ircNickServicePassword;
                _sst.Mod.Irc.IrcManager.IrcSettings.autoAuthWithNickService =
                    cfg.IrcOptions.autoAuthWithNickService;
                _sst.Mod.Irc.IrcManager.IrcSettings.hideHostnameOnQuakeNet =
                    cfg.IrcOptions.hideHostnameOnQuakeNet;

                HandleIrcModActivation(cfg.IrcOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("IRC settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("IRC settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write("Validation error prevented IRC settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Validated event of the modIRCServerAddressTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCServerAddressTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCServerAddressTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCServerAddressTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCServerPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCServerPassTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCServerPassTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCServerPassTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modIRCServerPortTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modIRCServerPortTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modIRCServerPortTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modIRCServerPortTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modMOTDLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modMOTDLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModMotdUi();
            Log.Write("MOTD settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("MOTD settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the modMOTDRepeatMsgTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modMOTDRepeatMsgTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modMOTDRepeatMsgTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modMOTDRepeatMsgTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modMOTDRepeatTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modMOTDRepeatTimeTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modMOTDRepeatTimeTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modMOTDRepeatTimeTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modMOTDResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modMOTDResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.MotdOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            HandleMotdModActivation(cfg.MotdOptions.isActive);
            PopulateModMotdUi();
            Log.Write(
                "Account date limiter settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("Account date limiter settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modMOTDSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modMOTDSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.MotdOptions.isActive = modMOTDEnableCheckBox.Checked;
                cfg.MotdOptions.repeatInterval = int.Parse(modMOTDRepeatTimeTextBox.Text);
                cfg.MotdOptions.message = modMOTDRepeatMsgTextBox.Text;
                _cfgHandler.WriteConfiguration(cfg);
                HandleMotdModActivation(cfg.MotdOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("MOTD settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("MOTD settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write("Validation error prevented MOTD settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Click event of the modPickupLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modPickupLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModPickupUi();
            Log.Write("Pickup settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Pickup settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the modPickupMaxNoShowsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modPickupMaxNoShowsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupMaxNoShowsTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modPickupMaxNoShowsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modPickupMaxSubsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modPickupMaxSubsTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupMaxSubsTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modPickupMaxSubsTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modPickupNoShowsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modPickupNoShowsTimeBanTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupNoShowsTimeBanTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modPickupNoShowsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Validated event of the modPickupPlayersPerTeamTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modPickupPlayersPerTeamTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupPlayersPerTeamTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modPickupPlayersPerTeamTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modPickupResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modPickupResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.PickupOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            await HandlePickupModActivation(cfg.PickupOptions.isActive);
            PopulateModPickupUi();
            Log.Write("Pickup settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("Pickup settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modPickupSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void modPickupSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.PickupOptions.isActive = modPickupEnableCheckBox.Checked;
                cfg.PickupOptions.maxSubsPerPlayer = int.Parse(modPickupMaxSubsTextBox.Text);
                cfg.PickupOptions.maxNoShowsPerPlayer = int.Parse(modPickupMaxNoShowsTextBox.Text);
                cfg.PickupOptions.excessiveSubUseBanTime = double.Parse(modPickupSubsTimeBanTextBox.Text);
                cfg.PickupOptions.excessiveNoShowBanTime = double.Parse(modPickupNoShowsTimeBanTextBox.Text);
                cfg.PickupOptions.excessiveSubUseBanTimeScale =
                    (string)modPickupSubsTimeBanScaleComboBox.SelectedItem;
                cfg.PickupOptions.excessiveNoShowBanTimeScale =
                    (string)modPickupNoShowsTimeBanScaleComboBox.SelectedItem;
                cfg.PickupOptions.excessiveSubUseBanTimeScaleIndex =
                    modPickupSubsTimeBanScaleComboBox.SelectedIndex;
                cfg.PickupOptions.excessiveNoShowBanTimeScaleIndex =
                    modPickupNoShowsTimeBanScaleComboBox.SelectedIndex;
                cfg.PickupOptions.teamSize = int.Parse(modPickupPlayersPerTeamTextBox.Text);
                _cfgHandler.WriteConfiguration(cfg);

                // Go into effect now
                _sst.Mod.Pickup.MaxSubsPerPlayer = cfg.PickupOptions.maxSubsPerPlayer;
                _sst.Mod.Pickup.MaxNoShowsPerPlayer = cfg.PickupOptions.maxNoShowsPerPlayer;
                _sst.Mod.Pickup.ExcessiveSubUseBanTime = cfg.PickupOptions.excessiveSubUseBanTime;
                _sst.Mod.Pickup.ExcessiveNoShowBanTime = cfg.PickupOptions.excessiveNoShowBanTime;
                _sst.Mod.Pickup.ExcessiveSubUseBanTimeScale = cfg.PickupOptions.excessiveSubUseBanTimeScale;
                _sst.Mod.Pickup.ExcessiveNoShowBanTimeScale = cfg.PickupOptions.excessiveNoShowBanTimeScale;
                _sst.Mod.Pickup.ExcessiveSubUseBanTimeScaleIndex =
                    cfg.PickupOptions.excessiveSubUseBanTimeScaleIndex;
                _sst.Mod.Pickup.ExcessiveNoShowBanTimeScaleIndex =
                    cfg.PickupOptions.excessiveNoShowBanTimeScaleIndex;
                _sst.Mod.Pickup.Teamsize = cfg.PickupOptions.teamSize;

                await HandlePickupModActivation(cfg.PickupOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("Pickup settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Pickup settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write("Validation error prevented pickup settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Validated event of the modPickupSubsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modPickupSubsTimeBanTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modPickupSubsTimeBanTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modPickupSubsTimeBanTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modServerListLoadSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modServerListLoadSettingsPictureBox_Click(object sender, EventArgs e)
        {
            PopulateModServerListUi();
            Log.Write("Server list settings loaded from configuration file.",
                _logClassType, _logPrefix);
            ShowInfoMessage("Server list settings loaded.", "Settings Loaded");
        }

        /// <summary>
        /// Handles the Validated event of the modServerListMaxServersTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modServerListMaxServersTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modServerListMaxServersTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modServerListMaxServersTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the modServerListResetSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modServerListResetSettingsPictureBox_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            cfg.ServersOptions.SetDefaults();
            _cfgHandler.WriteConfiguration(cfg);
            HandleStandardModuleActivation(_sst.Mod.Servers, cfg.ServersOptions.isActive);
            PopulateModServerListUi();
            Log.Write(
                "Server list settings were reset to their default values", _logClassType, _logPrefix);
            ShowInfoMessage("Server list settings were reset to their default values.",
                "Defaults Loaded");
        }

        /// <summary>
        /// Handles the Click event of the modServerListSaveSettingsPictureBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modServerListSaveSettingsPictureBox_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                var cfg = _cfgHandler.ReadConfiguration();
                cfg.ServersOptions.isActive = modServerListEnableCheckBox.Checked;
                cfg.ServersOptions.maxServers = int.Parse(modServerListMaxServersTextBox.Text);
                cfg.ServersOptions.timeBetweenQueries = double.Parse(modServerListTimeBetweenTextBox.Text);
                _cfgHandler.WriteConfiguration(cfg);

                // Go into effect now
                _sst.Mod.Servers.TimeBetweenQueries = cfg.ServersOptions.timeBetweenQueries;
                _sst.Mod.Servers.MaxServersToDisplay = cfg.ServersOptions.maxServers;

                HandleStandardModuleActivation(_sst.Mod.Servers, cfg.ServersOptions.isActive);
                UpdateActiveModulesStatusText();
                Log.Write("Server list settings saved.", _logClassType, _logPrefix);
                ShowInfoMessage("Server list settings saved.", "Settings Saved");
            }
            else
            {
                Log.Write(
                    "Validation error prevented server liset settings from being saved.",
                    _logClassType, _logPrefix);
                ShowErrorMessage("Please correct all errors.", "Errors Detected");
            }
        }

        /// <summary>
        /// Handles the Validated event of the modServerListTimeBetweenTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void modServerListTimeBetweenTextBox_Validated(object sender, EventArgs e)
        {
            errorProvider.SetError(modServerListTimeBetweenTextBox, string.Empty);
        }

        /// <summary>
        /// Handles the Validating event of the modServerListTimeBetweenTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
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
        /// Handles the SelectedIndexChanged event of the moduleTabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
        /// Populates all UI tabs.
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
        /// Populates the core options UI tab.
        /// </summary>
        private void PopulateCoreOptionsUi()
        {
            var cfg = _cfgHandler.ReadConfiguration();
            coreAccountNameTextBox.Text = cfg.CoreOptions.accountName;
            coreAppendEventsCheckBox.Checked = cfg.CoreOptions.appendToActivityLog;
            coreAutoOpAdminsCheckBox.Checked = cfg.CoreOptions.autoOpAdmins;
            coreAutoMonitorStartCheckBox.Checked = cfg.CoreOptions.autoMonitorServerOnStart;
            coreCheckForUpdatesCheckBox.Checked = cfg.CoreOptions.checkForUpdatesOnStart;
            coreEloCacheTextBox.Text = cfg.CoreOptions.eloCacheExpiration.ToString();
            coreTimeCommandTextBox.Text = cfg.CoreOptions.requiredTimeBetweenCommands.
                ToString(CultureInfo.InvariantCulture);
            coreHideQlConsoleCheckBox.Checked = cfg.CoreOptions.hideAllQlConsoleText;
            coreLogEventsDiskCheckBox.Checked = cfg.CoreOptions.logSstEventsToDisk;
            // Special case for logging. Set value on population, otherwise it would be ignored
            Log.LogToDisk = coreLogEventsDiskCheckBox.Checked;
            coreMinimizeToTrayCheckBox.Checked = cfg.CoreOptions.minimizeToTray;
            coreOwnerNameTextBox.Text = cfg.CoreOptions.owner;
        }

        /// <summary>
        /// Sets the automatic vote optional text.
        /// </summary>
        private void SetAutoVoteOptionalText()
        {
            modAutoVoterContainingDescLabel.Text =
                string.Format("If empty then ALL {0} votes will {1}",
                    (modAutoVoterVoteTypeComboxBox.SelectedItem), (string.Format("{0}automatically {1}",
                        Environment.NewLine, ((modAutoVoterPassRadioButton.Checked) ? "PASS." : "FAIL."))));
        }

        /// <summary>
        /// Shows an error message box.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="title">The title.</param>
        private void ShowErrorMessage(string text, string title)
        {
            MessageBox.Show(text, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Shows the information message box.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="title">The title.</param>
        private void ShowInfoMessage(string text, string title)
        {
            MessageBox.Show(text, title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the MouseMove event of the sstLogo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void sstLogo_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == 0)
            {
                return;
            }
            Win32Api.ReleaseCapture();
            Win32Api.SendMessage(Handle, Win32Api.WM_NCLBUTTONDOWN, Win32Api.HT_CAPTION, 0);
        }

        /// <summary>
        /// Handles the Click event of the sstResetButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void sstResetButton_Click(object sender, EventArgs e)
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Log.Write(
                    "User attempted to reset monitoring of server but QL window not found. Won't allow.",
                    _logClassType, _logPrefix);
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Log.Write(
                "Got user request to reset server monitoring; Stopping if exists, starting if it doesn't.",
                _logClassType, _logPrefix);

            if (_sst.IsMonitoringServer)
            {
                _sst.StopMonitoring();
            }
            else
            {
                await _sst.BeginMonitoring();
            }
        }

        /// <summary>
        /// Handles the Click event of the sstStartButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void sstStartButton_Click(object sender, EventArgs e)
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Log.Write(
                    "User attempted to start monitoring of server but QL window not found. Won't allow.",
                    _logClassType, _logPrefix);
                MessageBox.Show(@"Unable to locate a running instance of Quake Live!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check for default names
            var cfg = _cfgHandler.ReadConfiguration();
            if (cfg.CoreOptions.accountName.Equals(CoreOptions.defaultUnsetAccountName,
                StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Write("SST account name has not been set. Cannot start.", _logClassType, _logPrefix);
                MessageBox.Show(@"Cannot start. You must first set the SST account name in the core options!",
                    @"Account Name Is Unset",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (cfg.CoreOptions.owner.Equals(CoreOptions.defaultUnsetOwnerName,
                StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Write("SST owner's account name has not been set. Cannot start.", _logClassType,
                    _logPrefix);
                MessageBox.Show(
                    @"Cannot start. You must first set the SST owner's account name in the core options!",
                    @"Owner Name Is Unset",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (_sst.IsMonitoringServer)
            {
                // Do nothing if we're already monitoring
                Log.Write(
                    "Got user's request to start monitoring, but we're already monitoring the server. Ignoring.",
                    _logClassType, _logPrefix);
                MessageBox.Show(@"Already monitoring a Quake Live server!", @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await _sst.BeginMonitoring();
        }

        /// <summary>
        /// Handles the Click event of the sstStopButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void sstStopButton_Click(object sender, EventArgs e)
        {
            if (!_sst.IsMonitoringServer)
            {
                Log.Write(
                    "SST was not previously monitoring server; ignoring user's request to stop monitoring.",
                    _logClassType, _logPrefix);
                return;
            }

            _sst.StopMonitoring();
            Log.Write("Got user request to stop monitoring server. Stopping monitoring.",
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Paint event of the statusPanel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        private void statusPanel_Paint(object sender, PaintEventArgs e)
        {
            // left, top, right, bottom
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                Color.FromArgb(0, 0, 0), 0, ButtonBorderStyle.Solid,
                Color.FromArgb(62, 234, 246, 255), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(0, 0, 0), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(0, 0, 0), 1, ButtonBorderStyle.Solid);
        }

        /// <summary>
        /// Handles the Click event of the sysTrayExitMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void sysTrayExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the MouseClick event of the sysTrayIcon control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
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
        /// Handles the Click event of the sysTrayUpdateMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void sysTrayUpdateMenuItem_Click(object sender, EventArgs e)
        {
            var checker = new VersionChecker();
            await checker.CheckForUpdates(false);
        }

        /// <summary>
        /// Handles the Click event of the sysTrayWebsiteMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void sysTrayWebsiteMenuItem_Click(object sender, EventArgs e)
        {
            Helpers.LaunchUrlInBrowser("http://sst.syncore.org");
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the UiTabCtl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void UiTabCtl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;
            var currentTabPage = tabControl.SelectedTab;

            // For certain tabs, re-populate the tab on switch modules have their own event handler,
            // see: moduleTabControl_SelectedIndexChanged
            if (currentTabPage == coreOptionsTab)
                PopulateCoreOptionsUi();
            else if (currentTabPage == usersTab)
                PopulateUserManagementUi();
            else if (currentTabPage == banTab)
                PopulateBanManagementUi();
        }

        /// <summary>
        /// Updates the active modules status bar text.
        /// </summary>
        private void UpdateActiveModulesStatusText()
        {
            activeModulesLabel.InvokeIfRequired(c =>
            {
                c.Text = _sst.Mod.ActiveModuleCount == 0
                    ? @"No active modules"
                    : string.Format("Active modules: {0}", _sst.Mod.GetActiveModules());
            });
        }

        /// <summary>
        /// Handles the Paint event of the UserInterface control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        private void UserInterface_Paint(object sender, PaintEventArgs e)
        {
            // left, top, right, bottom draw a light border at the top of the status bar
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                Color.FromArgb(104, 234, 246, 255), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(104, 234, 246, 255), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(104, 234, 246, 255), 1, ButtonBorderStyle.Solid,
                Color.FromArgb(104, 234, 246, 255), 1, ButtonBorderStyle.Solid);
        }

        /// <summary>
        /// Handles the Click event of the usrMAddUserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void usrMAddUserButton_Click(object sender, EventArgs e)
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
            var cfg = _cfgHandler.ReadConfiguration();
            var owner = cfg.CoreOptions.owner;
            var accessLevel = (UserLevel)usrMUserAccessComboBox.SelectedItem;
            userDb.AddUserToDb(user, accessLevel, owner,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            RefreshCurrentSstUsersDataSource();
            usrMCurUsersListBox.SelectedIndex = ((usrMCurrentUserBindingSource.Count > 0)
                ? 0
                : -1);
            usrMUserAccessComboBox.SelectedIndex = 0;
            usrMUserQlNameTextBox.Clear();

            Log.Write(
                string.Format("Owner {0} added user {1} with access level {2} to user database.",
                    owner, user, accessLevel), _logClassType, _logPrefix);

            // Auto-op if necessary
            if (accessLevel > UserLevel.SuperUser)
            {
                await _sst.ServerEventProcessor.AutoOpActiveAdmin(user);
            }
        }

        /// <summary>
        /// Handles the Click event of the usrMDelAllUsersButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void usrMDelAllUsersButton_Click(object sender, EventArgs e)
        {
            var cfg = _cfgHandler.ReadConfiguration();
            var owner = cfg.CoreOptions.owner;
            var userDb = new DbUsers();
            var allUsers = userDb.GetAllUsers();

            if (allUsers.Count == 0)
            {
                Log.Write(string.Format("Owner {0} attempted to remove all users from user" +
                                        " database, but no users exist.", owner),
                    _logClassType, _logPrefix);

                ShowErrorMessage("There are no users to remove.",
                    "No users");
                return;
            }

            foreach (var user in allUsers)
            {
                userDb.DeleteUserFromDb(user.Name, owner, UserLevel.Owner);
                if (!_sst.IsMonitoringServer) break;
                if (!_sst.ServerInfo.CurrentPlayers.ContainsKey(user.Name)) continue;
                var id = _sst.ServerEventProcessor.GetPlayerId(user.Name);
                if (id != -1)
                {
                    // doesn't matter if not opped, since QL shows no error message
                    await _sst.QlCommands.SendToQlAsync(string.Format("deop {0}", id), false);
                }
            }

            usrMCurUsersListBox.SelectedIndex = ((usrMCurrentUserBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentSstUsersDataSource();

            Log.Write(string.Format("Owner {0} removed all {1} users from the user database",
                owner, allUsers.Count), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Handles the Click event of the usrMDelUserButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void usrMDelUserButton_Click(object sender, EventArgs e)
        {
            if (usrMCurrentUserBindingSource.Count == 0 ||
                usrMCurUsersListBox.SelectedIndex == -1) return;

            var cfg = _cfgHandler.ReadConfiguration();
            var userDb = new DbUsers();
            var selectedUser = (User)usrMCurUsersListBox.SelectedItem;
            usrMCurrentUserBindingSource.Remove(selectedUser);
            userDb.DeleteUserFromDb(selectedUser.Name, cfg.CoreOptions.owner, UserLevel.Owner);

            usrMCurUsersListBox.SelectedIndex = ((usrMCurrentUserBindingSource.Count > 0)
                ? 0
                : -1);
            RefreshCurrentSstUsersDataSource();

            Log.Write(
                string.Format("Owner {0} removed user {1} with access level {2} from user database.",
                    cfg.CoreOptions.owner, selectedUser.Name, selectedUser.AccessLevel), _logClassType,
                _logPrefix);

            // De-op
            if (!_sst.IsMonitoringServer) return;
            if (!_sst.ServerInfo.CurrentPlayers.ContainsKey(selectedUser.Name)) return;
            var id = _sst.ServerEventProcessor.GetPlayerId(selectedUser.Name);
            if (id != -1)
            {
                // doesn't matter if not opped, since QL shows no error message
                await _sst.QlCommands.SendToQlAsync(string.Format("deop {0}", id), false);
            }
        }
    }
}
