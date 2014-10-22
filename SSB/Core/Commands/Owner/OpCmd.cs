using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Owner
{
    /// <summary>
    ///     Command: Give a player operator privileges.
    /// </summary>
    public class OpCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Owner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OpCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public OpCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            HasAsyncExecution = false;
        }

        /// <summary>
        ///     Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
        /// </value>
        public bool HasAsyncExecution { get; private set; }

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
        public void DisplayArgLengthError(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        public void Exec(CmdArgs c)
        {
            DoOp(c.Args[1]);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task ExecAsync(CmdArgs c)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Sends the 'op' command to QL.
        /// </summary>
        /// <param name="player">The player.</param>
        private void DoOp(string player)
        {
            string id = _ssb.ServerEventProcessor.GetPlayerId(player);
            if (!String.IsNullOrEmpty(id))
            {
                _ssb.QlCommands.SendToQl(string.Format("op {0}", id), false);
            }
            else
            {
                Debug.WriteLine(string.Format("Unable to op player {0} because ID could not be retrieved.",
                    player));
            }
        }
    }
}