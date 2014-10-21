using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands
{
    public class KickBanCmd : IBotCommand
    {
        private int _minArgs = 2;
        private readonly SynServerBot _ssb;
        private UserLevel _userLevel = UserLevel.Admin;

        public KickBanCmd(SynServerBot ssb)
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
        /// <exception cref="System.NotImplementedException"></exception>
        public void DisplayArgLengthError(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Exec(CmdArgs c)
        {
            throw new NotImplementedException();
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
