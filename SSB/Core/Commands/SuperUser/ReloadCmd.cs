using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.SuperUser
{
    /// <summary>
    ///     Command: Refresh server information.
    /// </summary>
    public class ReloadCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.SuperUser;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReloadCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ReloadCmd(SynServerBot ssb)
        {
            _ssb = ssb;
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
            await _ssb.QlCommands.QlCmdSay("^3[ATTENTION] ^2Reloading...please wait");
            _ssb.ReloadInit();
            
        }
    }
}