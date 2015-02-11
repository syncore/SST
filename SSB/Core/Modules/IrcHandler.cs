using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using IrcDotNet;
using SSB.Config;
using SSB.Core.Commands.Modules;

namespace SSB.Core.Modules
{
    public class IrcHandler
    {
        // MaxNickLength: 15 for QuakeNet; Freenode is 16; some others might be up to 32
        private const int MaxNickLength = 15;

        private readonly StandardIrcClient _client;
        private readonly ConfigHandler _configHandler;

        // Regex for testing validity of IRC nick according to IRC RFC specification;
        // currently set from 2-15 max length
        private readonly Regex _validIrcNick;

        private bool _autoAuthWithNickService;

        private bool _autoConnectOnStart;

        private bool _hideHostnameOnQuakeNet;

        private string _ircAdminNickname;

        private string _ircChannel;

        private string _ircChannelKey;

        private string _ircNickName;

        private string _ircNickServiceBot;

        private string _ircNickServiceUsername;
        
        private string _ircNickServicePassword;

        private string _ircRealName;

        private string _ircServerAddress;

        private string _ircServerPassword;

        private uint _ircServerPort;

        private string _ircUserName;

        //private bool _isDisposed;
        private string _quitMessage = "SSB QL Interface Quit Message";

        /// <summary>
        /// Initializes a new instance of the <see cref="Irc"/> class.
        /// </summary>
        public IrcHandler()
        {
            _client = new StandardIrcClient();
            _configHandler = new ConfigHandler();
            SetIrcSettingsFromConfig();
            _validIrcNick = new Regex(@"^([a-zA-Z\[\]\\`_\^\{\|\}][a-zA-Z0-9\[\]\\`_\^\{\|\}-]{1,15})");
        }

        //~Irc()
        //{
        //    Dispose(false);
        //}

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //protected void Dispose(bool disposing)
        //{
        //    if (!_isDisposed)
        //    {
        //        if (disposing)
        //        {
        //            // Gracefully disconnect the client
        //                if (_client != null)
        //                {
        //                    _client.Quit(3000, _quitMessage);
        //                    _client.Dispose();
        //                }

        //        }
        //    }
        //    _isDisposed = true;
        //}

        /// <summary>
        /// Gets a value indicating whether to automatically authenticate with
        /// the IRC nickname authentication service.
        /// </summary>
        /// <value>
        /// <c>true</c> if automatic authentication with nick auth service is to occur;
        ///  otherwise, <c>false</c>.
        /// </value>
        public bool AutoAuthWithNickService { get { return _autoAuthWithNickService; } }

        /// <summary>
        /// Gets the main IRC channel.
        /// </summary>
        /// <value>
        /// The main IRC channel.
        /// </value>
        public string IrcChannel { get { return _ircChannel; } }

        /// <summary>
        /// Gets the IRC channel key.
        /// </summary>
        /// <value>
        /// The IRC channel key.
        /// </value>
        public string IrcChannelKey { get { return _ircChannelKey; } }

        /// <summary>
        /// Gets the name of the IRC nickname authentication service bot.
        /// </summary>
        /// <value>
        /// The name of the IRC nickname authentication service bot.
        /// </value>
        public string IrcNickServiceBot { get { return _ircNickServiceBot; } }

        /// <summary>
        /// Gets the username to be sent to the IRC nickname authentication service bot.
        /// </summary>
        /// <value>
        /// The username to be sent to the IRC nickname authentication service bot.
        /// </value>
        public string IrcNickServiceUsername { get { return _ircNickServiceUsername;} }
        
