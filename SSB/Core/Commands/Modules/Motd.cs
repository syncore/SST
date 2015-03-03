using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
using SSB.Core.Modules;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Message of the day. Repeat a given message every X minutes.
    /// </summary>
    public class Motd : IModule
    {
        public const string NameModule = "motd";
        private const uint MinRepeatThresholdStart = 0;
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _minModuleArgs = 3;
        private readonly MotdHandler _motd;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Motd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Motd(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            _motd = new MotdHandler(_ssb);
            LoadConfig();
        }

        /// <summary>
        ///     Gets or sets the MOTD message to repeat.
        /// </summary>
        /// <value>
        ///     The MOTD message to repeat.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        ///     Gets or sets the message repeat time.
        /// </summary>
        /// <value>
        ///     The message repeat time.
        /// </value>
        public uint RepeatInterval { get; set; }

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
                await DisableMotd(c);
                return true;
            }

            uint minsNum;
            if (!uint.TryParse(c.Args[2], out minsNum))
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
                        CommandList.GameCommandPrefix, c.CmdName, ModuleCmd.MotdArg);
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (c.Args.Length < 4)
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
                CommandList.GameCommandPrefix, c.CmdName, ModuleCmd.MotdArg);
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
            if (Active)
            {
                Init();
            }
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
        public void UpdateConfig(bool active)
        {
            Active = active;
            if (active)
            {
                _configHandler.Config.MotdOptions.isActive = true;
                _configHandler.Config.MotdOptions.message = Message;
                _configHandler.Config.MotdOptions.repeatInterval = RepeatInterval;
            }
            else
            {
                _configHandler.Config.MotdOptions.SetDefaults();
            }
            _configHandler.WriteConfiguration();
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
        }

        /// <summary>
        ///     Automatically starts the module if an active flag is detected in the configuration.
        /// </summary>
        /// <remarks>This is used after <see cref="LoadConfig" /> has been called, to set the motd on load.</remarks>
        private void Init()
        {
            if ((string.IsNullOrEmpty(Message)) || (RepeatInterval == 0)) return;
            _motd.Message = Message;
            _motd.RepeatInterval = RepeatInterval;
            _motd.StartMotdTimer();
            Debug.WriteLine("Active flag detected in saved configuration; auto-initializing motd module.");
        }

        /// <summary>
        ///     Sets the message of the day.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="interval">The interval.</param>
        /// <remarks>
        ///     This is used when an admin issues the command in-game.
        /// </remarks>
        private async Task SetMotd(CmdArgs c, uint interval)
        {
            //!mod motd 60 message
            //TODO: test this
            Message =
                c.Text.Substring(CommandList.GameCommandPrefix.Length + c.CmdName.Length + c.Args[2].Length +
                                 1);
            RepeatInterval = interval;
            _configHandler.Config.MotdOptions.repeatInterval = RepeatInterval;

            _motd.Message = Message;
            _motd.RepeatInterval = RepeatInterval;
            _motd.StartMotdTimer();

            UpdateConfig(true);

            StatusMessage = string.Format(
                "^2[SUCCESS]^7 Message of the day in config has been set and will repeat every^2 {0}^7 minutes.",
                interval);
            await SendServerSay(c, StatusMessage);
        }
    }
}