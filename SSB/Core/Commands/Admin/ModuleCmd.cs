﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public const string AccountDateLimitArg = AccountDateLimit.NameModule;
        public const string AutoVoteArg = AutoVoter.NameModule;
        public const string EloLimitArg = EloLimit.NameModule;
        private const string ActiveModuleArg = "active";
        private readonly Module _module;
        private readonly List<IModule> _moduleList;
        private readonly SynServerBot _ssb;
        private readonly List<string> _validModuleNames;
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
            _validModuleNames = new List<string> { AccountDateLimitArg, AutoVoteArg, EloLimitArg };
            _moduleList = new List<IModule> { _module.AccountDateLimit, _module.AutoVoter, _module.EloLimit };
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
                "^1[ERROR]^3 Usage: {0}{1} <type> <args> ^7 - possible types are: {2} - ^5{0}{3}^7 to see list of currently active modules.",
                CommandProcessor.BotCommandPrefix, c.CmdName, string.Join(", ", _validModuleNames),
                ActiveModuleArg));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            switch (c.Args[1])
            {
                case ActiveModuleArg:
                    await DisplayActiveModules(c);
                    break;

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

        /// <summary>
        ///     Displays the active modules.
        /// </summary>
        private async Task DisplayActiveModules(CmdArgs c)
        {
            if (!_moduleList.Any(mod => mod.Active))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^7[MOD]^1 No modules are active at this time. Use: ^3{0}{1}^7 to activate module(s).",
                        CommandProcessor.BotCommandPrefix, c.CmdName));
                return;
            }

            var sb = new StringBuilder();
            foreach (IModule mod in _moduleList.Where(mod => mod.Active))
            {
                sb.Append(string.Join("^7^2, ", mod.ModuleName));
            }
            await
                _ssb.QlCommands.QlCmdSay(string.Format("^2[ACTIVE MODULES]^7 {0}",
                    sb.ToString().TrimEnd(',', ' ')));
        }
    }
}