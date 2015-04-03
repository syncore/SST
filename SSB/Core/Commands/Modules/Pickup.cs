using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Modules;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: enable or disable the ability to start pick-up games and specify no-show/excessive sub use ban lengths.
    /// </summary>
    public class Pickup : IModule
    {
        public const string NameModule = "pickup";
        public const int TeamMaxSize = 8;
        public const int TeamMinSize = 3;
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly PickupManager _pickupManager;
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Pickup" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Pickup(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            LoadConfig();
            _pickupManager = new PickupManager(_ssb);
        }

        /// <summary>
        ///     Gets or sets a numeric value representing the time to ban excessive substitutes.
        /// </summary>
        /// <value>
        ///     A numeric value representing the time to ban excessive substitutes.
        /// </value>
        /// <remarks>
        ///     <see cref="ExcessiveNoShowBanTimeScale" /> for the scale that is to be combined with this setting.
        /// </remarks>
        public double ExcessiveNoShowBanTime { get; set; }

        /// <summary>
        ///     Gets or sets the scale that is combined with <see cref="ExcessiveNoShowBanTime" /> that specifies the duration of
        ///     the ban.
        /// </summary>
        /// <value>
        ///     The scale that is combined with <see cref="ExcessiveNoShowBanTime" /> that specifies the duration of the ban.
        /// </value>
        public string ExcessiveNoShowBanTimeScale { get; set; }

        /// <summary>
        /// Gets or sets the index of the excessive no show ban time scale.
        /// </summary>
        /// <value>
        /// The index of the excessive no show ban time scale.
        /// </value>
        public int ExcessiveNoShowBanTimeScaleIndex { get; set; }
        
        /// <summary>
        ///     Gets or sets a numeric value representing the time to ban early quitters.
        /// </summary>
        /// <value>
        ///     A numeric value representing the time to ban early quitters.
        /// </value>
        /// <remarks>
        ///     <see cref="ExcessiveSubUseBanTimeScale" /> for the scale that is to be combined with this setting.
        /// </remarks>
        public double ExcessiveSubUseBanTime { get; set; }

        /// <summary>
        ///     Gets or sets the scale that is combined with <see cref="ExcessiveSubUseBanTime" /> that specifies the duration of
        ///     the ban.
        /// </summary>
        /// <value>
        ///     The scale that is combined with <see cref="ExcessiveSubUseBanTime" /> that specifies the duration of the ban.
        /// </value>
        public string ExcessiveSubUseBanTimeScale { get; set; }

        /// <summary>
        /// Gets or sets the index of the excessive sub use ban time scale.
        /// </summary>
        /// <value>
        /// The index of the excessive sub use ban time scale.
        /// </value>
        public int ExcessiveSubUseBanTimeScaleIndex { get; set; }

        /// <summary>
        ///     Gets the pickup manager.
        /// </summary>
        /// <value>
        ///     The pickup manager.
        /// </value>
        public PickupManager Manager
        {
            get { return _pickupManager; }
        }

        /// <summary>
        ///     Gets or sets the maximum no-shows (leaving a pickup early) without securing a sub,
        ///     that a player can have before being banned for
        ///     <see cref="ExcessiveNoShowBanTime" /> and <see cref="ExcessiveNoShowBanTimeScale" />
        /// </summary>
        /// <value>
        ///     The maximum sub requests that a player can make before being banned.
        /// </value>
        public int MaxNoShowsPerPlayer { get; set; }

        /// <summary>
        ///     Gets or sets the maximum sub requests that a player can make before being banned for
        ///     <see cref="ExcessiveSubUseBanTime" /> and <see cref="ExcessiveSubUseBanTimeScale" />
        /// </summary>
        /// <value>
        ///     The maximum sub requests that a player can make before being banned.
        /// </value>
        /// <remarks>
        ///     Note: this refers to the sub requests that are successfully fulfilled, not the use of the sub
        ///     command itself.
        /// </remarks>
        public int MaxSubsPerPlayer { get; set; }

        /// <summary>
        ///     Gets or sets the teamsize.
        /// </summary>
        /// <value>
        ///     The teamsize.
        /// </value>
        public int Teamsize { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets the minimum module arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum module arguments for the IRC command.
        /// </value>
        public int IrcMinModuleArgs
        {
            get { return _qlMinModuleArgs + 1; }
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the name of the module.
        /// </summary>
        /// <value>
        ///     The name of the module.
        /// </value>
        public string ModuleName
        {
            get { return NameModule; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinModuleArgs
        {
            get { return _qlMinModuleArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c>if the command evaluation was successful,
        ///     otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < (c.FromIrc ? IrcMinModuleArgs : _qlMinModuleArgs))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (Helpers.GetArgVal(c, 2).Equals("off"))
            {
                await DisablePickup(c);
                return false;
            }
            if (Helpers.GetArgVal(c, 2).Equals("noshowbans") || Helpers.GetArgVal(c, 2).Equals("subbans"))
            {
                return await EvalSetBanSettings(c);
            }
            int teamsize;
            if (!int.TryParse(Helpers.GetArgVal(c, 2), out teamsize) || teamsize < TeamMinSize || teamsize > TeamMaxSize)
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 Minimum team size is {0}, maximum team size is {1}.",
                        TeamMinSize, TeamMaxSize);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!_ssb.ServerInfo.IsATeamGame())
            {
                // Might have not gotten it the first time, so request again, in a few seconds.
                await _ssb.QlCommands.SendToQlDelayedAsync("serverinfo", false, 3);
                _ssb.QlCommands.ClearQlWinConsole();
                StatusMessage =
                    "^1[ERROR]^3 Pickup module can only be enabled for team-based games. If this is" +
                    " an error, try again in a few seconds.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            await EnablePickup(c, teamsize);
            return true;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <teamsize> [noshowbans] [subbans] - teamsize is a number",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();

            // Valid values?
            if (_configHandler.Config.PickupOptions.teamSize < TeamMinSize ||
                _configHandler.Config.PickupOptions.teamSize > TeamMaxSize)
            {
                Active = false;
                _configHandler.Config.PickupOptions.SetDefaults();
                return;
            }
            // Valid scales?
            if (
                (!Helpers.ValidTimeScales.Contains(
                    _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale))
                &&
                ((!Helpers.ValidTimeScales.Contains(
                    _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale))))
            {
                Active = false;
                _configHandler.Config.PickupOptions.SetDefaults();
                return;
            }
            
            Active = _configHandler.Config.PickupOptions.isActive;
            MaxNoShowsPerPlayer = _configHandler.Config.PickupOptions.maxNoShowsPerPlayer;
            MaxSubsPerPlayer = _configHandler.Config.PickupOptions.maxSubsPerPlayer;
            ExcessiveNoShowBanTime = _configHandler.Config.PickupOptions.excessiveNoShowBanTime;
            ExcessiveNoShowBanTimeScale = _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale;
            ExcessiveNoShowBanTimeScaleIndex = _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScaleIndex;
            ExcessiveSubUseBanTime = _configHandler.Config.PickupOptions.excessiveSubUseBanTime;
            ExcessiveSubUseBanTimeScale = _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale;
            ExcessiveSubUseBanTimeScaleIndex = _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScaleIndex;
            Teamsize = _configHandler.Config.PickupOptions.teamSize;
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Updates the configuration.
        /// </summary>
        /// <param name="active">
        ///     if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        ///     updating the configuration.
        /// </param>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            _configHandler.Config.PickupOptions.isActive = active;
            _configHandler.Config.PickupOptions.teamSize = Teamsize;
            _configHandler.Config.PickupOptions.maxNoShowsPerPlayer = MaxNoShowsPerPlayer;
            _configHandler.Config.PickupOptions.maxSubsPerPlayer = MaxSubsPerPlayer;
            _configHandler.Config.PickupOptions.excessiveNoShowBanTime = ExcessiveNoShowBanTime;
            _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale = ExcessiveNoShowBanTimeScale;
            _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScaleIndex = ExcessiveNoShowBanTimeScaleIndex;
            _configHandler.Config.PickupOptions.excessiveSubUseBanTime = ExcessiveSubUseBanTime;
            _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale = ExcessiveSubUseBanTimeScale;
            _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScaleIndex = ExcessiveSubUseBanTimeScaleIndex;
            
            _configHandler.WriteConfiguration();

            // Reflect changes in UI
            _ssb.UserInterface.PopulateModPickupUi();
        }

        /// <summary>
        ///     Disables the pickup module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisablePickup(CmdArgs c)
        {
            UpdateConfig(false);
            // Unlock the teams and clear eligible players if any
            _ssb.QlCommands.SendToQl("unlock", false);
            Manager.ResetPickupStatus();
            StatusMessage = "^2[SUCCESS]^7 Pickup game module has been disabled.";
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Enables the pickup module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="teamsize">The teamsize.</param>
        private async Task EnablePickup(CmdArgs c, int teamsize)
        {
            // Note: notice the missing ban settings here.
            // The configuration has some pretty sane defaults, so unless the admin specifically
            // overrides the defaults with noshows/subbans args, then we will just use those.
            UsePickupDefaults();
            // Set the specified teamsize to override the default teamsize, keeping other default settings
            Teamsize = teamsize;
            // Activate the module, overriding the default of active = false
            UpdateConfig(true);
            StatusMessage = string.Format("^2[SUCCESS]^7 Pickup game module has been enabled with" +
                                          " initial teamsize of^2 {0} ^7 - To start: ^2{1}{2} start",
                teamsize, CommandList.GameCommandPrefix, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        CommandList.CmdPickup))
                    : CommandList.CmdPickup));
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Evaluates whether the noshow or sub ban settings can be set based on the user's input.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the noshow or sub ban settings could be set; otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> EvalSetBanSettings(CmdArgs c)
        {
            var settingsType = string.Empty;
            if (Helpers.GetArgVal(c, 2).Equals("noshowbans"))
            {
                settingsType = "noshows";
            }
            else if (Helpers.GetArgVal(c, 2).Equals("subbans"))
            {
                settingsType = "subs";
            }
            if (c.Args.Length != (c.FromIrc ? 7 : 6))
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} {2} {3} <max> <bantime> <banscale> - max: max # {4}, bantime: #, banscale: secs," +
                    " mins, hours, days, months, OR years",
                    CommandList.GameCommandPrefix, c.CmdName,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}", c.Args[1],
                            NameModule))
                        : NameModule), Helpers.GetArgVal(c, 2), settingsType);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            int maxNum;
            if (!int.TryParse(Helpers.GetArgVal(c, 3), out maxNum) || maxNum <= 0)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Max # of {0} to allow must be a number greater than zero.",
                    settingsType);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            double timeNum;
            if (!double.TryParse(Helpers.GetArgVal(c, 4), out timeNum) || timeNum <= 0)
            {
                StatusMessage = "^1[ERROR]^3 The time to ban must be a number greater than zero.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var isValidScale = Helpers.ValidTimeScales.Contains(Helpers.GetArgVal(c, 5));
            if (!isValidScale)
            {
                StatusMessage = "^1[ERROR]^3 Scale must be: secs, mins, hours, days, months OR years.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            await SetBanSettings(c, settingsType, maxNum, timeNum, Helpers.GetArgVal(c, 5));
            return true;
        }

        /// <summary>
        ///     Sets the no-show or excessive subs ban settings.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="bType">Type of ban to set (noshows or subs).</param>
        /// <param name="maxNum">The maximum number of noshows or subs used.</param>
        /// <param name="timeToBan">The time to ban.</param>
        /// <param name="scaleToBan">The scale to ban.</param>
        /// <returns></returns>
        private async Task SetBanSettings(CmdArgs c, string bType, int maxNum, double timeToBan,
            string scaleToBan)
        {
            if (bType.Equals("noshows"))
            {
                MaxNoShowsPerPlayer = maxNum;
                ExcessiveNoShowBanTime = timeToBan;
                ExcessiveNoShowBanTimeScale = scaleToBan;
                ExcessiveNoShowBanTimeScaleIndex = Helpers.GetTimeScaleIndex(scaleToBan);
                StatusMessage = string.Format(
                    "^2[SUCCESS]^7 Players leaving without a sub more than^3 {0} ^7times will be banned for^1 {1} {2}.",
                    maxNum, timeToBan, scaleToBan);
                await SendServerSay(c, StatusMessage);
            }
            else if (bType.Equals("subs"))
            {
                MaxSubsPerPlayer = maxNum;
                ExcessiveSubUseBanTime = timeToBan;
                ExcessiveSubUseBanTimeScale = scaleToBan;
                ExcessiveSubUseBanTimeScaleIndex = Helpers.GetTimeScaleIndex(scaleToBan);
                StatusMessage = string.Format(
                    "^2[SUCCESS]^7 Players who've requested subs more than^3 {0} ^7times will be banned for^1 {1} {2}.",
                    maxNum, timeToBan, scaleToBan);
                await SendServerSay(c, StatusMessage);
            }
            UpdateConfig(true);
        }

        /// <summary>
        ///     Uses the pickup defaults when enabling the pickup module.
        /// </summary>
        private void UsePickupDefaults()
        {
            _configHandler.Config.PickupOptions.SetDefaults();
            MaxNoShowsPerPlayer = _configHandler.Config.PickupOptions.maxNoShowsPerPlayer;
            MaxSubsPerPlayer = _configHandler.Config.PickupOptions.maxSubsPerPlayer;
            ExcessiveNoShowBanTime = _configHandler.Config.PickupOptions.excessiveNoShowBanTime;
            ExcessiveNoShowBanTimeScale = _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale;
            ExcessiveNoShowBanTimeScaleIndex =
                _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScaleIndex;
            ExcessiveSubUseBanTime = _configHandler.Config.PickupOptions.excessiveSubUseBanTime;
            ExcessiveSubUseBanTimeScale =
                _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale;
            ExcessiveSubUseBanTimeScaleIndex =
                _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScaleIndex;
        }
    }
}