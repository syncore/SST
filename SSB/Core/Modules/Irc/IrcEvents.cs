using System;
using System.Diagnostics;
using IrcDotNet;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    /// Class responsible for handling various events defined by the IRC protocol.
    /// </summary>
    public class IrcEvents : IrcHandler
    {
        // TODO: irc command processor class here?

        /// <summary>
        /// Called when a message is received in the channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected override void OnChannelMessageReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
            var client = channel.Client;

            if (e.Source is IrcUser)
            {
                if (e.Text.Equals("test", StringComparison.InvariantCultureIgnoreCase))
                {
                    client.LocalUser.SendMessage(channel, "wee");
                }

                // Read message.
                Debug.WriteLine("[{0}]({1}): {2}", channel.Name, e.Source.Name, e.Text);
            }
            else
            {
                Debug.WriteLine("[{0}]({1}) Message: {2}", channel.Name, e.Source.Name, e.Text);
            }
        }

        /// <summary>
        /// Called when a channel-wide notice is received.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected override void OnChannelNoticeReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
            Debug.WriteLine("[{0}] Notice: {1}", channel.Name, e.Text);
        }

        /// <summary>
        /// Called when a user joins the channel in which our irc client is currently located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs" /> instance containing the event data.</param>
        protected override void OnChannelUserJoined(IrcChannel channel, IrcChannelUserEventArgs e)
        {
            Debug.WriteLine("[{0}] User: {1} joined the channel", channel.Name, e.ChannelUser.User.NickName);
        }

        /// <summary>
        /// Called when a user leaves the channel in which our irc client is currenty located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs" /> instance containing the event data.</param>
        protected override void OnChannelUserLeft(IrcChannel channel, IrcChannelUserEventArgs e)
        {
            Debug.WriteLine("[{0}] User {1} left the channel", channel.Name, e.ChannelUser.User.NickName);
        }

        /// <summary>
        /// Called when the irc client connects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected override void OnClientConnect(IrcClient client)
        {
            //
        }

        /// <summary>
        /// Called when the irc client disconnects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected override void OnClientDisconnect(IrcClient client)
        {
            //
        }

        /// <summary>
        /// Called when the server registers the irc client's connection.
        /// </summary>
        /// <param name="client">The client.</param>
        protected override void OnClientRegistered(IrcClient client)
        {
            // Automatically identify with services, i.e.:
            // msg "Q@CServe.quakenet.org" AUTH user password
            if (AutoAuthWithNickService && AuthInfoIsValid())
            {
                // Has to be sent as a raw message, because IrcDotNet library
                // does not accept '@' in message target, which is at least necessary
                // for QuakeNet authentication.
                // TODO: Perhaps make universal auth for networks other than Quakenet
                client.SendRawMessage(string.Format("PRIVMSG {0} :AUTH {1} {2}",
                    IrcNickServiceBot, IrcNickServiceUsername, IrcNickServicePassword));
            }

            // Join the main channel
            var channel = Tuple.Create(MainChannel, MainChannelKey);
            client.Channels.Join(channel);
            
        }

        /// <summary>
        /// Called when our irc client joins a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs" /> instance containing the event data.</param>
        protected override void OnLocalUserJoinedChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
            Debug.WriteLine("Bot: {0} joined channel: {1}", localUser.NickName, e.Channel.Name);
        }

        /// <summary>
        /// Called when our irc client leaves a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs" /> instance containing the event data.</param>
        protected override void OnLocalUserLeftChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
            Debug.WriteLine("Bot: {0} left channel: {1}", localUser.NickName, e.Channel.Name);
        }

        /// <summary>
        /// Called when our irc client receives a message.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected override void OnLocalUserMessageReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
            if (e.Source is IrcUser)
            {
                Debug.WriteLine("Received message from nickname {0}: {1}", e.Source.Name, e.Text);
            }
            else
            {
                Debug.WriteLine("Received message from {0}: {1}", e.Source.Name, e.Text);
            }
        }

        /// <summary>
        /// Called when our irc client receives a notice.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected override void OnLocalUserNoticeReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
            if (e.Source is IrcUser)
            {
                Debug.WriteLine("Received notice from nickname {0}: {1}", e.Source.Name, e.Text);
            }
            else
            {
                Debug.WriteLine("Received notice from {0}: {1}", e.Source.Name, e.Text);
            }

            // Services (Quakenet) auto authentication
            if (HideQuakeNetHostname)
            {
                if (e.Text.StartsWith("You are now logged in",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    localUser.SetModes("+x");
                    Debug.WriteLine("Hiding hostname...");
                }
            }
        }
    }
}