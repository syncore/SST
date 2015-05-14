using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    /// Module: enable or disable the ability to check a player's accuracy when bot is in spectator mode.
    /// </summary>
    public class Accuracy : IModule
    {
        public const string NameModule = "acc";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:ACCURACY]";
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="Accuracy"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public Accuracy(SynServerTool sst)
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
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command evaluation was successful, otherwise <c>false</c>.</returns>
        public async Task<bool> EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < (c.FromIrc ? IrcMinModuleArgs : _qlMinModuleArgs))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (Helpers.GetArgVal(c, 2).Equals("off"))
            {
                await DisableAcc(c);
                return true;
            }
            if (c.Args.Length != (c.FromIrc ? 4 : 3))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (Helpers.GetArgVal(c, 2).Equals("on"))
            {
                // Active check: prevent another timer class from being instantiated
                if (Active)
                {
                    StatusMessage = string.Format(
                        "^1[ERROR]^3 Accuracy display is already active. To disable: ^1{0}{1} {2} off",
                        CommandList.GameCommandPrefix, c.CmdName,
                        ((c.FromIrc)
                            ? (string.Format("{0} {1}", c.Args[1],
                                NameModule))
                            : NameModule));
                    return false;
                }
                await EnableAcc(c);
            }
            return true;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} <on/off>",
                CommandList.GameCommandPrefix, c.CmdName,
                ((c.FromIrc)
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
            Active = cfg.AccuracyOptions.isActive;
            Log.Write(string.Format("Active: {0}", (Active ? "YES" : "NO")),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
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
            Active = active;

            var cfg = _configHandler.ReadConfiguration();
            cfg.AccuracyOptions.isActive = active;
            _configHandler.WriteConfiguration(cfg);

            // Reflect changes in UI
            _sst.UserInterface.PopulateModAccuracyUi();
        }

        /// <summary>
        /// Disables the accuracy scanning module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisableAcc(CmdArgs c)
        {
            UpdateConfig(false);
            StatusMessage = "^2[SUCCESS]^7 Accuracy display has been ^1disabled.";
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Received {0} request from {1} to disable accuracy display module. Disabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Enables the accuracy scanning module.
        /// </summary>
        private async Task EnableAcc(CmdArgs c)
        {
            UpdateConfig(true);
            StatusMessage = "^2[SUCCESS]^7 Accuracy display has been ^2enabled.";
            await SendServerSay(c, StatusMessage);
            Log.Write(string.Format("Received {0} request from {1} to enable accuracy display module. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }
    }
}
