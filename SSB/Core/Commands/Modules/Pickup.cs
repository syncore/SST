using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
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
        private const int TeamMaxSize = 8;
        private const int TeamMinSize = 3;
        private readonly ConfigHandler _configHandler;
        private readonly PickupManager _pickupManager;
        private readonly SynServerBot _ssb;
        private int _minModuleArgs = 3;

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
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

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
        public uint MaxNoShowsPerPlayer { get; set; }

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
        public uint MaxSubsPerPlayer { get; set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinModuleArgs
        {
            get { return _minModuleArgs; }
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
        ///     Gets or sets the teamsize.
        /// </summary>
        /// <value>
        ///     The teamsize.
        /// </value>
        public uint Teamsize { get; set; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <teamsize> [noshowbans] [subbans] - teamsize is a number",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.PickupArg));
        }

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[2].Equals("off"))
            {
                await DisablePickup();
                return;
            }
            if (c.Args[2].Equals("noshowbans") || c.Args[2].Equals("subbans"))
            {
                await EvalSetBanSettings(c);
                return;
            }
            uint teamsize;
            if (!uint.TryParse(c.Args[2], out teamsize))
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 Minimum team size is {0}, maximum team size is {1}.",
                    TeamMinSize, TeamMaxSize));
                return;
            }
            if (teamsize < TeamMinSize || teamsize > TeamMaxSize)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 Minimum team size is {0}, maximum team size is {1}.",
                    TeamMinSize, TeamMaxSize));
                return;
            }
            if (!_ssb.ServerInfo.IsATeamGame())
            {
                //TODO: create a fix for this
                // Might have not gotten it the first time, so request again
                await _ssb.QlCommands.SendToQlAsync("serverinfo", true);
                _ssb.QlCommands.ClearBothQlConsoles();
                // If we still don't have it now, then notify
                if (!_ssb.ServerInfo.IsATeamGame())
                {
                    await
                        _ssb.QlCommands.QlCmdSay(
                            "^1[ERROR]^3 Pickup module can only be enabled for team-based games.");
                    return;
                }
                    await EnablePickup(teamsize);
                
            }
            await EnablePickup(teamsize);
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
                (!Tools.ValidTimeScales.Contains(
                    _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale))
                &&
                ((!Tools.ValidTimeScales.Contains(
                    _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale))))
            {
                Active = false;
                _configHandler.Config.PickupOptions.SetDefaults();
                return;
            }
            // Current game must be team game, despite Active setting
            if (!_ssb.ServerInfo.IsATeamGame())
            {
                Active = false;
                return;
            }

            Active = _configHandler.Config.PickupOptions.isActive;
            MaxNoShowsPerPlayer = _configHandler.Config.PickupOptions.maxNoShowsPerPlayer;
            MaxSubsPerPlayer = _configHandler.Config.PickupOptions.maxSubsPerPlayer;
            ExcessiveNoShowBanTime = _configHandler.Config.PickupOptions.excessiveNoShowBanTime;
            ExcessiveNoShowBanTimeScale = _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale;
            ExcessiveSubUseBanTime = _configHandler.Config.PickupOptions.excessiveSubUseBanTime;
            ExcessiveSubUseBanTimeScale = _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale;
            Teamsize = _configHandler.Config.PickupOptions.teamSize;
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
            Active = active;
            if (active)
            {
                _configHandler.Config.PickupOptions.isActive = true;
                _configHandler.Config.PickupOptions.teamSize = Teamsize;
                _configHandler.Config.PickupOptions.maxNoShowsPerPlayer = MaxNoShowsPerPlayer;
                _configHandler.Config.PickupOptions.maxSubsPerPlayer = MaxSubsPerPlayer;
                _configHandler.Config.PickupOptions.excessiveNoShowBanTime = ExcessiveNoShowBanTime;
                _configHandler.Config.PickupOptions.excessiveNoShowBanTimeScale = ExcessiveNoShowBanTimeScale;
                _configHandler.Config.PickupOptions.excessiveSubUseBanTime = ExcessiveSubUseBanTime;
                _configHandler.Config.PickupOptions.excessiveSubUseBanTimeScale = ExcessiveSubUseBanTimeScale;
            }
            else
            {
                _configHandler.Config.PickupOptions.SetDefaults();
            }
            _configHandler.WriteConfiguration();
        }

        /// <summary>
        ///     Disables the pickup module.
        /// </summary>
        private async Task DisablePickup()
        {
            UpdateConfig(false);
            // Unlock the teams and clear eligible players if any
            _ssb.QlCommands.SendToQl("unlock", false);
            Manager.ResetPickupStatus();
            await
                _ssb.QlCommands.QlCmdSay("^2[SUCCESS]^7 Pickup game module has been disabled.");
        }

        /// <summary>
        ///     Enables the pickup module.
        /// </summary>
        /// <param name="teamsize">The teamsize.</param>
        private async Task EnablePickup(uint teamsize)
        {
            Teamsize = teamsize;
            // Note: notice the missing ban settings here.
            // The configuration has some pretty sane defaults, so unless the admin specifically
            // overrides the defaults with noshows/subbans args, then we will just use those.
            UpdateConfig(true);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Pickup game module has been enabled with" +
                                  " initial teamsize of ^2{0}^7 - To start: ^2{1}{2} start",
                        teamsize, CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }

        /// <summary>
        ///     Evaluates whether the noshow or sub ban settings can be set based on the user's input.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalSetBanSettings(CmdArgs c)
        {
            string settingsType = string.Empty;
            if (c.Args[2].Equals("noshowbans"))
            {
                settingsType = "noshows";
            }
            else if (c.Args[2].Equals("subbans"))
            {
                settingsType = "subs";
            }
            if (c.Args.Length != 6)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} {2} {3} <max> <bantime> <banscale> - max: max # {4}, bantime: #, banscale: secs," +
                    " mins, hours, days, months, OR years",
                    CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.PickupArg, c.Args[2], settingsType));
                return;
            }
            uint maxNum;
            if (!uint.TryParse(c.Args[3], out maxNum))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 Max # of {0} to allow must be a number greater than zero.",
                            settingsType));
                return;
            }
            double timeNum;
            if (!double.TryParse(c.Args[4], out timeNum))
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 The time to ban must be a number greater than zero.");
                return;
            }
            if (timeNum <= 0)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 The time to ban must be a number greater than zero.");
                return;
            }
            bool isValidScale = Tools.ValidTimeScales.Contains(c.Args[5]);
            if (!isValidScale)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Scale must be: secs, mins, hours, days, months OR years.");
                return;
            }
            await SetBanSettings(settingsType, maxNum, timeNum, c.Args[5]);
        }

        /// <summary>
        ///     Sets the no-show or excessive subs ban settings.
        /// </summary>
        /// <param name="bType">Type of ban to set (noshows or subs).</param>
        /// <param name="maxNum">The maximum number of noshows or subs used.</param>
        /// <param name="timeToBan">The time to ban.</param>
        /// <param name="scaleToBan">The scale to ban.</param>
        private async Task SetBanSettings(string bType, uint maxNum, double timeToBan, string scaleToBan)
        {
            if (bType.Equals("noshows"))
            {
                MaxNoShowsPerPlayer = maxNum;
                ExcessiveNoShowBanTime = timeToBan;
                ExcessiveNoShowBanTimeScale = scaleToBan;
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^2[SUCCESS]^7 Players leaving without a sub more than ^3{0}^7 times will be banned for^1 {1} {2}.",
                            maxNum, timeToBan, scaleToBan));
            }
            else if (bType.Equals("subs"))
            {
                MaxSubsPerPlayer = maxNum;
                ExcessiveSubUseBanTime = timeToBan;
                ExcessiveSubUseBanTimeScale = scaleToBan;
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^2[SUCCESS]^7 Players who've requested subs more than ^3{0}^7 times will be banned for^1 {1} {2}.",
                            maxNum, timeToBan, scaleToBan));
            }
            UpdateConfig(true);
        }
    }
}