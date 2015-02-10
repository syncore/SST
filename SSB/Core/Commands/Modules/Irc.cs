using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
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
        // MaxNickLength: 15 for QuakeNet; Freenode is 16; some others might be up to 32
        private const int MaxNickLength = 15;
        private readonly ConfigHandler _configHandler;
        private readonly SynServerBot _ssb;
        // Regex for testing validity of IRC nick according to IRC RFC specification;
        // currently set from 2-15 max length
        private readonly Regex _validIrcNick;
        private int _minModuleArgs = 3;
        

        /// <summary>
        ///     Initializes a new instance of the <see cref="Irc" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Irc(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            _validIrcNick = new Regex(@"^([a-zA-Z\[\]\\`_\^\{\|\}][a-zA-Z0-9\[\]\\`_\^\{\|\}-]{1,15})");
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
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <connect|disconnect> ^7 - this uses server info from config file!",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.IrcArg));
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
                        _ssb.QlCommands.QlCmdSay(
                            string.Format(
                                "^1[ERROR]^3 IRC module is already enabled. To disable: {0}{1} {2} off ",
                                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.IrcArg));
                    return;
                }
                // Validity check
                if (RequiredIrcSettingsAreValid())
                {
                    await EnableIrc(c);
                }
                else
                {
                    await
                        _ssb.QlCommands.QlCmdSay(
                            "^1[ERROR]^3 Invalid IRC setting(s) found in config. Cannot load.");
                }
            }
            if (c.Args[2].Equals("disconnect"))
            {
                if (!Active)
                {
                    await
                       _ssb.QlCommands.QlCmdSay(
                           string.Format(
                               "^1[ERROR]^3 IRC module is not active. To enable: {0}{1} {2} connect",
                               CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.IrcArg));
                    //return;
                }
                // TODO: Do disconnection here
            }
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            // Validate
            if (!RequiredIrcSettingsAreValid())
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
            // TODO: Perform an IRC disconnection here
            UpdateConfig(false);
            await
                _ssb.QlCommands.QlCmdTell("^2[SUCCESS]^7 Internet Relay Chat module disabled. Existing connection has been terminated.",
                c.FromUser);
        }

        /// <summary>
        /// Enables the IRC module.
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

            // TODO: Do connection here
        }

        /// <summary>
        ///     Automatically starts the module if an active flag is detected in the configuration.
        /// </summary>
        /// <remarks>This is used after <see cref="LoadConfig" /> has been called, to connect to the IRC
        /// server on load, if applicable.</remarks>
        private void Init()
        {
            // TODO: Perform an IRC connection here
            Debug.WriteLine("Active flag detected in saved configuration; auto-initializing IRC module.");
        }

        /// <summary>
        /// Determines whether the specified channel is valid.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns><c>true</c> if the specified channel is valid, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Note: this does not take into account the length of the channel name, as such requirements
        /// vary from ircd to ircd.
        /// </remarks>
        private bool IsValidIrcChannelName(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return false;
            if (!channel.StartsWith("#")) return false;
            if (channel.Contains(" ")) return false;
            return true;
        }

        /// <summary>
        /// Determines whether the specified nickname is a valid IRC nickname.
        /// </summary>
        /// <param name="nick">The nickname to check.</param>
        /// <returns><c>true</c> if the specified nickname is valid, otherwise <c>false</c>.</returns>
        private bool IsValidIrcNickname(string nick)
        {
            if (!_validIrcNick.IsMatch(nick)) return false;
            if (string.IsNullOrEmpty(nick)) return false;
            if (nick.Length > MaxNickLength) return false;
            if (nick.Contains(" ")) return false;
            return true;
        }

        /// <summary>
        /// Checks the validity of the settings in the IRC portion of the configuration file that
        /// are required to enable basic irc functionality.
        /// </summary>
        /// <returns><c>true</c> if the required settings are valid, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Optional settings (i.e. Nickserv auto-auth, admin nickname) are not checked here.
        /// As with any other module, invalid settings will cause a default configuration to be loaded
        /// when the configuration is read.
        /// </remarks>
        private bool RequiredIrcSettingsAreValid()
        {
            _configHandler.ReadConfiguration();
            var cfg = _configHandler.Config.IrcOptions;
            // Nickname validity
            if (!IsValidIrcNickname(cfg.ircNickName)) return false;
            // Channel name validity
            if (!IsValidIrcChannelName(cfg.ircChannel)) return false;
            // Username (ident) validity; re-use nickname check
            if (!IsValidIrcNickname(cfg.ircUserName)) return false;
            // IRC host validity
            if (string.IsNullOrEmpty(cfg.ircServerAddress)) return false;
            // IRC port validity
            if (cfg.ircServerPort > 65535) return false;
            return true;
        }
    }
}