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
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <list> <check>",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!_ssb.Mod.EarlyQuit.Active)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 Early quit module has not been loaded. An admin must first load it with:^7 {0}{1} {2}",
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdModule,
                            ModuleCmd.EarlyQuitArg));
                return;
            }

            if ((!c.Args[1].Equals("list")) && (!c.Args[1].Equals("check")))
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[1].Equals("list"))
            {
                await ListQuits(c);
            }
            if (c.Args[1].Equals("check"))
            {
                await EvalCheckQuits(c);
            }
        }

        /// <summary>
        ///     Checks the amount of early quits that a given user has.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task CheckQuits(CmdArgs c)
        {
            var totalQuits = _quitDb.GetUserQuitCount(c.Args[2]);
            var qStr = totalQuits > 0
                ? string.Format("^1{0}^7 early quits", totalQuits)
                : "^2no^7 early quits";
            var quitsRemaining = (_ssb.Mod.EarlyQuit.MaxQuitsAllowed - totalQuits);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[EARLYQUIT] ^3{0}^7 has {1}. ^1{2}^7 remaining before banned for ^1{3} {4}",
                        c.Args[2], qStr, quitsRemaining, _ssb.Mod.EarlyQuit.BanTime,
                        _ssb.Mod.EarlyQuit.BanTimeScale));
        }

        /// <summary>
        ///     Evaluates the quit check command.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalCheckQuits(CmdArgs c)
        {
            if (c.Args.Length != 3)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} <check> <name> - name is without the clan tag",
                    CommandProcessor.BotCommandPrefix, c.CmdName));
            }
            else
            {
                await CheckQuits(c);
            }
        }

        /// <summary>
        ///     Lists all early quits, if any.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task ListQuits(CmdArgs c)
        {
            var quits = _quitDb.GetAllQuits();
            await _ssb.QlCommands.QlCmdSay(string.Format("^5[EARLYQUIT]^7 {0}",
                ((!string.IsNullOrEmpty(quits))
                    ? (string.Format(
                        "Early quitters: ^1{0}^7 - To see quits remaining: ^3{1}{2} check <player>",
                        quits, CommandProcessor.BotCommandPrefix, c.CmdName))
                    : "No players have quit early.")));
        }
    }
}