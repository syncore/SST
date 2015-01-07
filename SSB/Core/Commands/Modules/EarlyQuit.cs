using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Database;
using SSB.Enum;
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
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

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
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] [clear] [forgive] <# of early quits> <time> <scale> ^7 - time is a number, scale is: secs, mins, hours, days, months, or years.",
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
            if (c.Args[2].Equals("clear"))
            {
                await EvalEarlyQuitClear(c);
                return;
            }
            if (c.Args[2].Equals("forgive"))
            {
                await EvalEarlyQuitForgive(c);
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

            Active = _configHandler.Config.EarlyQuitOptions.isActive;
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
        /// Clears the early quits for a given user.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="qdb">The quit database.</param>
        private async Task ClearEarlyQuits(CmdArgs c, Quits qdb)
        {
            qdb.DeleteUserFromDb(c.Args[3]);
            await
                    _ssb.QlCommands.QlCmdSay(string.Format("^5[EARLYQUIT]^7 Cleared all early quit records for: ^3{0}", c.Args[3]));
            Debug.WriteLine(string.Format("Cleared all early quits for player {0} at admin's request.", c.Args[3]));
            // See if there is an early quit-related ban and remove it as well
            var banDb = new Bans();
            if (banDb.UserAlreadyBanned(c.Args[3]))
            {
                var bi = banDb.GetBanInfo(c.Args[3]);
                if (bi.BanType == BanType.AddedByEarlyQuit)
                {
                    banDb.DeleteUserFromDb(c.Args[3]);
                    await _ssb.QlCommands.SendToQlAsync(string.Format("unban {0}", c.Args[1]), false);
                    Debug.WriteLine(string.Format("Also removed early quit-related ban for player {0}.", c.Args[3]));
                }
            }
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
        /// Evaluates the early quit clear command to see if it can be successfully processed.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalEarlyQuitClear(CmdArgs c)
        {
            if (c.Args.Length != 4)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
                 "^1[ERROR]^3 Usage: {0}{1} {2} clear <name> ^7 - name is without the clan tag",
                 CommandProcessor.BotCommandPrefix, c.CmdName, NameModule));
                return;
            }
            var quitDb = new Quits();
            if (!quitDb.UserExistsInDb(c.Args[3]))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^7 {0}^3 has no early quits.", c.Args[3]));
                return;
            }
            await ClearEarlyQuits(c, quitDb);
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
                // Just compare to int type's max size because in the case of month and year
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

        /// <summary>
        /// Evaluates teh early quit forgive command to see if it can be successfully processed.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalEarlyQuitForgive(CmdArgs c)
        {
            if (c.Args.Length != 5)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
                  "^1[ERROR]^3 Usage: {0}{1} {2} forgive <name> <# of quits> ^7 - name is without the clan tag. # quits is amount to forgive",
                  CommandProcessor.BotCommandPrefix, c.CmdName, NameModule));
                return;
            }
            var quitDb = new Quits();
            if (!quitDb.UserExistsInDb(c.Args[3]))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^7 {0}^3 has no early quits.", c.Args[3]));
                return;
            }
            int numQuitsToForgive;
            if (!int.TryParse(c.Args[4], out numQuitsToForgive))
            {
                await
                      _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 # of quits must be a number greater than zero!");
                return;
            }
            if (numQuitsToForgive == 0)
            {
                await
                      _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 # of quits must be greater than zero.");
                return;
            }
            long userTotalQuits = quitDb.GetUserQuitCount(c.Args[3]);
            if (numQuitsToForgive >= userTotalQuits)
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format("^1[ERROR] ^7{0}^3 has {1} total quits. This would remove all. If that's what you want: ^7{2}{3} {4} clear {0}",
                    c.Args[3], userTotalQuits, CommandProcessor.BotCommandPrefix, c.CmdName, NameModule));
                return;
            }
            await ForgiveEarlyQuits(c, quitDb);
        }

        /// <summary>
        /// Forgives a given numer of early quits for a user.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="qdb">The quit database.</param>
        private async Task ForgiveEarlyQuits(CmdArgs c, Quits qdb)
        {
            int num = Convert.ToInt32(c.Args[4]);
            qdb.DecrementUserQuitCount(c.Args[3], num);
            await
                   _ssb.QlCommands.QlCmdSay(string.Format("^5[EARLYQUIT]^7 Forgave ^3{0}^7 of ^3{1}^7's early quits.", num, c.Args[3]));
        }
    }
}