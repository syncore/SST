using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SST.Core.Commands.Modules;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Admin
{
    /// <summary>
    /// Command: Class for module access.
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
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:MOD]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Admin;
        private readonly List<string> _validModuleNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public ModuleCmd(SynServerTool sst)
        {
            _sst = sst;
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
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        public async Task<bool> ExecAsync(Cmd c)
        {
            switch (Helpers.GetArgVal(c, 1))
            {
                case ActiveModuleArg:
                    StatusMessage = GetActiveModules(c);
                    await SendServerSay(c, StatusMessage);
                    return true;

                case AccountDateLimitArg:
                    await _sst.Mod.AccountDateLimit.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.AccountDateLimit.StatusMessage;
                    return true;

                case AccuracyArg:
                    await _sst.Mod.Accuracy.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.Accuracy.StatusMessage;
                    return true;

                case AutoVoteArg:
                    await _sst.Mod.AutoVoter.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.AutoVoter.StatusMessage;
                    return true;

                case EarlyQuitArg:
                    await _sst.Mod.EarlyQuit.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.EarlyQuit.StatusMessage;
                    return true;

                case EloLimitArg:
                    await _sst.Mod.EloLimit.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.EloLimit.StatusMessage;
                    return true;

                case IrcArg:
                    await _sst.Mod.Irc.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.Irc.StatusMessage;
                    return true;

                case MotdArg:
                    await _sst.Mod.Motd.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.Motd.StatusMessage;
                    return true;

                case PickupArg:
                    await _sst.Mod.Pickup.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.Pickup.StatusMessage;
                    return true;

                case ServersArg:
                    await _sst.Mod.Servers.EvalModuleCmdAsync(c);
                    StatusMessage = _sst.Mod.Servers.StatusMessage;
                    return true;
            }
            StatusMessage = string.Format("^1[ERROR]^3 Invalid module. Valid modules are: ^1{0}",
                string.Join(", ", _validModuleNames));
            await SendServerTell(c, StatusMessage);

            Log.Write(string.Format("{0} specified an invalid module name from {1}",
                c.FromUser, (c.FromIrc) ? "from IRC" : "from in-game"), _logClassType, _logPrefix);

            return false;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <type> <args> - types: {2} - For active: {0}{1} {3}",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName),
                string.Join(", ", _validModuleNames),
                ActiveModuleArg);
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message, false);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        /// Displays the active modules.
        /// </summary>
        private string GetActiveModules(Cmd c)
        {
            var activeCount = _sst.Mod.ActiveModuleCount;

            if (activeCount == 0)
            {
                return string.Format(
                    "^7[MOD]^1 No modules are active at this time. Use: {0}{1} to activate module(s).",
                    CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
            }

            var activeMods = _sst.Mod.GetActiveModules();
            return string.Format("^2[ACTIVE MODULES]^7 {0}",
                activeMods);
        }
    }
}
