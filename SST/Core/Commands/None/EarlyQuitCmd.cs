using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SST.Core.Commands.Admin;
using SST.Core.Modules.Irc;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    ///     Command: user command for listing the early quitters in the database.
    /// </summary>
    public class EarlyQuitCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:EARLYQUIT]";
        private readonly int _qlMinArgs = 2;
        private readonly DbQuits _quitDb;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EarlyQuitCmd" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public EarlyQuitCmd(SynServerTool sst)
        {
            _sst = sst;
            _quitDb = new DbQuits();
        }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs
        {
            get { return _qlMinArgs + 1; }
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
            if (!_sst.Mod.EarlyQuit.Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Early quit module has not been loaded. An admin must" +
                    " first load it with:^7 {0}{1} {2}",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}",
                            IrcCommandList.IrcCmdQl, CommandList.CmdModule))
                        : CommandList.CmdModule),
                    ModuleCmd.EarlyQuitArg);
                await SendServerTell(c, StatusMessage);

                Log.Write(
                    string.Format(
                        "{0} attempted {1} command from {2}, but {3} module is not loaded. Ignoring.",
                        c.FromUser, c.CmdName, ((c.FromIrc) ? "from IRC" : "from in-game"),
                        ModuleCmd.EarlyQuitArg), _logClassType, _logPrefix);

                return false;
            }

            if ((!Helpers.GetArgVal(c, 1).Equals("list")) &&
                (!Helpers.GetArgVal(c, 1).Equals("check")))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (Helpers.GetArgVal(c, 1).Equals("list"))
            {
                await ListQuits(c);
                return true;
            }
            if (Helpers.GetArgVal(c, 1).Equals("check"))
            {
                return await EvalCheckQuits(c);
            }
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
                "^1[ERROR]^3 Usage: {0}{1} <list> <check>",
                CommandList.GameCommandPrefix,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}",
                        c.CmdName, c.Args[1]))
                    : c.CmdName));
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Checks the amount of early quits that a given user has.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task CheckQuits(CmdArgs c)
        {
            var totalQuits = _quitDb.GetUserQuitCount(Helpers.GetArgVal(c, 2));
            var qStr = totalQuits > 0
                ? string.Format("^1 {0} ^7early quits", totalQuits)
                : "^2no^7 early quits";
            var quitsRemaining = (_sst.Mod.EarlyQuit.MaxQuitsAllowed - totalQuits);
            StatusMessage = string.Format(
                "^5[EARLYQUIT] ^3{0}^7 has {1}. ^1 {2} ^7remaining before banned for ^1{3} {4}",
                Helpers.GetArgVal(c, 2), qStr, quitsRemaining, _sst.Mod.EarlyQuit.BanTime,
                _sst.Mod.EarlyQuit.BanTimeScale);
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Evaluates the quit check command.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task<bool> EvalCheckQuits(CmdArgs c)
        {
            if (c.Args.Length != (c.FromIrc ? 4 : 3))
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} <check> <name> - name is without the clan tag",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}"
                            , c.CmdName, c.Args[1]))
                        : c.CmdName));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            await CheckQuits(c);
            return true;
        }

        /// <summary>
        ///     Lists all early quits, if any.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task ListQuits(CmdArgs c)
        {
            var quits = _quitDb.GetAllQuitters();
            var quitBuilder = new StringBuilder();
            foreach (var q in quits)
            {
                quitBuilder.Append(string.Format("{0}({1})", q.Name, q.QuitCount));
            }

            StatusMessage = string.Format("^5[EARLYQUIT]^7 {0}",
                ((quits.Count != 0)
                    ? (string.Format(
                        "Early quitters: ^1{0}^7 - To see quits remaining: ^3{1}{2} check <player>",
                        quitBuilder, CommandList.GameCommandPrefix,
                        ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName)))
                    : "No players have quit early."));
            await SendServerSay(c, StatusMessage);
        }
    }
}