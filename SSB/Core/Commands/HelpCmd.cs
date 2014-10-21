using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands
{
    /// <summary>
    ///     Command: Help command
    /// </summary>
    public class HelpCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;

        private int _minArgs = 0;

        private UserLevel _userLevel = UserLevel.None;

        public HelpCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        /// Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        /// <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
        /// </value>
        public bool HasAsyncExecution { get; set; }

        /// <summary>
        /// Gets the minimum arguments.
        /// </summary>
        /// <value>
        /// The minimum arguments.
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
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        /// Uses the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        public void Exec(CmdArgs c)
        {
            //TODO: implement
            _ssb.QlCommands.QlCmdSay("^7The ^2!help ^7command will go here.");
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task ExecAsync(CmdArgs c)
        {
            throw new NotImplementedException();
        }
    }
}