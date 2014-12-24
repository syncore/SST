using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Database;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Early quitter detection. Keep track of players who leave the game before it is finished.
    /// </summary>
    public class EarlyQuit : IModule
    {
        public const string NameModule = "earlyquit";
        private readonly ConfigHandler _configHandler;
        private readonly SynServerBot _ssb;

        private readonly string[] _validTimeScales =
        {
            "sec", "secs", "min", "mins", "hour", "hours", "day", "days",
            "month", "months", "year", "years"
        };

        private int _minmoduleArgs = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EarlyQuit" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public EarlyQuit(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            LoadConfig();
        }

        /// <summary>
        ///     Gets or sets a numeric value representing the time to ban early quitters.
        /// </summary>
        /// <value>
        ///     A numeric value representing the time to ban early quitters.
        /// </value>
        /// <remarks>
        ///     <see cref="BanTimeScale" /> for the scale that is to be combined with this setting.
        /// </remarks>
        public double BanTime { get; set; }

        /// <summary>
        ///     Gets or sets the scale that is combined with <see cref="BanTime" /> that specifies the duration of the ban.
        /// </summary>
        /// <value>
        ///     The scale that is combined with <see cref="BanTime" /> that specifies the duration of the ban.
        /// </value>
        public string BanTimeScale { get; set; }

        /// <summary>
        ///     Gets or sets the maximum quits allowed before a user is banned.
        /// </summary>
        /// <value>
        ///     The maximum quits allowed before a user is banned.
        /// </value>
        public uint MaxQuitsAllowed { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinModuleArgs
        {
            get { return _minmoduleArgs; }
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
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <# of early quits> <time> <scale> ^7 - time is a number, scale is: secs, mins, hours, days, months, or years.",
                CommandProcessor.BotCommandPrefix, c.CmdName, NameModule));
        }

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minmoduleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[2].Equals("off"))
            {
                await DisableEarlyQuit();
                return;
            }
            // In the case of enable, evaluate parameters to see if we can enable the module
            await EvalEarlyQuitEnable(c);
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            // Initialize database
            var eq = new Quits();
            eq.InitDb();

            _configHandler.ReadConfiguration();
            // See if we're dealing with the default values
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_configHandler.Config.EarlyQuitOptions.banTime == 0 ||
                _configHandler.Config.EarlyQuitOptions.banTimeScale == string.Empty)
            {
                Active = false;
                return;
            }
            // See if it's a valid scale
            if (!_validTimeScales.Contains(_configHandler.Config.EarlyQuitOptions.banTimeScale))
            {
                Active = false;
                _configHandler.Config.EarlyQuitOptions.SetDefaults();
                return;
            }

            BanTime = _configHandler.Config.EarlyQuitOptions.banTime;
            BanTimeScale = _configHandler.Config.EarlyQuitOptions.banTimeScale;
            MaxQuitsAllowed = _configHandler.Config.EarlyQuitOptions.maxQuitsAllowed;
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
                _configHandler.Config.EarlyQuitOptions.isActive = true;
                _configHandler.Config.EarlyQuitOptions.banTime = BanTime;
                _configHandler.Config.EarlyQuitOptions.banTimeScale = BanTimeScale;
                _configHandler.Config.EarlyQuitOptions.maxQuitsAllowed = MaxQuitsAllowed;
            }
            else
            {
                _configHandler.Config.EarlyQuitOptions.SetDefaults();
            }

            _configHandler.WriteConfiguration();
        }

        /// <summary>
        ///     Disables the early quitter module.
        /// </summary>
        private async Task DisableEarlyQuit()
        {
            UpdateConfig(false);
            await
                _ssb.QlCommands.QlCmdSay(
                    "^2[SUCCESS]^7 Early quit tracker ^1disabled^7. Players may quit early without incurring a ban penalty.");
        }

        /// <summary>
        ///     Enables the early quitter module.
        /// </summary>
        /// <param name="maxQuits">The maximum number of quits allowed.</param>
        /// <param name="time">The time to ban for if max quits is exceeded.</param>
        /// <param name="scale">The time scale to be used with the time.</param>
        /// <returns></returns>
        private async Task EnableEarlyQuit(uint maxQuits, double time, string scale)
        {
            MaxQuitsAllowed = maxQuits;
            BanTime = time;
            BanTimeScale = scale;
            UpdateConfig(true);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[EARLYQUIT]^7 Early quit tracker is now ^2ON^7. Players who spectate or quit more than ^2{0}^7 times before the game is over will be banned for ^1{1}^7 {2}.",
                        maxQuits, time, scale));
        }

        /// <summary>
        ///     Evaluates the parameters passed to the early quit module to see if it can be enabled and enables if parameters are
        ///     valid.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        private async Task EvalEarlyQuitEnable(CmdArgs c)
        {
            // !mod earlyquit numquits time scale
            // [0]  [1]       [2]      [3]  [4]
            if (c.Args.Length != 5)
            {
                await DisplayArgLengthError(c);
                return;
            }
            uint numQuits;
            if (!uint.TryParse(c.Args[2], out numQuits))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 The maximum number of quits must be a positive number!");
                return;
            }
            if (numQuits > int.MaxValue)
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 The maximum number of quits is too large!");
                return;
            }
            double time;
            if (!double.TryParse(c.Args[3], out time))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Time must be a positive number!");
                return;
            }
            if (time <= 0)
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Time must be a positive number!");
                return;
            }
            if (time > int.MaxValue)
            {
                // Just compare to int type's max size as in case of month and year
                // it has to be converted to int and can't be double anyway
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Time is too large!");
                return;
            }
            bool validScale = false;
            foreach (string scale in _validTimeScales)
            {
                if (c.Args[4].Equals(scale))
                {
                    validScale = true;
                }
            }
            if (!validScale)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Scale must be: secs, mins, hours, days, months, OR years");
                return;
            }

            await EnableEarlyQuit(numQuits, time, c.Args[4]);
        }
    }
}