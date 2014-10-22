using System.Collections.Generic;
using System.Threading.Tasks;
using SSB.Core.Commands.Limits;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Limit various player options (elo, account registration date, etc).
    /// </summary>
    public class LimitCmd : IBotCommand
    {
        public const string AccountDateLimitArg = "accountdate";
        public const string EloLimitArg = "elo";
        private readonly Limiter _limiter;
        private readonly SynServerBot _ssb;
        private readonly List<string> _validLimiters;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LimitCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        /// <param name="limiter">The command limiter manager.</param>
        public LimitCmd(SynServerBot ssb, Limiter limiter)
        {
            _ssb = ssb;
            _limiter = limiter;
            _validLimiters = new List<string> {AccountDateLimitArg, EloLimitArg};
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
                CommandProcessor.BotCommandPrefix, c.CmdName, string.Join(", ", _validLimiters)));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            switch (c.Args[1])
            {
                case EloLimitArg:
                    await _limiter.EloLimit.EvalLimitCmdAsync(c);
                    break;

                case AccountDateLimitArg:
                    await _limiter.AccountDateLimit.EvalLimitCmdAsync(c);
                    break;
            }
        }
    }
}