        /// <summary>
        /// Gets the password to be sent to the IRC nickname authentication service.
        /// </summary>
        /// <value>
        /// The password to be sent to the IRC nickname authentication service.
        /// </value>
        public string IrcNickServicePassword { get { return _ircNickServicePassword; } }

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
                    NickName = _ircNickName,
                    UserName = _ircUserName,
                    RealName = _ircRealName,
                    Password = _ircServerPassword
                    //UserModes = { 'i' }
                };
            }
        }

        /// <summary>
        /// Connects the client to the Irc server.
        /// </summary>
        public void Connect()
        {
            if (!RequiredIrcSettingsAreValid()) return;

            using (_client)
            {
                _client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
                _client.Disconnected += IrcClient_Disconnected;
                _client.Registered += IrcClient_Registered;
                // Wait until connection has succeeded or timed out.
                using (var registeredEvent = new ManualResetEventSlim(false))
                {
                    using (var connectedEvent = new ManualResetEventSlim(false))
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        _client.Connected += (sender2, e2) => connectedEvent.Set();
                        // ReSharper disable once AccessToDisposedClosure
                        _client.Registered += (sender2, e2) => registeredEvent.Set();
                        _client.Connect(_ircServerAddress, Convert.ToInt32(_ircServerPort), false, RegistrationInfo);
                        if (!connectedEvent.Wait(10000))
                        {
                            Debug.WriteLine("Connection to '{0}:{1}' timed out.", _ircServerAddress, _ircServerPort);
                            return;
                        }
                    }
                    Debug.WriteLine("Now connected to '{0}:{1}'.", _ircServerAddress, _ircServerPort);
                    if (!registeredEvent.Wait(10000))
                    {
                        Debug.WriteLine("Could not register to '{0}:{1}'.", _ircServerAddress, _ircServerPort);
                        return;
                    }
                }

                Console.Out.WriteLine("Now registered to '{0}:{1}' as '{2}'.", _ircServerAddress, _ircServerPort, _ircUserName);
                HandleEventLoop(_client);
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
            if (string.IsNullOrEmpty(cfg.ircServerAddress)) return false;
            // IRC port validity
            if (cfg.ircServerPort > 65535) return false;
            return true;
        }

        /// <summary>
        /// Called when a message is received in the channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        protected virtual void OnChannelMessageReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when a channel-wide notice is received.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        protected virtual void OnChannelNoticeReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when a user joins the channel in which our irc client is currently located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs"/> instance containing the event data.</param>
        protected virtual void OnChannelUserJoined(IrcChannel channel, IrcChannelUserEventArgs e)
        {
        }

        /// <summary>
        /// Called when a user leaves the channel in which our irc client is currenty located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs"/> instance containing the event data.</param>
        protected virtual void OnChannelUserLeft(IrcChannel channel, IrcChannelUserEventArgs e)
        {
        }

        /// <summary>
        /// Called when the irc client connects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnClientConnect(IrcClient client)
        {
        }

        /// <summary>
        /// Called when the irc client disconnects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnClientDisconnect(IrcClient client)
        {
        }

        /// <summary>
        /// Called when the server registers the irc client's connection.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnClientRegistered(IrcClient client)
        {
        }

        /// <summary>
        /// Called when our irc client joins a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs"/> instance containing the event data.</param>
        protected virtual void OnLocalUserJoinedChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
        }

        /// <summary>
        /// Called when our irc client leaves a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs"/> instance containing the event data.</param>
        protected virtual void OnLocalUserLeftChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
        }

        /// <summary>
        /// Called when our irc client receives a message.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        protected virtual void OnLocalUserMessageReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when our irc client receives a notice.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        protected virtual void OnLocalUserNoticeReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        /// Event handler that handles the MessageReceived event of the channel. Occurs when a channel on which the
        /// client (bot) is in receives a message.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        private void Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelMessageReceived(channel, e);
        }

        /// <summary>
        /// Event handler that handles the NoticeReceived event of the channel. Occurs when a channel on which
        /// the client (bot) is in receives a notice.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        private void Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelNoticeReceived(channel, e);
        }

        /// <summary>
        /// Event handler that handles the UserJoined event of the channel. Occurs when a user joins a channel in which the
        /// client (bot) is located.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs"/> instance containing the event data.</param>
        private void Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelUserJoined(channel, e);
        }

        /// <summary>
        /// Event handler that handles the UserLeft event of the channel. Occurs when a user leaves a channel in which
        /// the client (bot) is located.
        ///
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs"/> instance containing the event data.</param>
        private void Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelUserLeft(channel, e);
        }

        /// <summary>
        /// Gets the default reply target.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="source">The source.</param>
        /// <param name="targets">The targets.</param>
        /// <returns></returns>
        private IList<IIrcMessageTarget> GetDefaultReplyTarget(IrcClient client, IIrcMessageSource source,
            IList<IIrcMessageTarget> targets)
        {
            if (targets.Contains(client.LocalUser) && source is IIrcMessageTarget)
            {
                return new[] { (IIrcMessageTarget)source };
            }
            return targets;
        }

        /// <summary>
        /// Handles the event loop.
        /// </summary>
        /// <param name="client">The client.</param>
        private void HandleEventLoop(IrcClient client)
        {
            bool isExit = false;
            while (!isExit)
            {
                Console.Write("> ");
                var command = Console.ReadLine();
                switch (command)
                {
                    case "exit":
                        isExit = true;
                        break;

                    default:
                        if (!string.IsNullOrEmpty(command))
                        {
                            if (command.StartsWith("/") && command.Length > 1)
                            {
                                client.SendRawMessage(command.Substring(1));
                            }
                            else
                            {
                                Debug.WriteLine("unknown command '{0}'", command);
                            }
                        }
                        break;
                }
            }
            client.Quit(3000, _quitMessage);
        }

        /// <summary>
        /// Handles the Connected event of the IrcClient. Occurs when the client (bot) has
        /// connected to the server. The <see cref="IrcClient_Registered"/> event is what actually
        /// occurs once the client is registered with the server.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IrcClient_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
            OnClientConnect(client);
        }

        /// <summary>
        /// Handles the Disconnected event of the IrcClient. Occurs when the client (bot)
        /// has disconnected from the server.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IrcClient_Disconnected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
            OnClientDisconnect(client);
        }

        /// <summary>
        /// Handles the Registered event of the Irc client. Occurs when the connection
        /// has been registered.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void IrcClient_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += LocalUser_LeftChannel;

            OnClientRegistered(client);
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
        /// Handles the JoinedChannel event of the LocalUser. Occurs when the client (bot)
        /// has joined a channel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs"/> instance containing the event data.</param>
        private void LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined += Channel_UserJoined;
            e.Channel.UserLeft += Channel_UserLeft;
            e.Channel.MessageReceived += Channel_MessageReceived;
            e.Channel.NoticeReceived += Channel_NoticeReceived;

            OnLocalUserJoinedChannel(localUser, e);
        }

        /// <summary>
        /// Handles the LeftChannel event of the LocalUser. Occurs
        /// when the client (bot) leaves the channel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs"/> instance containing the event data.</param>
        private void LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined -= Channel_UserJoined;
            e.Channel.UserLeft -= Channel_UserLeft;
            e.Channel.MessageReceived -= Channel_MessageReceived;
            e.Channel.NoticeReceived -= Channel_NoticeReceived;

            OnLocalUserLeftChannel(localUser, e);
        }

        /// <summary>
        /// Handles the MessageReceived event of the LocalUser. Occurs when the client (bot) receives a
        /// private message.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        private void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            OnLocalUserMessageReceived(localUser, e);
        }

        /// <summary>
        /// Handles the NoticeReceived event of the LocalUser. Occurs when the client (bot)
        /// receives a notice.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs"/> instance containing the event data.</param>
        private void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            OnLocalUserNoticeReceived(localUser, e);
        }

        /// <summary>
        /// Sets the IRC settings from the configuration file.
        /// </summary>
        private void SetIrcSettingsFromConfig()
        {
            _configHandler.ReadConfiguration();
            var cfg = _configHandler.Config.IrcOptions;
            _autoAuthWithNickService = cfg.autoAuthWithNickService;
            _autoConnectOnStart = cfg.autoConnectOnStart;
            _hideHostnameOnQuakeNet = cfg.hideHostnameOnQuakeNet;
            _ircAdminNickname = cfg.ircAdminNickname;
            _ircChannel = cfg.ircChannel;
            _ircChannelKey = cfg.ircChannelKey;
            _ircNickName = cfg.ircNickName;
            _ircNickServiceBot = cfg.ircNickServiceBot;
            _ircNickServiceUsername = cfg.ircNickServiceUsername;
            _ircNickServicePassword = cfg.ircNickServicePassword;
            _ircRealName = cfg.ircRealName;
            _ircServerAddress = cfg.ircServerAddress;
            _ircServerPassword = cfg.ircServerPassword;
            _ircServerPort = cfg.ircServerPort;
            _ircUserName = cfg.ircUserName;
        }
    }
}