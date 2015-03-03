using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: user command for listing the early quitters in the database.
    /// </summary>
    public class EarlyQuitCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _minArgs = 2;
        private readonly DbQuits _quitDb;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EarlyQuitCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public EarlyQuitCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _quitDb = new DbQuits();
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
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise
        /// <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (!_ssb.Mod.EarlyQuit.Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Early quit module has not been loaded. An admin must" +
                    " first load it with:^7 {0}{1} {2}",
                    CommandList.GameCommandPrefix, CommandList.CmdModule,
                    ModuleCmd.EarlyQuitArg);
                await SendServerTell(c, StatusMessage);
                return false;
            }

            if ((!c.Args[1].Equals("list")) && (!c.Args[1].Equals("check")))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (c.Args[1].Equals("list"))
            {
                await ListQuits(c);
                return true;
            }
            if (c.Args[1].Equals("check"))
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
                CommandList.GameCommandPrefix, c.CmdName);
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
        ///     Checks the amount of early quits that a given user has.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task CheckQuits(CmdArgs c)
        {
            var totalQuits = _quitDb.GetUserQuitCount(c.Args[2]);
            var qStr = totalQuits > 0
                ? string.Format("^1{0}^7 early quits", totalQuits)
                : "^2no^7 early quits";
            var quitsRemaining = (_ssb.Mod.EarlyQuit.MaxQuitsAllowed - totalQuits);
            StatusMessage = string.Format(
                "^5[EARLYQUIT] ^3{0}^7 has {1}. ^1{2}^7 remaining before banned for ^1{3} {4}",
                c.Args[2], qStr, quitsRemaining, _ssb.Mod.EarlyQuit.BanTime,
                _ssb.Mod.EarlyQuit.BanTimeScale);
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Evaluates the quit check command.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task<bool> EvalCheckQuits(CmdArgs c)
        {
            if (c.Args.Length != 3)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} <check> <name> - name is without the clan tag",
                    CommandList.GameCommandPrefix, c.CmdName);
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
            var quits = _quitDb.GetAllQuits();
            StatusMessage = string.Format("^5[EARLYQUIT]^7 {0}",
                ((!string.IsNullOrEmpty(quits))
                    ? (string.Format(
                        "Early quitters: ^1{0}^7 - To see quits remaining: ^3{1}{2} check <player>",
                        quits, CommandList.GameCommandPrefix, c.CmdName))
                    : "No players have quit early."));
            await SendServerSay(c, StatusMessage);
        }
    }
}