using System;
using System.Reflection;
using IrcDotNet;
using SST.Config.Modules;
using SST.Util;

namespace SST.Core.Modules.Irc
{
    /// <summary>
    /// Class responsible for explicitly handling various events defined by the IRC protocol.
    /// </summary>
    public class IrcEvents : IrcEventHandlers
    {
        private readonly IrcCommandProcessor _ircCommandProcessor;
        private readonly IrcOptions _ircSettings;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:IRC]";

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcEvents"/> class.
        /// </summary>
        /// <param name="ircSettings">The irc settings.</param>
        /// <param name="ircCommandProcessor">The irc command processor.</param>
        public IrcEvents(IrcOptions ircSettings, IrcCommandProcessor ircCommandProcessor)
        {
            _ircSettings = ircSettings;
            _ircCommandProcessor = ircCommandProcessor;
        }

        /// <summary>
        /// Called when a message is received in the channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">
        /// The <see cref="IrcMessageEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnChannelMessageReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
            if (e.Source is IrcUser)
            {
                Log.Write(string.Format("[{0}] {1}: {2}", channel.Name, e.Source.Name, e.Text),
                    _logClassType, _logPrefix);

                // If it's a command, then process it
                if (e.Text.StartsWith(IrcCommandList.IrcCommandPrefix))
                {
                    // ReSharper disable once UnusedVariable (synchronous)
                    var i = _ircCommandProcessor.ProcessIrcCommand(e.Source.Name,
                        e.Text.ToLowerInvariant());
                }
            }
            else
            {
                Log.Write(string.Format("[{0}] {1}: {2}", channel.Name, e.Source.Name, e.Text),
                    _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Called when a channel-wide notice is received.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">
        /// The <see cref="IrcMessageEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnChannelNoticeReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
            Log.Write(string.Format("[{0}] Notice: {1}", channel.Name, e.Text), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Called when a user joins the channel in which our irc client is currently located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">
        /// The <see cref="IrcChannelUserEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnChannelUserJoined(IrcChannel channel, IrcChannelUserEventArgs e)
        {
            Log.Write(
                string.Format("[{0}] User: {1} joined the channel", channel.Name, e.ChannelUser.User.NickName),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Called when a user leaves the channel in which our irc client is currenty located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">
        /// The <see cref="IrcChannelUserEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnChannelUserLeft(IrcChannel channel, IrcChannelUserEventArgs e)
        {
            Log.Write(string.Format("[{0}] User {1} left the channel",
                channel.Name, e.ChannelUser.User.NickName), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Called when the irc client connects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected override void OnClientConnect(IrcClient client)
        {
            if (client.LocalUser != null)
            {
                Log.Write(string.Format("IRC client with name {0} connected to IRC {1}",
                    client.LocalUser.NickName, client.LocalUser.ServerName), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Called when the irc client disconnects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected override void OnClientDisconnect(IrcClient client)
        {
            if (client.LocalUser != null && client.ServerName != null)
            {
                Log.Write(string.Format("IRC client with name {0} disconnected from {1}",
                    client.LocalUser.NickName, client.ServerName), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Called when the server registers the irc client's connection.
        /// </summary>
        /// <param name="client">The client.</param>
        protected override void OnClientRegistered(IrcClient client)
        {
            // Automatically identify with services, i.e.: msg "Q@CServe.quakenet.org" AUTH user password
            if (_ircSettings.autoAuthWithNickService && IsAuthInfoValid())
            {
                // Has to be sent as a raw message, because IrcDotNet library does not accept '@' in
                // message target, which is at least necessary for QuakeNet authentication.
                // TODO: Perhaps make universal auth for networks other than Quakenet
                client.SendRawMessage(string.Format("PRIVMSG {0} :AUTH {1} {2}",
                    _ircSettings.ircNickServiceBot,
                    _ircSettings.ircNickServiceUsername,
                    _ircSettings.ircNickServicePassword));

                Log.Write("Attempted to automatically auth with IRC service",
                    _logClassType, _logPrefix);
            }

            // Join the main channel
            var channel = Tuple.Create(_ircSettings.ircChannel, _ircSettings.ircChannelKey);
            client.Channels.Join(channel);
        }

        /// <summary>
        /// Called when our irc client joins a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">
        /// The <see cref="IrcChannelEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnLocalUserJoinedChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
            Log.Write(string.Format("We joined channel: {0}", e.Channel.Name),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Called when our irc client leaves a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">
        /// The <see cref="IrcChannelEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnLocalUserLeftChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
            Log.Write(string.Format("We left channel: {0}", e.Channel.Name),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Called when our irc client receives a message.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">
        /// The <see cref="IrcMessageEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnLocalUserMessageReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
            Log.Write(string.Format("We received message from {0}: {1}", e.Source.Name, e.Text),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Called when our irc client receives a notice.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">
        /// The <see cref="IrcMessageEventArgs"/> instance containing the event data.
        /// </param>
        protected override void OnLocalUserNoticeReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
            Log.Write(string.Format("We received notice from {0}: {1}", e.Source.Name, e.Text),
                _logClassType, _logPrefix);

            // Services (Quakenet) auto authentication
            if (_ircSettings.hideHostnameOnQuakeNet)
            {
                if (e.Text.StartsWith("You are now logged in",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    localUser.SetModes("+x");
                    Log.Write("Setting mode +x to hide our hostname", _logClassType, _logPrefix);
                }
            }
        }

        /// <summary>
        /// Checks whether the information necessary for IRC nickname service authentication is complete.
        /// </summary>
        /// <returns><c>true</c> if the information is complete, otherwise <c>false</c>.</returns>
        private bool IsAuthInfoValid()
        {
            return !string.IsNullOrEmpty(_ircSettings.ircNickServiceBot) &&
                   !string.IsNullOrEmpty(_ircSettings.ircNickServiceUsername) &&
                   !string.IsNullOrEmpty(_ircSettings.ircNickServicePassword);
        }
    }
}
