using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using IrcDotNet;
using SSB.Config;
using SSB.Config.Modules;
using SSB.Enum;

namespace SSB.Core.Modules.Irc
{
    public class IrcManager
    {
        // MaxNickLength: 15 for QuakeNet; Freenode is 16; some others might be up to 32
        private const int MaxNickLength = 15;

        private readonly StandardIrcClient _client;
        private readonly ConfigHandler _configHandler;
        private readonly IrcEventHandlers _ircEventHandlers;
        

        // Regex for testing validity of IRC nick according to IRC RFC specification;
        // currently set from 2-15 max length
        private readonly Regex _validIrcNick;

        //private bool _isDisposed;
        private string _quitMessage = "[QUIT] SSB Quake Live Interface";

        /// <summary>
        /// Initializes a new instance of the <see cref="Irc"/> class.
        /// </summary>
        public IrcManager(SynServerBot ssb)
        {
            _client = new StandardIrcClient();
            _configHandler = new ConfigHandler();
            IrcSettings = GetIrcSettingsFromConfig();
            IrcCmdProcessor = new IrcCommandProcessor(ssb, this);
            _ircEventHandlers = new IrcEventHandlers(IrcSettings, IrcCmdProcessor);
            
            _validIrcNick = new Regex(@"^([a-zA-Z\[\]\\`_\^\{\|\}][a-zA-Z0-9\[\]\\`_\^\{\|\}-]{1,15})");
        }

        // TODO: disposal code

        /// <summary>
        /// Gets the IRC command processor.
        /// </summary>
        /// <value>
        /// The IRC command processor.
        /// </value>
        public IrcCommandProcessor IrcCmdProcessor { get; private set; }

        /// <summary>
        /// Gets the IRC configuration settings.
        /// </summary>
        /// <value>
        /// The IRC configuration settings.
        /// </value>
        public IrcOptions IrcSettings { get; private set; }

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
                    UserModes = { 'i' }
                };
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

            using (_client)
            {
                _client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
                _client.Connected += _ircEventHandlers.IrcClient_Connected;
                _client.Disconnected += _ircEventHandlers.IrcClient_Disconnected;
                _client.Registered += _ircEventHandlers.IrcClient_Registered;
                // Wait until connection has succeeded or timed out.
                using (var registeredEvent = new ManualResetEventSlim(false))
                {
                    using (var connectedEvent = new ManualResetEventSlim(false))
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        _client.Connected += (sender2, e2) => connectedEvent.Set();
                        // ReSharper disable once AccessToDisposedClosure
                        _client.Registered += (sender2, e2) => registeredEvent.Set();
                        _client.Connect(IrcSettings.ircServerAddress, Convert.ToInt32(IrcSettings.ircServerPort), false, RegistrationInfo);
                        if (!connectedEvent.Wait(10000))
                        {
                            Debug.WriteLine("Connection to '{0}:{1}' timed out.",
                                IrcSettings.ircServerAddress, IrcSettings.ircServerPort);
                            return;
                        }
                    }
                    Debug.WriteLine("Now connected to '{0}:{1}'.",
                        IrcSettings.ircServerAddress, IrcSettings.ircServerPort);
                    if (!registeredEvent.Wait(10000))
                    {
                        Debug.WriteLine("Could not register to '{0}:{1}'.",
                            IrcSettings.ircServerAddress, IrcSettings.ircServerPort);
                        return;
                    }
                }

