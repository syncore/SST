using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Database;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    /// Module: Early quitter detection. Keep track of players who leave the game before it is finished.
    /// </summary>
    public class EarlyQuit : IModule
    {
        public const string NameModule = "earlyquit";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:EARLYQUIT]";
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="EarlyQuit"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public EarlyQuit(SynServerTool sst)
        {
            _sst = sst;
            _configHandler = new ConfigHandler();
            LoadConfig();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IModule"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a numeric value representing the time to ban early quitters.
        /// </summary>
        /// <value>A numeric value representing the time to ban early quitters.</value>
        /// <remarks>
        /// <see cref="BanTimeScale"/> for the scale that is to be combined with this setting.
        /// </remarks>
        public double BanTime { get; set; }

        /// <summary>
        /// Gets or sets the scale that is combined with <see cref="BanTime"/> that specifies the
        /// duration of the ban.
        /// </summary>
        /// <value>
        /// The scale that is combined with <see cref="BanTime"/> that specifies the duration of the ban.
        /// </value>
        public string BanTimeScale { get; set; }

        /// <summary>
        /// Gets or sets the array index of the ban time scale.
        /// </summary>
        /// <value>The array index of the ban time scale.</value>
        public int BanTimeScaleIndex { get; set; }

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
        /// Gets or sets the maximum quits allowed before a user is banned.
        /// </summary>
        /// <value>The maximum quits allowed before a user is banned.</value>
        public uint MaxQuitsAllowed { get; set; }

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
                await DisableEarlyQuit(c);
                return true;
            }
            if (Helpers.GetArgVal(c, 2).Equals("clear"))
            {
                return await EvalEarlyQuitClear(c);
            }
            if (Helpers.GetArgVal(c, 2).Equals("forgive"))
            {
                return await EvalEarlyQuitForgive(c);
            }
            // In the case of enable, evaluate parameters to see if we can enable the module
            return await EvalEarlyQuitEnable(c);
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
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] [clear] [forgive] <# of early quits> <time> <scale> ^7 - time is a " +
                "number, scale is: secs, mins, hours, days, months, or years.",
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
            // Initialize database
            var eq = new DbQuits();
            eq.InitDb();

            var cfg = _configHandler.ReadConfiguration();
            // See if we're dealing with the default values ReSharper disable once CompareOfFloatsByEqualityOperator
            if (cfg.EarlyQuitOptions.banTime == 0 ||
                cfg.EarlyQuitOptions.banTimeScale == string.Empty)
            {
                Active = false;
                return;
            }
            // See if it's a valid scale
            if (!Helpers.ValidTimeScales.Contains(cfg.EarlyQuitOptions.banTimeScale))
            {
                Log.WriteCritical(
                    "Invalid time scale detected. Won't enable. Setting early quit banner defaults.",
                    _logClassType, _logPrefix);

                Active = false;
                cfg.EarlyQuitOptions.SetDefaults();
                _configHandler.WriteConfiguration(cfg);
                return;
            }

            Active = cfg.EarlyQuitOptions.isActive;
            BanTime = cfg.EarlyQuitOptions.banTime;
            BanTimeScale = cfg.EarlyQuitOptions.banTimeScale;
            MaxQuitsAllowed = cfg.EarlyQuitOptions.maxQuitsAllowed;

            Log.Write(string.Format(
                "Active: {0}, ban time: {1} {2}, max early quits allowed: {3}",
                (Active ? "YES" : "NO"), BanTime, BanTimeScale, MaxQuitsAllowed), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
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
            cfg.EarlyQuitOptions.isActive = active;
            cfg.EarlyQuitOptions.banTime = BanTime;
            cfg.EarlyQuitOptions.banTimeScale = BanTimeScale;
            cfg.EarlyQuitOptions.banTimeScaleIndex = BanTimeScaleIndex;
            cfg.EarlyQuitOptions.maxQuitsAllowed = MaxQuitsAllowed;

            _configHandler.WriteConfiguration(cfg);

            // Reflect changes in UI
            _sst.UserInterface.PopulateModEarlyQuitUi();
        }

        /// <summary>
        /// Clears the early quits for a given user.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="qdb">The quit database.</param>
        private async Task ClearEarlyQuits(Cmd c, DbQuits qdb)
        {
            qdb.DeleteUserFromDb(Helpers.GetArgVal(c, 3));

            StatusMessage = string.Format("^5[EARLYQUIT]^7 Cleared all early quit records for: ^3{0}",
                Helpers.GetArgVal(c, 3));
            await SendServerSay(c, StatusMessage);

            // See if there is an early quit-related ban and remove it as well
            await qdb.RemoveQuitRelatedBan(_sst, Helpers.GetArgVal(c, 3));

            // UI: reflect changes
            _sst.UserInterface.RefreshCurrentQuittersDataSource();
            _sst.UserInterface.RefreshCurrentBansDataSource();
        }

        /// <summary>
        /// Disables the early quitter module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns></returns>
        private async Task DisableEarlyQuit(Cmd c)
        {
            UpdateConfig(false);
            StatusMessage = "^2[SUCCESS]^7 Early quit tracker ^1disabled^7. Players may quit" +
                            " early without incurring a ban penalty.";
            await SendServerSay(c, StatusMessage);

            Log.Write(
                string.Format("Received {0} request from {1} to disable early quit banner module. Disabling.",
                    (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Enables the early quitter module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="maxQuits">The maximum number of quits allowed.</param>
        /// <param name="time">The time to ban for if max quits is exceeded.</param>
        /// <param name="scale">The time scale to be used with the time.</param>
        /// <returns></returns>
        private async Task EnableEarlyQuit(Cmd c, uint maxQuits, double time, string scale)
        {
            MaxQuitsAllowed = maxQuits;
            BanTime = time;
            BanTimeScale = scale;
            BanTimeScaleIndex = Helpers.GetTimeScaleIndex(scale);
            UpdateConfig(true);
            StatusMessage = string.Format(
                "^5[EARLYQUIT]^7 Early quit tracker is now ^2ON^7. Players who spectate or quit " +
                "more than^2 {0} ^7times before the game is over will be banned for^1 {1} ^7{2}.",
                maxQuits, time, scale);
            await SendServerSay(c, StatusMessage);

            Log.Write(
                string.Format("Received {0} request from {1} to enable early quit banner module. Enabling.",
                    (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Evaluates the early quit clear command to see if it can be successfully processed.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the evaluation was successful, otherwise <c>false</c>.</returns>
        private async Task<bool> EvalEarlyQuitClear(Cmd c)
        {
            if (c.Args.Length != (c.FromIrc ? 5 : 4))
            {
                StatusMessage =
                    string.Format(
                        "^1[ERROR]^3 Usage: {0}{1} {2} clear <name> ^7 - name is without the clan tag",
                        CommandList.GameCommandPrefix, c.CmdName,
                        ((c.FromIrc)
                            ? (string.Format("{0} {1}", c.Args[1],
                                NameModule))
                            : NameModule));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var quitDb = new DbQuits();
            if (!quitDb.UserExistsInDb(Helpers.GetArgVal(c, 3)))
            {
                StatusMessage = string.Format("^1[ERROR] {0}^3 has no early quits.",
                    Helpers.GetArgVal(c, 3));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            await ClearEarlyQuits(c, quitDb);
            return true;
        }

        /// <summary>
        /// Evaluates the parameters passed to the early quit module to see if it can be enabled and
        /// enables if parameters are valid.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the evaluation was successful, otherwise <c>false</c>.</returns>
        private async Task<bool> EvalEarlyQuitEnable(Cmd c)
        {
            // !mod earlyquit numquits time scale [0] [1] [2] [3] [4]
            if (c.Args.Length != (c.FromIrc ? 6 : 5))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            uint numQuits;
            if (!uint.TryParse(Helpers.GetArgVal(c, 2), out numQuits))
            {
                StatusMessage = "^1[ERROR]^3 The maximum number of quits must be a positive number!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (numQuits > int.MaxValue)
            {
                StatusMessage = "^1[ERROR]^3 The maximum number of quits is too large!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            double time;
            if (!double.TryParse(Helpers.GetArgVal(c, 3), out time))
            {
                StatusMessage = "^1[ERROR]^3 Time must be a positive number!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (time <= 0)
            {
                StatusMessage = "^1[ERROR]^3 Time must be a positive number!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (time > int.MaxValue)
            {
                // Just compare to int type's max size because in the case of month and year it has
                // to be converted to int and can't be double anyway
                StatusMessage = "^1[ERROR]^3 Time is too large!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var validScale = Helpers.ValidTimeScales.Contains(Helpers.GetArgVal(c, 4));
            if (!validScale)
            {
                StatusMessage = "^1[ERROR]^3 Scale must be: secs, mins, hours, days, months, OR years";
                await SendServerTell(c, StatusMessage);
                return false;
            }

            await EnableEarlyQuit(c, numQuits, time, Helpers.GetArgVal(c, 4));
            return true;
        }

        /// <summary>
        /// Evaluates the early quit forgive command to see if it can be successfully processed.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the evaluation was successful, otherwise <c>false</c>.</returns>
        private async Task<bool> EvalEarlyQuitForgive(Cmd c)
        {
            if (c.Args.Length != (c.FromIrc ? 6 : 5))
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} {2} forgive <name> <# of quits> ^7 - name is without the clan" +
                    " tag. # quits is amount to forgive",
                    CommandList.GameCommandPrefix, c.CmdName,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}", c.Args[1],
                            NameModule))
                        : NameModule));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var quitDb = new DbQuits();
            if (!quitDb.UserExistsInDb(Helpers.GetArgVal(c, 3)))
            {
                StatusMessage = string.Format("^1[ERROR]^7 {0}^3 has no early quits.", Helpers.GetArgVal(c, 3));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            int numQuitsToForgive;
            if (!int.TryParse(Helpers.GetArgVal(c, 4), out numQuitsToForgive))
            {
                StatusMessage = "^1[ERROR]^3 # of quits must be a number greater than zero!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (numQuitsToForgive == 0)
            {
                StatusMessage = "^1[ERROR]^3 # of quits must be greater than zero.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var userTotalQuits = quitDb.GetUserQuitCount(Helpers.GetArgVal(c, 3));
            if (numQuitsToForgive >= userTotalQuits)
            {
                StatusMessage =
                    string.Format(
                        "^1[ERROR]^3 {0} has^1 {1}^3 total quits. This would remove all. If that's" +
                        " what you want: ^1{2}{3} {4} clear {0}",
                        Helpers.GetArgVal(c, 3), userTotalQuits,
                        CommandList.GameCommandPrefix, c.CmdName,
                        ((c.FromIrc)
                            ? (string.Format("{0} {1}", c.Args[1],
                                NameModule))
                            : NameModule));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            await ForgiveEarlyQuits(c, numQuitsToForgive, Helpers.GetArgVal(c, 3), quitDb);
            return true;
        }

        /// <summary>
        /// Forgives a given numer of early quits for a user.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="num">The number of quits to forgive.</param>
        /// <param name="player">The player.</param>
        /// <param name="qdb">The quit database.</param>
        private async Task ForgiveEarlyQuits(Cmd c, int num, string player, DbQuits qdb)
        {
            qdb.DecrementUserQuitCount(player, num);

            StatusMessage = string.Format("^5[EARLYQUIT]^7 Forgave^3 {0} ^7of ^3{1}^7's early quits.",
                num, player);
            await SendServerSay(c, StatusMessage);

            // UI: reflect changes
            _sst.UserInterface.RefreshCurrentQuittersDataSource();
        }
    }
}
