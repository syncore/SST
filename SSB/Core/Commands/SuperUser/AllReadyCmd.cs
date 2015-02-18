﻿using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.SuperUser
{
    /// <summary>
    ///     Command: Force all players to ready up.
    /// </summary>
    public class AllReadyCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.SuperUser;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AllReadyCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AllReadyCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        /// <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

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
        /// <param name="c"></param>
        /// <remarks>
        ///     Not implemented because the cmd in this class requires no args.
        /// </remarks>
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            await _ssb.QlCommands.SendToQlAsync("allready", false);
        }
    }
}