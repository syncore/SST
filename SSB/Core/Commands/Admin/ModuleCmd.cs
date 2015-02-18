using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SSB.Core.Commands.Modules;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Class for module access.
    /// </summary>
    public class ModuleCmd : IBotCommand
    {
        public const string AccountDateLimitArg = AccountDateLimit.NameModule;
        public const string AccuracyArg = Accuracy.NameModule;
        public const string AutoVoteArg = AutoVoter.NameModule;
        public const string EarlyQuitArg = EarlyQuit.NameModule;
        public const string EloLimitArg = EloLimit.NameModule;
        public const string IrcArg = Irc.NameModule;
        public const string MotdArg = Motd.NameModule;
        public const string PickupArg = Pickup.NameModule;
        public const string ServersArg = Servers.NameModule;
        private const string ActiveModuleArg = "active";
        private readonly SynServerBot _ssb;
        private readonly List<string> _validModuleNames;
        private bool _isIrcAccessAllowed = true;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModuleCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public ModuleCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _validModuleNames = new List<string>
            {
                AccountDateLimitArg,
                AccuracyArg,
                AutoVoteArg,
                EarlyQuitArg,
                EloLimitArg,
                MotdArg,
                IrcArg,
                PickupArg,
                ServersArg
            };
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <type> <args> ^7 - types: {2} - ^7For active: ^4{0}{1} {3}",
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

                case AccountDateLimitArg:
                    await _ssb.Mod.AccountDateLimit.EvalModuleCmdAsync(c);
                    break;

                case AccuracyArg:
                    await _ssb.Mod.Accuracy.EvalModuleCmdAsync(c);
                    break;

                case AutoVoteArg:
                    await _ssb.Mod.AutoVoter.EvalModuleCmdAsync(c);
                    break;

                case EarlyQuitArg:
                    await _ssb.Mod.EarlyQuit.EvalModuleCmdAsync(c);
                    break;

                case EloLimitArg:
                    await _ssb.Mod.EloLimit.EvalModuleCmdAsync(c);
                    break;

                case IrcArg:
                    await _ssb.Mod.Irc.EvalModuleCmdAsync(c);
                    break;

                case MotdArg:
                    await _ssb.Mod.Motd.EvalModuleCmdAsync(c);
                    break;

                case PickupArg:
                    await _ssb.Mod.Pickup.EvalModuleCmdAsync(c);
                    break;

                case ServersArg:
                    await _ssb.Mod.Servers.EvalModuleCmdAsync(c);
                    break;
            }
        }

        /// <summary>
        ///     Displays the active modules.
        /// </summary>
        private async Task DisplayActiveModules(CmdArgs c)
        {
            var activeMods = _ssb.Mod.GetActiveModules();
            if (activeMods.Count == 0)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^7[MOD]^1 No modules are active at this time. Use: ^3{0}{1}^7 to activate module(s).",
                        CommandProcessor.BotCommandPrefix, c.CmdName));
                return;
            }

            var sb = new StringBuilder();
            foreach (var mod in activeMods)
            {
                sb.Append(string.Format("^7{0}^2, ", mod.ModuleName));
            }
            await
                _ssb.QlCommands.QlCmdSay(string.Format("^2[ACTIVE MODULES]^7 {0}",
                    sb.ToString().TrimEnd(',', ' ')));
        }
    }
}