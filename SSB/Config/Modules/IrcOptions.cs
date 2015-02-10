using System;

namespace SSB.Config.Modules
{
    /// <summary>
    ///     Model class that represents the Internet Relay Chat (IRC) module options for the configuration file.
    /// </summary>
    public class IrcOptions
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the IRC client should
        ///     attempt to automatically authenticate with the nickname service bot
        ///     specified by <see cref="ircNickServiceName" /> using the password specified
        ///     by <see cref="ircNickServicePassword" />.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the IRC client should automatically authenticate with the IRC
        ///     server's nickname service bot specified by <see cref="ircNickServiceName" /> using the
        ///     password specified by <see cref="ircNickServicePassword" />.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool autoAuthWithNickService { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether an IRC connection should
        ///     automatically be made when the bot is launched, provided that the
        ///     module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if an IRC connection should be made on start,
        ///     otherwise, <c>false</c>.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool autoConnectOnStart { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether or not to hide the bot's
        ///     hostname when identified with the QuakeNet Q nickname service.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the bot's hostname should be hidden when identified with
        ///     the QuakeNet Q nickname service, otherwise <c>false</c>.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool hideHostnameOnQuakeNet { get; set; }

        /// <summary>
        ///     Gets or sets nickname of the main IRC admin.
        ///     This user can request operator status from the bot,
        ///     in order to issue commands. Note, there is no hostname
        ///     verification, so this is to be used with caution, if at all.
        ///     The preferable way of handling operator status is allowing the
        ///     IRC server's channel services to handle it, but this setting is
        ///     included for servers without such services.
        /// </summary>
        /// <value>
        ///     The nickname of the main IRC admin. Note: there is no hostname
        ///     verification, so this is to be used with caution, if at all.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircAdminNickname { get; set; }

        /// <summary>
        ///     Gets or sets the name of the main IRC channel.
        /// </summary>
        /// <value>
        ///     The name of the main IRC channel.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircChannel { get; set; }

        /// <summary>
        ///     Gets or sets the main IRC channel's key, if required.
        /// </summary>
        /// <value>
        ///     The key to the main IRC channel, if required.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircChannelKey { get; set; }

        /// <summary>
        ///     Gets or sets the IRC nickname.
        /// </summary>
        /// <value>
        ///     The IRC nickname.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircNickName { get; set; }

        /// <summary>
        ///     Gets or sets the name of the IRC nickname services bot,
        ///     for example, "Q@CServe.quakenet.org" for QuakeNet, used for
        ///     nickname authentication.
        /// </summary>
        /// <value>
        ///     The name of the IRC nickname services bot, used for
        ///     nickname authentication.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircNickServiceName { get; set; }

        /// <summary>
        ///     Gets or sets the password used for nickname authentication
        ///     with the <see cref="ircNickServiceName" /> auth bot.
        ///     nickname authentication.
        /// </summary>
        /// <value>
        ///     The password used for nickname authentication with the IRC
        ///     service's nickname bot.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircNickServicePassword { get; set; }

        /// <summary>
        ///     Gets or sets the IRC real name.
        /// </summary>
        /// <value>
        ///     The IRC real name.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircRealName { get; set; }

        /// <summary>
        ///     Gets or sets the address of the IRC server.
        /// </summary>
        /// <value>
        ///     The address of the IRC server.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircServerAddress { get; set; }

        /// <summary>
        ///     Gets or sets the password of the IRC server. This is empty by
        ///     default.
        /// </summary>
        /// <value>
        ///     The password of the IRC server. Empty by default.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircServerPassword { get; set; }

        /// <summary>
        ///     Gets or sets the port of the IRC server. The default is
        ///     6667.
        /// </summary>
        /// <value>
        ///     The port of the IRC server. The default is 6667.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public uint ircServerPort { get; set; }

        /// <summary>
        ///     Gets or sets the IRC user name (ident).
        /// </summary>
        /// <value>
        ///     The IRC user name (ident).
        /// </value>
        // ReSharper disable once InconsistentNaming
        public string ircUserName { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this module is active; otherwise, <c>false</c>.
        /// </value>
        // ReSharper disable once InconsistentNaming
        public bool isActive { get; set; }

        /// <summary>
        ///     Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            autoAuthWithNickService = false;
            autoConnectOnStart = false;
            hideHostnameOnQuakeNet = true;
            ircAdminNickname = string.Empty;
            ircChannel = string.Empty;
            ircChannelKey = string.Empty;
            ircNickName = string.Format("SSB|QLive-{0}", GenerateRandomIdentifier());
            ircNickServiceName = "Q@CServe.quakenet.org";
            ircNickServicePassword = string.Empty;
            ircRealName = "SSB Internet Relay Chat Interface";
            ircServerAddress = string.Empty;
            ircServerPassword = string.Empty;
            ircServerPort = 6667;
            ircUserName = string.Format("SSBQuakeLive{0}", GenerateRandomIdentifier());
        }

        /// <summary>
        ///     Generates a random identifier.
        /// </summary>
        /// <returns>A random identifier in the range of 0 to 32768.</returns>
        private string GenerateRandomIdentifier()
        {
            var r = new Random();
            return r.Next(0, 32769).ToString();
        }
    }
}