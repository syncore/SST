using System;
using System.Collections.Generic;
using SST.Interfaces;

namespace SST.Core.Modules.Irc
{
    /// <summary>
    /// Class that contains the IRC commands.
    /// </summary>
    public class IrcCommandList
    {
        public const string IrcCmdQl = "ql";
        public const string IrcCommandPrefix = "!";
        private readonly IrcManager _irc;
        private readonly SynServerTool _sst;
        private readonly string IrcCmdHelp = "help";
        private readonly string IrcCmdMods = "mods";
        private readonly string IrcCmdMonitor = "monitor";
        private readonly string IrcCmdOpMe = "opme";
        private readonly string IrcCmdSay = "say";
        private readonly string IrcCmdSayTeam = "sayteam";
        private readonly string IrcCmdStatus = "status";
        private readonly string IrcCmdUsers = "users";
        private readonly string IrcCmdVersion = "version";
        private readonly string IrcCmdWho = "who";

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandList"/> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcCommandList(SynServerTool sst, IrcManager irc)
        {
            _sst = sst;
            _irc = irc;
            Commands = new Dictionary<string, IIrcCommand>(StringComparer.InvariantCultureIgnoreCase);
            InitializeCommands();
        }

        /// <summary>
        /// Gets the IRC commands.
        /// </summary>
        /// <value>The IRC commands.</value>
        public Dictionary<string, IIrcCommand> Commands { get; private set; }

        /// <summary>
        /// Initializes the IRC commands.
        /// </summary>
        private void InitializeCommands()
        {
            Commands.Add(IrcCmdSay, new IrcSayCmd(_sst, _irc));
            Commands.Add(IrcCmdSayTeam, new IrcSayTeamCmd(_sst, _irc));
            Commands.Add(IrcCmdWho, new IrcWhoCmd(_sst, _irc));
            Commands.Add(IrcCmdStatus, new IrcStatusCmd(_sst, _irc));
            Commands.Add(IrcCmdMods, new IrcModsCmd(_sst, _irc));
            Commands.Add(IrcCmdMonitor, new IrcMonitorCmd(_sst, _irc));
            Commands.Add(IrcCmdUsers, new IrcUsersCmd(_sst, _irc));
            Commands.Add(IrcCmdOpMe, new IrcOpMeCmd(_irc));
            Commands.Add(IrcCmdQl, new IrcQlCmd(_sst, _irc));
            Commands.Add(IrcCmdVersion, new IrcVersionCmd(_irc));
            Commands.Add(IrcCmdHelp, new IrcHelpCmd(_irc, Commands));
        }
    }
}
