using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: enable or disable the ability to check a player's accuracy when bot is in spectator mode.
    /// </summary>
    public class Accuracy : IModule
    {
        public const string NameModule = "acc";
        private readonly ConfigHandler _configHandler;
        private readonly SynServerBot _ssb;
        private int _minModuleArgs = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Accuracy" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Accuracy(SynServerBot ssb)
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
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} <on/off>",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AccuracyArg));
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
                await DisableAcc();
                return;
            }
            if (c.Args.Length != 3)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[2].Equals("on"))
            {
                // Active check: prevent another timer class from being instantiated
                if (Active)
                {
                    await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format(
                                "^1[ERROR]^3 Accuracy scanner is already active. To disable: {0}{1} {2} off ",
                                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AccuracyArg));
                    return;
                }
                await EnableAcc();
            }
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            Active = _configHandler.Config.AccuracyOptions.isActive;
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
                _configHandler.Config.AccuracyOptions.isActive = true;
            }
            else
            {
                _configHandler.Config.AccuracyOptions.SetDefaults();
            }
            _configHandler.WriteConfiguration();
        }

        /// <summary>
        ///     Disables the accuracy scanning module.
        /// </summary>
        /// <returns></returns>
        private async Task DisableAcc()
        {
            UpdateConfig(false);
            await
                _ssb.QlCommands.QlCmdSay(string.Format("^2[SUCCESS]^7 Accuracy scanning has been disabled."));
        }

        /// <summary>
        ///     Enables the accuracy scanning module.
        /// </summary>
        private async Task EnableAcc()
        {
            UpdateConfig(true);
            await
                _ssb.QlCommands.QlCmdSay("^2[SUCCESS]^7 Accuracy scanning has been enabled.");
        }
    }
}