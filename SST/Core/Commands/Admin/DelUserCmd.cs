﻿using System.Threading.Tasks;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Admin
{
    /// <summary>
    /// Command: Removes a user from the tool's internal user database.
    /// </summary>
    public class DelUserCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Admin;
        private readonly DbUsers _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelUserCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public DelUserCmd(SynServerTool sst)
        {
            _sst = sst;
            _users = new DbUsers();
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        public async Task<bool> ExecAsync(Cmd c)
        {
            var todelUserLevel = _users.GetUserLevel(Helpers.GetArgVal(c, 1));
            var result = _users.DeleteUserFromDb(Helpers.GetArgVal(c, 1), c.FromUser,
                _users.GetUserLevel(c.FromUser));
            if (result == UserDbResult.Success)
            {
                StatusMessage = string.Format("^2[SUCCESS]^7 Removed user^2 {0}^7 from the^2 [{1}] ^7group.",
                    Helpers.GetArgVal(c, 1), todelUserLevel);
                await SendServerSay(c, StatusMessage);

                // UI: reflect changes
                _sst.UserInterface.RefreshCurrentSstUsersDataSource();

                // de-op
                if (!_sst.IsMonitoringServer) return true;
                if (!_sst.ServerInfo.CurrentPlayers.ContainsKey(Helpers.GetArgVal(c, 1))) return true;
                var id = _sst.ServerEventProcessor.GetPlayerId(Helpers.GetArgVal(c, 1));
                if (id != -1)
                {
                    // doesn't matter if not opped, since QL shows no error message
                    await _sst.QlCommands.SendToQlAsync(string.Format("deop {0}", id), false);
                }

                return true;
            }

            StatusMessage = string.Format(
                "^1[ERROR]^3 Unable to remove user^1 {0}^3 from the ^1[{1}]^3 group. Code:^1 {2}",
                Helpers.GetArgVal(c, 1), todelUserLevel, result);
            await SendServerTell(c, StatusMessage);
            return false;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return (string.Format(
                "^1[ERROR]^3 Usage: {0}{1} user - user is without clantag",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName)));
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message, false);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}
