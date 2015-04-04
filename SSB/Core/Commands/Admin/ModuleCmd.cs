using System.Collections.Generic;
using System.Threading.Tasks;
using SSB.Core.Commands.Modules;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

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
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _qlMinArgs = 2;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.Admin;
        private readonly List<string> _validModuleNames;

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
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

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
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

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
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was successfully executed, otherwise
        ///     <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            switch (Helpers.GetArgVal(c, 1))
            {
                case ActiveModuleArg:
                    StatusMessage = GetActiveModules(c);
                    await SendServerSay(c, StatusMessage);
                    return true;

                case AccountDateLimitArg:
                    await _ssb.Mod.AccountDateLimit.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.AccountDateLimit.StatusMessage;
                    return true;

                case AccuracyArg:
                    await _ssb.Mod.Accuracy.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.Accuracy.StatusMessage;
                    return true;

                case AutoVoteArg:
                    await _ssb.Mod.AutoVoter.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.AutoVoter.StatusMessage;
                    return true;

                case EarlyQuitArg:
                    await _ssb.Mod.EarlyQuit.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.EarlyQuit.StatusMessage;
                    return true;

                case EloLimitArg:
                    await _ssb.Mod.EloLimit.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.EloLimit.StatusMessage;
                    return true;

                case IrcArg:
                    await _ssb.Mod.Irc.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.Irc.StatusMessage;
                    return true;

                case MotdArg:
                    await _ssb.Mod.Motd.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.Motd.StatusMessage;
                    return true;

                case PickupArg:
                    await _ssb.Mod.Pickup.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.Pickup.StatusMessage;
                    return true;

                case ServersArg:
                    await _ssb.Mod.Servers.EvalModuleCmdAsync(c);
                    StatusMessage = _ssb.Mod.Servers.StatusMessage;
                    return true;
            }
            StatusMessage = string.Format("^1[ERROR]^3 Invalid module. Valid modules are: ^1{0}",
                string.Join(", ", _validModuleNames));
            await SendServerTell(c, StatusMessage);
            return false;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <type> <args> - types: {2} - For active: {0}{1} {3}",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName),
                string.Join(", ", _validModuleNames),
                ActiveModuleArg);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Displays the active modules.
        /// </summary>
        private string GetActiveModules(CmdArgs c)
        {
            var activeCount = _ssb.Mod.ActiveModuleCount;

            if (activeCount == 0)
            {
                return string.Format(
                    "^7[MOD]^1 No modules are active at this time. Use: {0}{1} to activate module(s).",
                    CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
            }

            var activeMods = _ssb.Mod.GetActiveModules();
            return string.Format("^2[ACTIVE MODULES]^7 {0}",
                activeMods);
        }
    }
}