                Console.Out.WriteLine("Now registered to '{0}:{1}' as '{2}'.",
                    IrcSettings.ircServerAddress, IrcSettings.ircServerPort, IrcSettings.ircUserName);
                HandleEventLoop(_client);
            }
        }

        /// <summary>
        /// Disconnects the client from the IRC server.
        /// </summary>
        public void Disconnect()
        {
            // Quit in three seconds
            _client.Quit(3000, _quitMessage);
            // TODO: dispose
        }

        /// <summary>
        /// Gets the IRC user level of a user in the main channel.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's IRC user level as an <see cref="IrcUserLevel"/>
        /// enum value, if it exists.</returns>
        public IrcUserLevel GetIrcUserLevel(string user)
        {
            if (_client.Channels.Count == 0) return IrcUserLevel.None;
            var userList = _client.Channels[0].Users;
            foreach (var u in userList.Where(u => u.User.NickName.Equals
                (user, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (IsUserVoicedInChannel(u)) return IrcUserLevel.Voice;
                if (IsUserOppedInChannel(u)) return IrcUserLevel.Operator;
                if (IsUserChannelAdmin(u)) return IrcUserLevel.Admin;
                if (IsUserChannelOwner(u)) return IrcUserLevel.Owner;
            }
            return IrcUserLevel.None;
        }

        /// <summary>
        /// Ops the user.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void OpUser(object obj)
        {
            if (obj is IrcUser)
            {
                var user = (IrcUser)obj;
                IrcChannelUser cUser = user.GetChannelUsers().First(g => g.User.NickName == user.NickName);
                cUser.Op();
            }
            if (obj is IrcChannelUser)
            {
                var user = (IrcChannelUser)obj;
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
            if (string.IsNullOrEmpty(cfg.ircServerAddress)) return false;
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
        /// Gets the IRC settings from the configuration file.
        /// </summary>
        private IrcOptions GetIrcSettingsFromConfig()
        {
            _configHandler.ReadConfiguration();
            return _configHandler.Config.IrcOptions;
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
        /// Determines whether the specified user is a channel admin (&)
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if the user is a channel admin.</returns>
        /// <remarks>This usermode (&) is inapplicable to QuakeNet's IRCD, on which
        /// operator (@) is the highest possible user mode in a channel.</remarks>
        private bool IsUserChannelAdmin(object obj)
        {
            if (obj is IrcUser)
            {
                var user = (IrcUser)obj;
                IrcChannelUser cUser = user.GetChannelUsers().First(g => g.User.NickName == user.NickName);
                return cUser.Modes.Contains('a');
            }
            if (obj is IrcChannelUser)
            {
                var user = (IrcChannelUser)obj;
                return user.Modes.Contains('a');
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified user is a channel owner (~)
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if the user is a channel owner.</returns>
        /// <remarks>This usermode (~) is inapplicable to QuakeNet's IRCD, on which
        /// operator (@) is the highest possible user mode in a channel.</remarks>
        private bool IsUserChannelOwner(object obj)
        {
            if (obj is IrcUser)
            {
                var user = (IrcUser)obj;
                IrcChannelUser cUser = user.GetChannelUsers().First(g => g.User.NickName == user.NickName);
                return cUser.Modes.Contains('q');
            }
            if (obj is IrcChannelUser)
            {
                var user = (IrcChannelUser)obj;
                return user.Modes.Contains('q');
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified user is a channel operator.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if the user is a channel operator, otherwise <c>false</c>.</returns>
        private bool IsUserOppedInChannel(object obj)
        {
            if (obj is IrcUser)
            {
                var user = (IrcUser)obj;
                IrcChannelUser cUser = user.GetChannelUsers().First(g => g.User.NickName == user.NickName);
                return cUser.Modes.Contains('o');
            }
            if (obj is IrcChannelUser)
            {
                var user = (IrcChannelUser)obj;
                return user.Modes.Contains('o');
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified user is voiced in the channel.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c>if the user is voiced in the channel, otherwise <c>false</c>.</returns>
        private bool IsUserVoicedInChannel(object obj)
        {
            if (obj is IrcUser)
            {
                var user = (IrcUser)obj;
                IrcChannelUser cUser = user.GetChannelUsers().First(g => g.User.NickName == user.NickName);
                return cUser.Modes.Contains('v');
            }
            if (obj is IrcChannelUser)
            {
                var user = (IrcChannelUser)obj;
                return user.Modes.Contains('v');
            }
            return false;
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
    }
}