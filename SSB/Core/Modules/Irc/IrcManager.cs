using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using IrcDotNet;
using SSB.Config;
using SSB.Config.Modules;
using SSB.Enums;
using SSB.Util;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    /// Class responsible for managing the Internet Relay Chat interface.
    /// </summary>
    public class IrcManager
    {
        // MaxNickLength: 15 for QuakeNet; Freenode is 16; some others might be up to 32
        public const int MaxNickLength = 15;

        public const int MaxReconnectionTries = 5;
        private readonly ConfigHandler _configHandler;
        private readonly IrcEvents _ircEvent;

        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;

        private readonly string _logPrefix = "[IRC]";

        // Regex for testing validity of IRC nick according to IRC RFC specification;
        // currently set from 2-15 max length
        private readonly Regex _validIrcNick;

        private StandardIrcClient _client;
        private volatile bool _isRunning;
        private string _quitMessage = "[QUIT] SSB Quake Live Interface";
        private int _reconnectTries;

        /// <summary>
        /// Initializes a new instance of the <see cref="Irc"/> class.
        /// </summary>
        public IrcManager(SynServerBot ssb)
        {
            _client = new StandardIrcClient();
            _configHandler = new ConfigHandler();
            IrcSettings = GetIrcSettingsFromConfig();
            var ircCmdProcessor = new IrcCommandProcessor(ssb, this);
            _ircEvent = new IrcEvents(IrcSettings, ircCmdProcessor);
            _validIrcNick = new Regex(@"^([a-zA-Z\[\]\\`_\^\{\|\}][a-zA-Z0-9\[\]\\`_\^\{\|\}-]{1,15})");
        }

        /// <summary>
        /// Gets the IRC configuration settings.
        /// </summary>
        /// <value>
        /// The IRC configuration settings.
        /// </value>
        public IrcOptions IrcSettings { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bot is connected to IRC.
        /// </summary>
        /// <value>
        /// <c>true</c> if the bot is connected to irc; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnectedToIrc { get; set; }

        /// <summary>
        /// Gets the Irc client registration information.
        /// </summary>
        /// <value>
        /// The Irc client registration information.
        /// </value>
        public IrcRegistrationInfo RegistrationInfo
        {
            get
            {
                return new IrcUserRegistrationInfo
                {
                    NickName = IrcSettings.ircNickName,
                    UserName = IrcSettings.ircUserName,
                    RealName = IrcSettings.ircRealName,
                    Password = IrcSettings.ircServerPassword,
                };
            }
        }

        /// <summary>
        /// Gets the valid IRC nickname regex.
        /// </summary>
        /// <value>
        /// The valid IRC nickname regex.
        /// </value>
        public Regex ValidIrcNickRegex
        {
            get { return _validIrcNick; }
        }

        /// <summary>
        /// Attempts to reconnect to the IRC server if the connection initially fails.
        /// </summary>
        /// <param name="isManualReconnection">if set to <c>true</c> then the
        /// reconnection attempt was user-issued; if set to <c>false</c>
        /// then the reconnection attempt was the result of being unable to initially
        /// connect.</param>
        public void AttemptReconnection(bool isManualReconnection)
        {
            if (_reconnectTries >= MaxReconnectionTries && !isManualReconnection)
            {
                Log.Write("Maximum reconnection attempts exceeded. Will not retry.",
                    _logClassType, _logPrefix);
                return;
            }

            Log.Write(string.Format("{0}",
                ((isManualReconnection) ?
                "Attempting IRC reconnection (user-issued)" :
                string.Format("Attempting IRC reconnection. Attempt #{0}", _reconnectTries))),
                _logClassType, _logPrefix);

            try
            {
                Disconnect();
            }
            catch (Exception)
            {
                // IrcDotNet: Client might already be disposed
            }
            finally
            {
                _isRunning = false;
                IsConnectedToIrc = false;
                StartIrcThread();
                if (!isManualReconnection)
                {
                    _reconnectTries++;
                }
            }
        }

        /// <summary>
        /// Checks whether the information necessary for IRC nickname service
        /// authentication is complete.
        /// </summary>
        /// <returns><c>true</c> if the information is complete, otherwise
        /// <c>false</c>.</returns>
        public bool AuthInfoIsValid()
        {
            return !string.IsNullOrEmpty(IrcSettings.ircNickServiceBot) &&
                   !string.IsNullOrEmpty(IrcSettings.ircNickServiceUsername) &&
                   !string.IsNullOrEmpty(IrcSettings.ircNickServicePassword);
        }

        /// <summary>
        /// Connects the client to the Irc server.
        /// </summary>
        public void Connect()
        {
            if (!RequiredIrcSettingsAreValid()) return;

            using (_client = new StandardIrcClient())
            {
                _client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
                _client.Connected += _ircEvent.IrcClient_Connected;
                _client.Disconnected += _ircEvent.IrcClient_Disconnected;
                _client.Registered += _ircEvent.IrcClient_Registered;

                // Wait until connection has succeeded or timed out.
                using (var registeredEvent = new ManualResetEventSlim(false))
                {
                    using (var connectedEvent = new ManualResetEventSlim(false))
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        _client.Connected += (sender2, e2) => connectedEvent.Set();
                        // ReSharper disable once AccessToDisposedClosure
                        _client.Registered += (sender2, e2) => registeredEvent.Set();
                        _client.Connect(IrcSettings.ircServerAddress, Convert.ToInt32(IrcSettings.ircServerPort),
                            false, RegistrationInfo);
                        if (!connectedEvent.Wait(10000))
                        {
                            Log.Write(string.Format("Connection to '{0}:{1}' timed out. Will try to reconnect up to {2} times.",
                                IrcSettings.ircServerAddress, IrcSettings.ircServerPort,
                                MaxReconnectionTries), _logClassType, _logPrefix);

                            StopIrcThread();
                            AttemptReconnection(false);
                            return;
                        }
                    }
                    Log.Write(string.Format("Now connected to '{0}:{1}'.",
                        IrcSettings.ircServerAddress, IrcSettings.ircServerPort), _logClassType, _logPrefix);
                    if (!registeredEvent.Wait(10000))
                    {
                        Log.Write(string.Format("Could not register to '{0}:{1}'. Will try to reconnect up to {2} times.",
                            IrcSettings.ircServerAddress,
                            IrcSettings.ircServerPort, MaxReconnectionTries), _logClassType, _logPrefix);

                        StopIrcThread();
                        AttemptReconnection(false);
                        return;
                    }
                }

                Log.Write(string.Format("Now registered to '{0}:{1}' as '{2}'.",
                    IrcSettings.ircServerAddress, IrcSettings.ircServerPort,
                    IrcSettings.ircUserName), _logClassType, _logPrefix);

                // Connected: reset attempt count
                _reconnectTries = 0;
                IsConnectedToIrc = true;
                Loop();
            }
        }

        /// <summary>
        /// Disconnects the client from the IRC server.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                Log.Write("Closing IRC client connection.", _logClassType, _logPrefix);
                _client.Quit(_quitMessage);
            }
            catch (Exception)
            {
                // IrcDotNet: Client might already be disposed
            }
            finally
            {
                _reconnectTries = MaxReconnectionTries;
                StopIrcThread();
                IsConnectedToIrc = false;
            }
        }

        /// <summary>
        /// Gets the IRC user level of a user in the main channel.
        /// </summary>
        /// <param name="nickname">The user's nickname.</param>
        /// <returns>The user's IRC user level as an <see cref="IrcUserLevel"/>
        /// enum value, if it exists.</returns>
        public IrcUserLevel GetIrcUserLevel(string nickname)
        {
            if (_client.Channels.Count == 0) return IrcUserLevel.None;
            if (IsUserVoicedInChannel(nickname)) return IrcUserLevel.Voice;
            if (IsUserOppedInChannel(nickname)) return IrcUserLevel.Operator;
            if (IsUserChannelAdmin(nickname)) return IrcUserLevel.Admin;
            if (IsUserChannelOwner(nickname)) return IrcUserLevel.Owner;

            return IrcUserLevel.None;
        }

        /// <summary>
        /// Ops the user.
        /// </summary>
        /// <param name="nickname">The user's nickname.</param>
        public void OpUser(string nickname)
        {
            var user = GetUserInChannel(nickname);
            if (user == null) return;
            if (user.User.NickName.Equals(IrcSettings.ircAdminNickname,
                StringComparison.InvariantCultureIgnoreCase))
            {
                user.Op();
            }
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
        public bool RequiredIrcSettingsAreValid()
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
            if (string.IsNullOrEmpty(cfg.ircServerAddress) || cfg.ircServerAddress.Contains(" ")) return false;
            // IRC port validity
            if (cfg.ircServerPort > 65535) return false;
            return true;
        }

        /// <summary>
        /// Sends a specified IRC message to the specified target.
        /// </summary>
        /// <param name="target">The target (nickname or channel)
        ///  to which the message should be sent.</param>
        /// <param name="message">The message.</param>
        public void SendIrcMessage(string target, string message)
        {
            _client.LocalUser.SendMessage(target, message);
        }

        /// <summary>
        /// Sends a specified IRC notice to the specified target.
        /// </summary>
        /// <param name="target">The target (nickname or channel)
        ///  to which the notice should be sent.</param>
        /// <param name="message">The message.</param>
        public void SendIrcNotice(string target, string message)
        {
            _client.LocalUser.SendNotice(target, message);
        }

        /// <summary>
        ///     Starts the console read thread.
        /// </summary>
        public void StartIrcThread()
        {
            if (_isRunning) return;
            _isRunning = true;
            var ircThread = new Thread(Connect) { IsBackground = true };
            ircThread.Start();
        }

        /// <summary>
        ///     Stops the console read thread.
        /// </summary>
        public void StopIrcThread()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Gets the IRC settings from the configuration file.
        /// </summary>
        private IrcOptions GetIrcSettingsFromConfig()
        {
            _configHandler.ReadConfiguration();
            return _configHandler.Config.IrcOptions;
        }

        /// <summary>
        /// Gets the user in the channel.
        /// </summary>
        /// <param name="nickname">The nickname.</param>
        /// <returns>The specified nickname as an <see cref="IrcChannelUser"/> object.
        /// </returns>
        private IrcChannelUser GetUserInChannel(string nickname)
        {
            if (_client.Channels.Count == 0) return null;
            IrcChannelUser user = null;
            var currentChan = _client.Channels.First().Users;
            foreach (var u in currentChan.Where(u => u.User.NickName
                .Equals(nickname, StringComparison.InvariantCultureIgnoreCase)))
            {
                user = u;
            }
            return user;
        }

        /// <summary>
        /// Determines whether the specified user is a channel admin (&)
        /// </summary>
        /// <param name="nickname">The nickname to check.</param>
        /// <returns><c>true</c> if the user is a channel admin.</returns>
        /// <remarks>This usermode (&) is inapplicable to QuakeNet's IRCD, on which
        /// operator (@) is the highest possible user mode in a channel.</remarks>
        private bool IsUserChannelAdmin(string nickname)
        {
            var user = GetUserInChannel(nickname);
            return user != null && user.Modes.Contains('a');
        }

        /// <summary>
        /// Determines whether the specified user is a channel owner (~)
        /// </summary>
        /// <param name="nickname">The nickname to check.</param>
        /// <returns><c>true</c> if the user is a channel owner.</returns>
        /// <remarks>This usermode (~) is inapplicable to QuakeNet's IRCD, on which
        /// operator (@) is the highest possible user mode in a channel.</remarks>
        private bool IsUserChannelOwner(string nickname)
        {
            var user = GetUserInChannel(nickname);
            return user != null && user.Modes.Contains('q');
        }

        /// <summary>
        /// Determines whether the specified user is a channel operator.
        /// </summary>
        /// <param name="nickname">The nickname to check.</param>
        /// <returns><c>true</c> if the user is a channel operator, otherwise <c>false</c>.</returns>
        private bool IsUserOppedInChannel(string nickname)
        {
            var user = GetUserInChannel(nickname);
            return user != null && user.Modes.Contains('o');
        }

        /// <summary>
        /// Determines whether the specified user is voiced in the channel.
        /// </summary>
        /// <param name="nickname">The nickname to check.</param>
        /// <returns><c>true</c>if the user is voiced in the channel, otherwise <c>false</c>.</returns>
        private bool IsUserVoicedInChannel(string nickname)
        {
            var user = GetUserInChannel(nickname);
            return user != null && user.Modes.Contains('v');
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
        /// Loops while the client is active to keep it alive.
        /// </summary>
        private void Loop()
        {
            while (_isRunning)
            {
                Thread.Sleep(10);
            }
            Disconnect();
        }
    }
}