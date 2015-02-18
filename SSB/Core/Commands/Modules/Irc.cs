using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
using SSB.Core.Modules.Irc;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Internet Relay Chat. Enables an IRC interface for viewing real-time
    ///     server information and issuing administrative functions from an IRC channel.
    /// </summary>
    public class Irc : IModule
    {
        public const string NameModule = "irc";
        private readonly ConfigHandler _configHandler;
        private readonly IrcManager _irc;
        private readonly int _minModuleArgs = 3;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Irc" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Irc(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            _irc = new IrcManager(_ssb);
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
        /// <returns></returns>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdTell(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <connect|disconnect> ^7 - this uses server info from config file!",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.IrcArg), c.FromUser);
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
            if (!c.Args[2].Equals("off") && !c.Args[2].Equals("connect") && !c.Args[2].Equals("disconnect"))
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args.Length != 3)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[2].Equals("off"))
            {
                await DisableIrc(c);
                return;
            }
            if (c.Args[2].Equals("connect"))
            {
                // Active check: prevent another IRC client from being instantiated
                if (Active)
                {
                    await
                        _ssb.QlCommands.QlCmdTell(
                            string.Format(
                                "^1[ERROR]^3 IRC module is already enabled. To disable: {0}{1} {2} off ",
                                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.IrcArg), c.FromUser);
                    return;
                }
                // Validity check

                if (_irc.RequiredIrcSettingsAreValid())
                {
                    await EnableIrc(c);
                }
                else
                {
                    await
                        _ssb.QlCommands.QlCmdTell(
                            "^1[ERROR]^3 Invalid IRC setting(s) found in config. Cannot load.", c.FromUser);
                }
            }
            if (c.Args[2].Equals("disconnect"))
            {
                if (!Active)
                {
                    await
                        _ssb.QlCommands.QlCmdTell(
                            string.Format(
                                "^1[ERROR]^3 IRC module is not active. To enable: {0}{1} {2} connect",
                                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.IrcArg), c.FromUser);
                    return;
                }
                _irc.Disconnect();
            }
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            // Validate
            if (!_irc.RequiredIrcSettingsAreValid())
            {
                Active = false;
            }
            else
            {
                Active = _configHandler.Config.IrcOptions.isActive;
            }

            if (Active && _configHandler.Config.IrcOptions.autoConnectOnStart)
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
                _configHandler.Config.IrcOptions.isActive = true;
            }
            else
            {
                _configHandler.Config.IrcOptions.SetDefaults();
            }
            _configHandler.WriteConfiguration();
        }

        /// <summary>
        ///     Disables the IRC module.
        /// </summary>
        private async Task DisableIrc(CmdArgs c)
        {
            _irc.Disconnect();
            UpdateConfig(false);
            await
                _ssb.QlCommands.QlCmdTell(
                    "^2[SUCCESS]^7 Internet Relay Chat module disabled. Existing connection has been terminated.",
                    c.FromUser);
        }

        /// <summary>
        ///     Enables the IRC module and makes the initial connection to the IRC server.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EnableIrc(CmdArgs c)
        {
            _configHandler.ReadConfiguration();
            var channel = _configHandler.Config.IrcOptions.ircChannel;
            var nick = _configHandler.Config.IrcOptions.ircNickName;
            var ircServer = _configHandler.Config.IrcOptions.ircServerAddress;
            var ircServerPort = _configHandler.Config.IrcOptions.ircServerPort;

            // Set the module to active
            UpdateConfig(true);

            await _ssb.QlCommands.QlCmdTell("^2[SUCCESS]^7 Internet Relay Chat module enabled.",
                c.FromUser);
            await
                _ssb.QlCommands.QlCmdTell(string.Format(
                    "^6[IRC]^7 Attempting to connect to IRC server: ^2{0}:{1}^7 using name: ^2{2},^7 channel:^2 {3}",
                    ircServer, ircServerPort, nick, channel), c.FromUser);

            // Attempt to make the connection
            _irc.Connect();
        }

        /// <summary>
        ///     Automatically starts the module if an active flag is detected in the configuration.
        /// </summary>
        /// <remarks>
        ///     This is used after <see cref="LoadConfig" /> has been called, to connect to the IRC
        ///     server on load, if applicable.
        /// </remarks>
        private void Init()
        {
            Debug.WriteLine("Active flag detected in saved configuration; auto-initializing IRC module.");
            _irc.Connect();
        }
    }
}