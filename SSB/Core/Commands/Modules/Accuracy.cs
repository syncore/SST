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
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _minModuleArgs = 3;
        private readonly SynServerBot _ssb;

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
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (c.Args[2].Equals("off"))
            {
                await DisableAcc(c);
                return true;
            }
            if (c.Args.Length != 3)
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (c.Args[2].Equals("on"))
            {
                // Active check: prevent another timer class from being instantiated
                if (Active)
                {
                    StatusMessage = string.Format(
                        "^1[ERROR]^3 Accuracy scanner is already active. To disable: ^1{0}{1} {2} off",
                        CommandList.GameCommandPrefix, c.CmdName, ModuleCmd.AccuracyArg);
                    return false;
                }
                await EnableAcc(c);
            }
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
                "^1[ERROR]^3 Usage: {0}{1} {2} <on/off>",
                CommandList.GameCommandPrefix, c.CmdName,
                ModuleCmd.AccuracyArg);
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
        /// <param name="c">The command argument information.</param>
        private async Task DisableAcc(CmdArgs c)
        {
            UpdateConfig(false);
            StatusMessage = "^2[SUCCESS]^7 Accuracy scanning has been ^1disabled.";
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Enables the accuracy scanning module.
        /// </summary>
        private async Task EnableAcc(CmdArgs c)
        {
            UpdateConfig(true);
            StatusMessage = "^2[SUCCESS]^7 Accuracy scanning has been ^2enabled.";
            await SendServerSay(c, StatusMessage);
        }
    }
}