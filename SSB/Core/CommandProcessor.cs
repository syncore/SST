using System.Collections.Generic;
using System.Threading.Tasks;
using SSB.Core.Commands;
using SSB.Core.Commands.Limits;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for processing bot commands.
    /// </summary>
    public class CommandProcessor
    {
        public const string BotCommandPrefix = "!";
        private readonly Dictionary<string, IBotCommand> _commands;
        private readonly SynServerBot _ssb;
        private readonly Users _users;
        private string NoPermission = "^1[ERROR]^7 You do not have permission to use that command.";

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandProcessor" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public CommandProcessor(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
            Limiter = new Limiter(_ssb);
            _commands = new Dictionary<string, IBotCommand>
            {
                {"access", new AccessCmd(_ssb)},
                {"adduser", new AddUserCmd(_ssb)},
                {"deluser", new DelUserCmd(_ssb)},
                {"help", new HelpCmd(_ssb)},
                {"limit", new LimitCmd(_ssb, Limiter)},
                {"kickban", new KickBanCmd(_ssb)}
            };
        }

        /// <summary>
        ///     Gets the limiter.
        /// </summary>
        /// <value>
        ///     The limiter.
        /// </value>
        public Limiter Limiter { get; private set; }

        /// <summary>
        ///     Processes the bot command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessBotCommand(string fromUser, string msg)
        {
            char[] sep = {' '};
            string[] args = msg.Split(sep, 5);
            string cmdName = args[0].Substring(1);
            IBotCommand ic;

            if (msg.Equals(BotCommandPrefix))
            {
                return;
            }
            if (!_commands.TryGetValue(cmdName, out ic))
            {
                return;
            }
            if (!UserHasReqLevel(fromUser, _commands[cmdName].UserLevel))
            {
                return;
            }
            var c = new CmdArgs(args, cmdName, fromUser);
            if (args.Length < _commands[cmdName].MinArgs)
            {
                _commands[cmdName].DisplayArgLengthError(c);
                return;
            }
            // Execute
            if (_commands[cmdName].HasAsyncExecution)
            {
                await _commands[cmdName].ExecAsync(c);
            }
            else
            {
                _commands[cmdName].Exec(c);
            }
        }

        /// <summary>
        ///     Checks whether the user has the required access level to access a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="requiredLevel">The required access level.</param>
        /// <returns><c>true</c> if the user has the required access level, otherwise <c>false</c>.</returns>
        private bool UserHasReqLevel(string user, UserLevel requiredLevel)
        {
            _users.RetrieveAllUsers();
            if (_users.GetUserLevel(user) >= requiredLevel)
            {
                return true;
            }
            _ssb.QlCommands.QlCmdSay(NoPermission);
            return false;
        }
    }
}