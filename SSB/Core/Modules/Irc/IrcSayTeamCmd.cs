using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: send a message from IRC to the game instance's team chat.
    /// </summary>
    public class IrcSayTeamCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly bool _isAsync = true;
        private readonly int _minArgs = 2;
        private readonly SynServerBot _ssb;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcSayTeamCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcSayTeamCmd(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
            _irc = irc;
        }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync
        {
            get { return _isAsync; }
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
        public IrcUserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void DisplayArgLengthError(CmdArgs c)
        {
            _irc.SendIrcNotice(c.FromUser,
                string.Format("\u0002[ERROR]\u0002 The correct usage is: \u0002{0}{1}\u0002 message",
                    IrcCommandProcessor.IrcCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented for this command since it is to be run asynchronously via <see cref="ExecAsync" />
        /// </remarks>
        public void Exec(CmdArgs c)
        {
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns></returns>
        public async Task ExecAsync(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSayTeam(string.Format("^4[IRC]^3 {0}:^7 {1}",
                c.FromUser,
                c.Text.Substring((IrcCommandProcessor.IrcCommandPrefix.Length + c.CmdName.Length) - 1)));
        }
    }
}