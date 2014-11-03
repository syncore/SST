using System.Collections.Generic;
using System.Threading.Tasks;
using SSB.Core.Commands.Modules;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Class for persisted modules
    /// </summary>
    public class ModuleCmd : IBotCommand
    {
        public const string AccountDateLimitArg = "accountdate";
        public const string EloLimitArg = "elo";
        public const string AutoVoteArg = "autovote";
        private readonly Module _module;
        private readonly SynServerBot _ssb;
        private readonly List<string> _validModules;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModuleCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        /// <param name="module">The module manager.</param>
        public ModuleCmd(SynServerBot ssb, Module module)
        {
            _ssb = ssb;
            _module = module;
            _validModules = new List<string> { AccountDateLimitArg, EloLimitArg };
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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <type> <args> ^7 - possible types are: {2}",
                CommandProcessor.BotCommandPrefix, c.CmdName, string.Join(", ", _validModules)));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            switch (c.Args[1])
            {
                case AutoVoteArg:
                    await _module.AutoVoter.EvalModuleCmdAsync(c);
                    break;
                
                case EloLimitArg:
                    await _module.EloLimit.EvalModuleCmdAsync(c);
                    break;

                case AccountDateLimitArg:
                    await _module.AccountDateLimit.EvalModuleCmdAsync(c);
                    break;
            }
        }
    }
}