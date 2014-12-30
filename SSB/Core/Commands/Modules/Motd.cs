using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
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
        private readonly MotdHandler _motd;
        private readonly SynServerBot _ssb;
        private int _minModuleArgs = 3;

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
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     Used to query activity status for a list of modules. Be sure to set
        ///     a public static bool property IsModuleActive for outside access in other parts of app.
        /// </remarks>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets or sets the MOTD message to repeat.
        /// </summary>
        /// <value>
        ///     The MOTD message to repeat.
        /// </value>
        public string Message { get; set; }

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
        ///     Gets or sets the message repeat time.
        /// </summary>
        /// <value>
        ///     The message repeat time.
        /// </value>
        public uint RepeatInterval { get; set; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <mins> ^7 - message set in config file will repeat every X mins",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.MotdArg));
        }

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[2].Equals("off"))
            {
                await DisableMotd();
                return;
            }
            if (c.Args.Length != 3)
            {
                await DisplayArgLengthError(c);
                return;
            }
            uint minsNum;
            if (!uint.TryParse(c.Args[2], out minsNum))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Minutes must be a positive number.}");
                return;
            }
            if (minsNum <= MinRepeatThresholdStart)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 Minutes must be greater than {0}.",
                        MinRepeatThresholdStart));
                return;
            }
            // Active check: prevent another timer class from being instantiated
            if (Active)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^1[ERROR]^3 MOTD has already been set. To disable: {0}{1} {2} off ",
                            CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.MotdArg));
                return;
            }
            _configHandler.ReadConfiguration();
            if (string.IsNullOrEmpty(_configHandler.Config.MotdOptions.message))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 A message has not been set in the configuration file!");
                return;
            }

            await SetMotd(c);
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
        private async Task DisableMotd()
        {
            _motd.StopMotdTimer();
            UpdateConfig(false);
            await
                _ssb.QlCommands.QlCmdSay(string.Format("^2[SUCCESS]^7 Message of the day has been disabled."));
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
        /// <param name="c">The c.</param>
        /// <remarks>This is used when an admin issues the command in-game.</remarks>
        private async Task SetMotd(CmdArgs c)
        {
            _configHandler.ReadConfiguration();
            if (string.IsNullOrEmpty(_configHandler.Config.MotdOptions.message)) return;

            Message = _configHandler.Config.MotdOptions.message;
            RepeatInterval = Convert.ToUInt32(c.Args[2]);

            _configHandler.Config.MotdOptions.repeatInterval = RepeatInterval;

            _motd.Message = Message;
            _motd.RepeatInterval = RepeatInterval;
            _motd.StartMotdTimer();

            UpdateConfig(true);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^2[SUCCESS]^7 Message of the day in config has been set and will repeat every^2 {0}^7 minutes.",
                        c.Args[2]));
        }
    }
}