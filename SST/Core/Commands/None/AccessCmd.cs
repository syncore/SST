﻿using System.Threading.Tasks;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    /// Command: Check user account access level.
    /// </summary>
    public class AccessCmd : IBotCommand
    {
        private readonly SynServerTool _sst;
        private readonly DbUsers _users;
        private bool _isIrcAccessAllowed = true;
        private int _qlMinArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public AccessCmd(SynServerTool sst)
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
        /// <remarks>Helpers.GetArgVal(c, 1) if specified: user to check</remarks>
        public async Task<bool> ExecAsync(Cmd c)
        {
            StatusMessage = c.Args.Length > ((c.FromIrc) ? 2 : 1)
                ? string.Format("^5{0}'s^7 user level is: ^5[{1}]", Helpers.GetArgVal(c, 1),
                    _users.GetUserLevel(Helpers.GetArgVal(c, 1)))
                : string.Format("^5{0}'s^7 user level is: ^5[{1}]", c.FromUser,
                    _users.GetUserLevel(c.FromUser));
            await SendServerSay(c, StatusMessage);
            return true;
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
            return string.Empty;
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
