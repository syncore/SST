﻿using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Owner
{
    /// <summary>
    ///     Command: Give a player operator privileges.
    /// </summary>
    public class OpCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _qlMinArgs = 2;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.Owner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OpCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public OpCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was successfully executed, otherwise
        ///     <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            var id = _ssb.ServerEventProcessor.GetPlayerId(Helpers.GetArgVal(c, 1));
            if (id != -1)
            {
                await _ssb.QlCommands.SendToQlAsync(string.Format("deop {0}", id), false);
                StatusMessage = string.Format("^2[SUCCESS]^7 Attemped to op ^2{0}", Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);
                Debug.WriteLine("OP: Got player id {0} for player: {1}", id, Helpers.GetArgVal(c, 1));
                return true;
            }

            StatusMessage = "^1[ERROR]^3 Player not found. Use player name without clan tag.";
            await SendServerTell(c, StatusMessage);

            Debug.WriteLine(string.Format("Unable to op player {0} because ID could not be retrieved.",
                Helpers.GetArgVal(c, 1)));
            return false;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name - name does NOT include clan tag.",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}",
                c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}