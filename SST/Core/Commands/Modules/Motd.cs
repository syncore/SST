using System;
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
    ///     Module: Message of the day. Repeat a given message every X minutes.
    /// </summary>
    public class Motd : IModule
    {
        public const int MinRepeatThresholdStart = 0;
        public const string NameModule = "motd";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:MOTD]";
        private readonly MotdHandler _motd;
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Motd" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public Motd(SynServerTool sst)
        {
            _sst = sst;
            _configHandler = new ConfigHandler();
            _motd = new MotdHandler(_sst);
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
        ///     Gets or sets the MOTD message to repeat.
        /// </summary>
        /// <value>
        ///     The MOTD message to repeat.
        /// </value>
        public string Message { get; set; }

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
        ///     Gets or sets the message repeat time.
        /// </summary>
        /// <value>
        ///     The message repeat time.
        /// </value>
        public int RepeatInterval { get; set; }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Deactivates the module.
        /// </summary>
        /// <remarks>This is called from the UI context.</remarks>
        public void Deactivate()
        {
            _motd.StopMotdTimer();
        }

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
                await DisableMotd(c);
                return true;
            }

            int minsNum;
            if (!int.TryParse(Helpers.GetArgVal(c, 2), out minsNum))
            {
                StatusMessage = "^1[ERROR]^3 Minutes must be a positive number.}";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (minsNum <= MinRepeatThresholdStart)
            {
                StatusMessage = string.Format("^1[ERROR]^3 Minutes must be greater than {0}.",
                    MinRepeatThresholdStart);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            // Active check: prevent another timer class from being instantiated
            if (Active)
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 MOTD has already been set. To disable: {0}{1} {2} off ",
                        CommandList.GameCommandPrefix, c.CmdName,
                        ((c.FromIrc)
                            ? (string.Format("{0} {1}", c.Args[1],
                                NameModule))
                            : NameModule));
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} request received from {1} to enable MOTD module, but module is already active. Ignoring.",
                    (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);

                return false;
            }
            if (c.Args.Length < (c.FromIrc ? 5 : 4))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            await SetMotd(c, minsNum);
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
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <mins> <msg> - msg will repeat every X mins",
                CommandList.GameCommandPrefix, c.CmdName,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        ///     Automatically starts the module if an active flag is detected in the configuration.
        /// </summary>
        /// <remarks>This is used after <see cref="LoadConfig" /> has been called, to set the motd on load.</remarks>
        /// <remarks>This is called from the UI context.</remarks>
        public void Init()
        {
            // Loaded from UI context
            _configHandler.ReadConfiguration();
            var interval = _configHandler.Config.MotdOptions.repeatInterval;
            var message = _configHandler.Config.MotdOptions.message;
            var active = _configHandler.Config.MotdOptions.isActive;
            if ((!active) || (string.IsNullOrEmpty(message)) || (interval <= MinRepeatThresholdStart)) return;
            _motd.Message = message;
            _motd.RepeatInterval = interval;
            _motd.RestartMotdTimer();
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            if (string.IsNullOrEmpty(_configHandler.Config.MotdOptions.message) ||
                (_configHandler.Config.MotdOptions.repeatInterval <= MinRepeatThresholdStart))
            {
                Active = false;
            }
            else
            {
                Active = _configHandler.Config.MotdOptions.isActive;
            }
            Message = _configHandler.Config.MotdOptions.message;
            RepeatInterval = _configHandler.Config.MotdOptions.repeatInterval;

            Log.Write(string.Format(
                "Active: {0}, message is: \"{1}\". Will repeat every: {2} {3}",
                (Active ? "YES" : "NO"), Message, RepeatInterval,
                (RepeatInterval != 1) ? "minutes" : "minute"), _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Updates the configuration.
        /// </summary>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            _configHandler.Config.MotdOptions.isActive = active;
            _configHandler.Config.MotdOptions.message = Message;
            _configHandler.Config.MotdOptions.repeatInterval = RepeatInterval;

            _configHandler.WriteConfiguration();

            // Reflect changes in UI
            _sst.UserInterface.PopulateModMotdUi();
        }

        /// <summary>
        ///     Disables the motd.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisableMotd(CmdArgs c)
        {
            _motd.StopMotdTimer();
            UpdateConfig(false);
            StatusMessage = string.Format("^2[SUCCESS]^7 Message of the day has been disabled.");
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Received {0} request from {1} to disable MOTD module. Disabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Sets the message of the day.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="interval">The interval.</param>
        /// <remarks>
        ///     This is used when an admin issues the command in-game.
        /// </remarks>
        private async Task SetMotd(CmdArgs c, int interval)
        {
            int msgStart;
            if (c.FromIrc)
            {
                //!ql mod motd 60 this is a test message
                // c.Args[0]: !ql + space + c.Args[1]: mod + space + c.Args[2]: motd + space, c.Args[3]: 60 +
                // space = start of message
                msgStart = ((c.Args[0].Length + 1) + (c.Args[1].Length + 1) + (c.Args[2].Length + 1) +
                            (c.Args[3].Length + 1));
            }
            else
            {
                //!mod motd 60 this is a test message
                // c.Args[0]: !mod + space + c.Args[1]: motd + space + c.Args[3]: 60 +
                // space = start of message
                msgStart = ((c.Args[0].Length + 1) + (c.Args[1].Length + 1) + (c.Args[2].Length + 1));
            }

            Message = c.Text.Substring(msgStart);
            RepeatInterval = interval;
            _configHandler.Config.MotdOptions.repeatInterval = RepeatInterval;

            _motd.Message = Message;
            _motd.RepeatInterval = RepeatInterval;
            _motd.StartMotdTimer();

            UpdateConfig(true);

            StatusMessage = string.Format(
                "^2[SUCCESS]^7 Message of the day set to:^5 {0}^7 and will repeat every^5 {1}^7 minutes.",
                Message, interval);
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Received {0} request from {1} to enable MOTD module. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }
    }
}