using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Modules.Irc;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

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
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Irc" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public Irc(SynServerBot ssb)
        {
            IsIrcAccessAllowed = false;
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
        /// Gets the IRC manager.
        /// </summary>
        /// <value>
        /// The IRC manager.
        /// </value>
        public IrcManager IrcManager { get { return _irc; } }

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
        /// Gets a value indicating whether the bot is connected to IRC.
        /// </summary>
        /// <value>
        /// <c>true</c> if the bot is connected to irc; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnectedToIrc { get { return _irc.IsConnectedToIrc; } }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed { get; private set; }

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
            // IRC access to the irc module command isn't allowed
            if (c.FromIrc)
            {
                StatusMessage = "^1[ERROR]^3 This command can only be accessed from in-game.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if ((c.Args.Length < _qlMinModuleArgs) || (c.Args.Length != 3))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (!Helpers.GetArgVal(c, 2).Equals("off") &&
                !Helpers.GetArgVal(c, 2).Equals("connect") && !Helpers.GetArgVal(c, 2).Equals("disconnect")
                && !Helpers.GetArgVal(c, 2).Equals("reconnect"))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (Helpers.GetArgVal(c, 2).Equals("off"))
            {
                await ProcessOffArg(c);
                return true;
            }
            if (Helpers.GetArgVal(c, 2).Equals("connect"))
            {
                return await ProcessConnectArg(c);
            }
            if (Helpers.GetArgVal(c, 2).Equals("disconnect"))
            {
                return await ProcessDisconnectArg(c);
            }
            if (Helpers.GetArgVal(c, 2).Equals("reconnect"))
            {
                return await ProcessReconnectArg(c);
            }
            return false;
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
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <connect|disconnect|reconnect> ^7 - this uses" +
                " server info from config file!",
                CommandList.GameCommandPrefix, c.CmdName,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
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
        ///     Updates the configuration.
        /// </summary>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            _configHandler.Config.IrcOptions.isActive = active;
            _configHandler.WriteConfiguration();

            // Reflect changes in UI
            _ssb.UserInterface.PopulateModIrcUi();
        }

        /// <summary>
        ///     Disables the IRC module.
        /// </summary>
        private async Task DisableIrc(CmdArgs c)
        {
            _irc.Disconnect();
            UpdateConfig(false);
            StatusMessage =
                "^2[SUCCESS]^7 Internet Relay Chat module disabled. Existing connection has been terminated.";
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Enables the IRC module and makes the initial connection to the IRC server.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task EnableIrc(CmdArgs c)
        {
            _configHandler.ReadConfiguration();

            // Set the module to active
            UpdateConfig(true);

            StatusMessage = "^2[SUCCESS]^7 Internet Relay Chat module enabled.";
            await SendServerSay(c, StatusMessage);

            StatusMessage = string.Format(
                "^6[IRC]^7 Attempting to connect to IRC server: ^2{0}:{1}^7 using name: ^2{2},^7 channel:^2 {3}",
                _configHandler.Config.IrcOptions.ircServerAddress,
                _configHandler.Config.IrcOptions.ircServerPort,
                _configHandler.Config.IrcOptions.ircNickName, _configHandler.Config.IrcOptions.ircChannel);
            // This was successful, but send as a /tell msg (error) to hide IRC server info.
            await SendServerTell(c, StatusMessage);

            // Attempt to make the connection
            _irc.StartIrcThread();
        }

        /// <summary>
        ///     Automatically starts the module if an active flag is detected in the configuration.
        /// </summary>
        /// <remarks>
        ///     This is used after <see cref="LoadConfig" /> has been called, to connect to the IRC
        ///     server on load, if applicable.
        /// </remarks>
        public void Init()
        {
            if (IsConnectedToIrc)
            {
                Deactivate();
            }
            
            Debug.WriteLine("Active flag detected in saved configuration; initializing IRC module.");
            _irc.StartIrcThread();
        }

        /// <summary>
        /// Deactivates this module.
        /// </summary>
        public void Deactivate()
        {
            _irc.Disconnect();
        }

        /// <summary>
        ///     Processes the "connect" argument.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task<bool> ProcessConnectArg(CmdArgs c)
        {
            // Active check: prevent another IRC client from being instantiated
            if (Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 IRC module is already enabled. To disable: {0}{1} {2} off -" +
                    " To reconnect: {0}{1} {2} reconnect",
                    CommandList.GameCommandPrefix, c.CmdName,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}", c.Args[1],
                            NameModule))
                        : NameModule));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            // Validity check
            if (_irc.RequiredIrcSettingsAreValid())
            {
                await EnableIrc(c);
                return true;
            }

            StatusMessage = "^1[ERROR]^3 Invalid IRC setting(s) found in config. Cannot load.";
            await SendServerTell(c, StatusMessage);
            return false;
        }

        /// <summary>
        ///     Processes the "disconnect" argument.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the disconnect attempt was successful; otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> ProcessDisconnectArg(CmdArgs c)
        {
            if (!Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 IRC module is not active. To enable: {0}{1} {2} connect",
                    CommandList.GameCommandPrefix, c.CmdName,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}", c.Args[1],
                            NameModule))
                        : NameModule));
                await SendServerTell(c, StatusMessage);
                return false;
            }

            StatusMessage = "^6[IRC]^7 Attempting to disconnect from IRC.";
            await SendServerTell(c, StatusMessage);
            _irc.Disconnect();
            return true;
        }

        /// <summary>
        ///     Processes the "off" argument.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task ProcessOffArg(CmdArgs c)
        {
            await DisableIrc(c);
        }

        /// <summary>
        ///     Processes the "reconnect" argument.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the reconnect attempt was successful; otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> ProcessReconnectArg(CmdArgs c)
        {
            if (!Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 IRC module is not active. To enable: {0}{1} {2} connect",
                    CommandList.GameCommandPrefix, c.CmdName,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}", c.Args[1],
                            NameModule))
                        : NameModule));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            // Validity check
            if (_irc.RequiredIrcSettingsAreValid())
            {
                _configHandler.ReadConfiguration();

                StatusMessage = string.Format(
                    "^6[IRC]^7 Attempting to reconnect to IRC server: ^2{0}:{1}^7 using name: ^2{2},^7 channel:^2 {3}",
                    _configHandler.Config.IrcOptions.ircServerAddress,
                    _configHandler.Config.IrcOptions.ircServerPort,
                    _configHandler.Config.IrcOptions.ircNickName, _configHandler.Config.IrcOptions.ircChannel);

                // This was successful, but send as a /tell msg (error) to hide IRC server info.
                await SendServerTell(c, StatusMessage);
                _irc.AttemptReconnection(true);
                return true;
            }

            StatusMessage = "^1[ERROR]^3 Invalid IRC setting(s) found in config. Will not connect.";
            await SendServerTell(c, StatusMessage);
            return false;
        }
    }
}