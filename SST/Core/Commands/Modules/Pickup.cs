﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Core.Modules;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    /// Module: enable or disable the ability to start pick-up games and specify no-show/excessive
    ///         sub use ban lengths.
    /// </summary>
    public class Pickup : IModule
    {
        public const string NameModule = "pickup";
        public const int TeamMaxSize = 8;
        public const int TeamMinSize = 2;
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:PICKUP]";
        private readonly PickupManager _pickupManager;
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pickup"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public Pickup(SynServerTool sst)
        {
            _sst = sst;
            _configHandler = new ConfigHandler();
            LoadConfig();
            _pickupManager = new PickupManager(_sst);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IModule"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a numeric value representing the time to ban excessive substitutes.
        /// </summary>
        /// <value>A numeric value representing the time to ban excessive substitutes.</value>
        /// <remarks>
        /// <see cref="ExcessiveNoShowBanTimeScale"/> for the scale that is to be combined with this setting.
        /// </remarks>
        public double ExcessiveNoShowBanTime { get; set; }

        /// <summary>
        /// Gets or sets the scale that is combined with <see cref="ExcessiveNoShowBanTime"/> that
        /// specifies the duration of the ban.
        /// </summary>
        /// <value>
        /// The scale that is combined with <see cref="ExcessiveNoShowBanTime"/> that specifies the
        /// duration of the ban.
        /// </value>
        public string ExcessiveNoShowBanTimeScale { get; set; }

        /// <summary>
        /// Gets or sets the index of the excessive no show ban time scale.
        /// </summary>
        /// <value>The index of the excessive no show ban time scale.</value>
        public int ExcessiveNoShowBanTimeScaleIndex { get; set; }

        /// <summary>
        /// Gets or sets a numeric value representing the time to ban early quitters.
        /// </summary>
        /// <value>A numeric value representing the time to ban early quitters.</value>
        /// <remarks>
        /// <see cref="ExcessiveSubUseBanTimeScale"/> for the scale that is to be combined with this setting.
        /// </remarks>
        public double ExcessiveSubUseBanTime { get; set; }

        /// <summary>
        /// Gets or sets the scale that is combined with <see cref="ExcessiveSubUseBanTime"/> that
        /// specifies the duration of the ban.
        /// </summary>
        /// <value>
        /// The scale that is combined with <see cref="ExcessiveSubUseBanTime"/> that specifies the
        /// duration of the ban.
        /// </value>
        public string ExcessiveSubUseBanTimeScale { get; set; }

        /// <summary>
        /// Gets or sets the index of the excessive sub use ban time scale.
        /// </summary>
        /// <value>The index of the excessive sub use ban time scale.</value>
        public int ExcessiveSubUseBanTimeScaleIndex { get; set; }

        /// <summary>
        /// Gets the minimum module arguments for the IRC command.
        /// </summary>
        /// <value>The minimum module arguments for the IRC command.</value>
        public int IrcMinModuleArgs
        {
            get { return _qlMinModuleArgs + 1; }
        }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        /// Gets the pickup manager.
        /// </summary>
        /// <value>The pickup manager.</value>
        public PickupManager Manager
        {
            get { return _pickupManager; }
        }

        /// <summary>
        /// Gets or sets the maximum no-shows (leaving a pickup early) without securing a sub, that
        /// a player can have before being banned for <see cref="ExcessiveNoShowBanTime"/> and <see cref="ExcessiveNoShowBanTimeScale"/>
        /// </summary>
        /// <value>The maximum sub requests that a player can make before being banned.</value>
        public int MaxNoShowsPerPlayer { get; set; }

        /// <summary>
        /// Gets or sets the maximum sub requests that a player can make before being banned for
        /// <see cref="ExcessiveSubUseBanTime"/> and <see cref="ExcessiveSubUseBanTimeScale"/>
        /// </summary>
        /// <value>The maximum sub requests that a player can make before being banned.</value>
        /// <remarks>
        /// Note: this refers to the sub requests that are successfully fulfilled, not the use of
        ///       the sub command itself.
        /// </remarks>
        public int MaxSubsPerPlayer { get; set; }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        /// <value>The name of the module.</value>
        public string ModuleName
        {
            get { return NameModule; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinModuleArgs
        {
            get { return _qlMinModuleArgs; }
        }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the teamsize.
        /// </summary>
        /// <value>The teamsize.</value>
        public int Teamsize { get; set; }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command evaluation was successful, otherwise <c>false</c>.</returns>
        public async Task<bool> EvalModuleCmdAsync(Cmd c)
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
            if (!int.TryParse(Helpers.GetArgVal(c, 2), out teamsize) || teamsize < TeamMinSize ||
                teamsize > TeamMaxSize)
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 Minimum team size is {0}, maximum team size is {1}.",
                        TeamMinSize, TeamMaxSize);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!_sst.ServerInfo.IsATeamGame())
            {
                StatusMessage =
                    "^1[ERROR]^3 Pickup module can only be enabled for team-based games. If this is" +
                    " an error, try again in a few seconds.";
                await SendServerTell(c, StatusMessage);
                // Might have not gotten it the first time, so request again, in a few seconds.
                await _sst.QlCommands.SendToQlDelayedAsync("serverinfo", false, 3);
                _sst.QlCommands.ClearQlWinConsole();
                return false;
            }
            await EnablePickup(c, teamsize);
            return true;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <teamsize> [noshowbans] [subbans] - teamsize is a number",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            var cfg = _configHandler.ReadConfiguration();

            // Valid values?
            if (cfg.PickupOptions.teamSize < TeamMinSize ||
                cfg.PickupOptions.teamSize > TeamMaxSize)
            {
                Log.Write("Invalid team size detected on initial load of pickup module configuration." +
                          " Won't enable. Setting pickup module defaults.",
                    _logClassType, _logPrefix);

                Active = false;
                cfg.PickupOptions.SetDefaults();
                _configHandler.WriteConfiguration(cfg);
                return;
            }
            // Valid scales?
            if (
                (!Helpers.ValidTimeScales.Contains(
                    cfg.PickupOptions.excessiveSubUseBanTimeScale))
                &&
                ((!Helpers.ValidTimeScales.Contains(
                    cfg.PickupOptions.excessiveNoShowBanTimeScale))))
            {
                Log.Write("Invalid time scales detected on initial load of pickup module configuration." +
                          " Won't enable. Setting pickup module defaults.",
                    _logClassType, _logPrefix);

                Active = false;
                cfg.PickupOptions.SetDefaults();
                _configHandler.WriteConfiguration(cfg);
                return;
            }

            Active = cfg.PickupOptions.isActive;
            MaxNoShowsPerPlayer = cfg.PickupOptions.maxNoShowsPerPlayer;
            MaxSubsPerPlayer = cfg.PickupOptions.maxSubsPerPlayer;
            ExcessiveNoShowBanTime = cfg.PickupOptions.excessiveNoShowBanTime;
            ExcessiveNoShowBanTimeScale = cfg.PickupOptions.excessiveNoShowBanTimeScale;
            ExcessiveNoShowBanTimeScaleIndex =
                cfg.PickupOptions.excessiveNoShowBanTimeScaleIndex;
            ExcessiveSubUseBanTime = cfg.PickupOptions.excessiveSubUseBanTime;
            ExcessiveSubUseBanTimeScale = cfg.PickupOptions.excessiveSubUseBanTimeScale;
            ExcessiveSubUseBanTimeScaleIndex =
                cfg.PickupOptions.excessiveSubUseBanTimeScaleIndex;
            Teamsize = cfg.PickupOptions.teamSize;

            Log.Write(string.Format(
                "Active: {0}, max no shows per player: {1}, max subs per player: {2}, no-show ban time: {3} {4}," +
                " sub abuse ban time: {5} {6}, team size: {7}v{7}",
                (Active ? "YES" : "NO"), MaxNoShowsPerPlayer, MaxSubsPerPlayer, ExcessiveNoShowBanTime,
                ExcessiveNoShowBanTimeScale, ExcessiveSubUseBanTime, ExcessiveSubUseBanTimeScale, Teamsize),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message, false);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="active">
        /// if set to <c>true</c> then the module is to remain active; otherwise it is to be
        /// disabled when updating the configuration.
        /// </param>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            var cfg = _configHandler.ReadConfiguration();
            cfg.PickupOptions.isActive = active;
            cfg.PickupOptions.teamSize = Teamsize;
            cfg.PickupOptions.maxNoShowsPerPlayer = MaxNoShowsPerPlayer;
            cfg.PickupOptions.maxSubsPerPlayer = MaxSubsPerPlayer;
            cfg.PickupOptions.excessiveNoShowBanTime = ExcessiveNoShowBanTime;
            cfg.PickupOptions.excessiveNoShowBanTimeScale = ExcessiveNoShowBanTimeScale;
            cfg.PickupOptions.excessiveNoShowBanTimeScaleIndex =
                ExcessiveNoShowBanTimeScaleIndex;
            cfg.PickupOptions.excessiveSubUseBanTime = ExcessiveSubUseBanTime;
            cfg.PickupOptions.excessiveSubUseBanTimeScale = ExcessiveSubUseBanTimeScale;
            cfg.PickupOptions.excessiveSubUseBanTimeScaleIndex =
                ExcessiveSubUseBanTimeScaleIndex;

            _configHandler.WriteConfiguration(cfg);

            // Reflect changes in UI
            _sst.UserInterface.PopulateModPickupUi();
        }

        /// <summary>
        /// Disables the pickup module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisablePickup(Cmd c)
        {
            UpdateConfig(false);
            // Unlock the teams and clear eligible players if any
            _sst.QlCommands.SendToQl("unlock", false);
            Manager.ResetPickupStatus();
            StatusMessage = "^2[SUCCESS]^7 Pickup game module has been disabled.";
            await SendServerSay(c, StatusMessage);

            Log.Write(
                string.Format("Received {0} request from {1} to disable pickup; banner module. Disabling.",
                    (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Enables the pickup module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="teamsize">The teamsize.</param>
        private async Task EnablePickup(Cmd c, int teamsize)
        {
            // Note: notice the missing ban settings here. The configuration has some pretty sane
            // defaults, so unless the admin specifically overrides the defaults with
            // noshows/subbans args, then we will just use those.
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

            Log.Write(string.Format("Received {0} request from {1} to enable pickup module. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Evaluates whether the noshow or sub ban settings can be set based on the user's input.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the noshow or sub ban settings could be set; otherwise <c>false</c>.</returns>
        private async Task<bool> EvalSetBanSettings(Cmd c)
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
        /// Sets the no-show or excessive subs ban settings.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="bType">Type of ban to set (noshows or subs).</param>
        /// <param name="maxNum">The maximum number of noshows or subs used.</param>
        /// <param name="timeToBan">The time to ban.</param>
        /// <param name="scaleToBan">The scale to ban.</param>
        /// <returns></returns>
        private async Task SetBanSettings(Cmd c, string bType, int maxNum, double timeToBan,
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

                Log.Write(
                    string.Format(
                        "Received {0} request from {1} to override default no-show ban settings. Overriding with new values:" +
                        "max no-shows per player: {2}, no-show ban time: {3} {4}",
                        (c.FromIrc ? "IRC" : "in-game"), c.FromUser, MaxNoShowsPerPlayer,
                        ExcessiveNoShowBanTime, ExcessiveNoShowBanTimeScale), _logClassType, _logPrefix);
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

                Log.Write(
                    string.Format(
                        "Received {0} request from {1} to override default sub ban settings. Overriding with new values:" +
                        "max no-subs per player: {2}, sub abuse ban time: {3} {4}",
                        (c.FromIrc ? "IRC" : "in-game"), c.FromUser, MaxSubsPerPlayer,
                        ExcessiveSubUseBanTime, ExcessiveSubUseBanTimeScale), _logClassType, _logPrefix);
            }
            UpdateConfig(true);
        }

        /// <summary>
        /// Uses the pickup defaults when enabling the pickup module.
        /// </summary>
        private void UsePickupDefaults()
        {
            var cfg = _configHandler.ReadConfiguration();
            cfg.PickupOptions.SetDefaults();
            MaxNoShowsPerPlayer = cfg.PickupOptions.maxNoShowsPerPlayer;
            MaxSubsPerPlayer = cfg.PickupOptions.maxSubsPerPlayer;
            ExcessiveNoShowBanTime = cfg.PickupOptions.excessiveNoShowBanTime;
            ExcessiveNoShowBanTimeScale = cfg.PickupOptions.excessiveNoShowBanTimeScale;
            ExcessiveNoShowBanTimeScaleIndex =
                cfg.PickupOptions.excessiveNoShowBanTimeScaleIndex;
            ExcessiveSubUseBanTime = cfg.PickupOptions.excessiveSubUseBanTime;
            ExcessiveSubUseBanTimeScale =
                cfg.PickupOptions.excessiveSubUseBanTimeScale;
            ExcessiveSubUseBanTimeScaleIndex =
                cfg.PickupOptions.excessiveSubUseBanTimeScaleIndex;
        }
    }
}
