using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    /// Command: Indefinitely pause a match.
    /// </summary>
    public class PauseCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        /// Initializes a new instance of the <see cref="PauseCmd"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PauseCmd(SynServerBot ssb)
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
            if (GetGameState() != QlGameStates.InProgress) return;
            _ssb.QlCommands.SendToQl("pause", false);
            _ssb.QlCommands.QlCmdSay("^7Pausing game indefinitely... Use the unpause command to un-pause.");
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

        /// <summary>
        /// Gets the state of the game.
        /// </summary>
        /// <returns>The state of the game.</returns>
        private QlGameStates GetGameState()
        {
            var gamestate = _ssb.ServerInfo.CurrentGameState;
            _ssb.QlCommands.SendToQl("g_gameState", false);
            return gamestate;
        }
    }
}