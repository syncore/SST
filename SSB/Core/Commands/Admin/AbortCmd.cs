using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    /// Command: Abort a match and return to warmup.
    /// </summary>
    public class AbortCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortCmd"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AbortCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            HasAsyncExecution = false;
        }

        /// <summary>
        /// Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        /// <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
        /// </value>
        public bool HasAsyncExecution { get; private set; }

        /// <summary>
        /// Gets the minimum arguments.
        /// </summary>
        /// <value>
        /// The minimum arguments.
        /// </value>
        public int MinArgs { get { return _minArgs; } }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>
        /// The user level.
        /// </value>
        public UserLevel UserLevel { get { return _userLevel; } }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        public void Exec(CmdArgs c)
        {
            _ssb.QlCommands.SendToQl("abort", false);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task ExecAsync(CmdArgs c)
        {
            throw new NotImplementedException();
        }
    }
}