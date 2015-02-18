using System.Linq;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: display general help information to irc users.
    /// </summary>
    public class IrcHelpCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcHelpCmd" /> class.
        /// </summary>
        /// <param name="irc">The IRC interface.</param>
        public IrcHelpCmd(IrcManager irc)
        {
            MinArgs = 0;
            IsAsync = false;
            _irc = irc;
        }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync { get; private set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs { get; private set; }

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
        /// <remarks>
        ///     Not implemented, as this command takes no arguments.
        /// </remarks>
        public void DisplayArgLengthError(CmdArgs c)
        {
        }

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void Exec(CmdArgs c)
        {
            var cmdList = IrcCommandProcessor.IrcCommandList.Select(cmd =>
                string.Format("{0}{1}", IrcCommandProcessor.IrcCommandPrefix, cmd.Key)).ToList();

            //TODO: update the URL
            _irc.SendIrcNotice(c.FromUser,
                string.Format(
                    "\u0003[COMMANDS]: \u0002{0}\u0002 - for more information, visit: http://ssb.syncore.org/help",
                    string.Join(",", cmdList)));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        ///     Not implemented, as this is not an async command.
        /// </remarks>
        public Task ExecAsync(CmdArgs c)
        {
            return null;
        }
    }
}