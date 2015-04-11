using System;
using IrcDotNet;

namespace SST.Core.Modules.Irc
{
    /// <summary>
    ///     Class that specifies various IRC-related event handlers.
    /// </summary>
    public class IrcEventHandlers
    {
        /// <summary>
        ///     Handles the Connected event of the IrcClient. Occurs when the client (bot) has
        ///     connected to the server. The <see cref="IrcClient_Registered" /> event is what actually
        ///     occurs once the client is registered with the server.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        public void IrcClient_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
            OnClientConnect(client);
        }

        /// <summary>
        ///     Handles the Disconnected event of the IrcClient. Occurs when the client (bot)
        ///     has disconnected from the server.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        public void IrcClient_Disconnected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
            OnClientDisconnect(client);
        }

        /// <summary>
        ///     Handles the Registered event of the Irc client. Occurs when the connection
        ///     has been registered.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        public void IrcClient_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += LocalUser_LeftChannel;

            OnClientRegistered(client);
        }

        /// <summary>
        ///     Called when a message is received in the channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected virtual void OnChannelMessageReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        ///     Called when a channel-wide notice is received.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected virtual void OnChannelNoticeReceived(IrcChannel channel, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        ///     Called when a user joins the channel in which our irc client is currently located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs" /> instance containing the event data.</param>
        protected virtual void OnChannelUserJoined(IrcChannel channel, IrcChannelUserEventArgs e)
        {
        }

        /// <summary>
        ///     Called when a user leaves the channel in which our irc client is currenty located.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs" /> instance containing the event data.</param>
        protected virtual void OnChannelUserLeft(IrcChannel channel, IrcChannelUserEventArgs e)
        {
        }

        /// <summary>
        ///     Called when the irc client connects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnClientConnect(IrcClient client)
        {
        }

        /// <summary>
        ///     Called when the irc client disconnects.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnClientDisconnect(IrcClient client)
        {
        }

        /// <summary>
        ///     Called when the server registers the irc client's connection.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnClientRegistered(IrcClient client)
        {
        }

        /// <summary>
        ///     Called when our irc client joins a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLocalUserJoinedChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
        }

        /// <summary>
        ///     Called when our irc client leaves a channel.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLocalUserLeftChannel(IrcLocalUser localUser, IrcChannelEventArgs e)
        {
        }

        /// <summary>
        ///     Called when our irc client receives a message.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLocalUserMessageReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        ///     Called when our irc client receives a notice.
        /// </summary>
        /// <param name="localUser">The local user.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLocalUserNoticeReceived(IrcLocalUser localUser, IrcMessageEventArgs e)
        {
        }

        /// <summary>
        ///     Event handler that handles the MessageReceived event of the channel. Occurs when a channel on which the
        ///     client (bot) is in receives a message.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        private void Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelMessageReceived(channel, e);
        }

        /// <summary>
        ///     Event handler that handles the NoticeReceived event of the channel. Occurs when a channel on which
        ///     the client (bot) is in receives a notice.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        private void Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelNoticeReceived(channel, e);
        }

        /// <summary>
        ///     Event handler that handles the UserJoined event of the channel. Occurs when a user joins a channel in which the
        ///     client (bot) is located.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs" /> instance containing the event data.</param>
        private void Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelUserJoined(channel, e);
        }

        /// <summary>
        ///     Event handler that handles the UserLeft event of the channel. Occurs when a user leaves a channel in which
        ///     the client (bot) is located.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelUserEventArgs" /> instance containing the event data.</param>
        private void Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            OnChannelUserLeft(channel, e);
        }

        /// <summary>
        ///     Handles the JoinedChannel event of the LocalUser. Occurs when the client (bot)
        ///     has joined a channel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs" /> instance containing the event data.</param>
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
        ///     Handles the LeftChannel event of the LocalUser. Occurs
        ///     when the client (bot) leaves the channel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcChannelEventArgs" /> instance containing the event data.</param>
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
        ///     Handles the MessageReceived event of the LocalUser. Occurs when the client (bot) receives a
        ///     private message.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        private void LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            OnLocalUserMessageReceived(localUser, e);
        }

        /// <summary>
        ///     Handles the NoticeReceived event of the LocalUser. Occurs when the client (bot)
        ///     receives a notice.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="IrcMessageEventArgs" /> instance containing the event data.</param>
        private void LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            OnLocalUserNoticeReceived(localUser, e);
        }
    }
}