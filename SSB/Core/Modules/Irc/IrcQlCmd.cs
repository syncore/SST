using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    /// IRC Command: Allow execution of various SSB QL in-game commands via IRC.
    /// Serves as an interface for commands that would typically be executed in-game,
    /// but allow very highly privileged users to issue such commands from the IRC channel.
    /// </summary>
    public class IrcQlCmd : IIrcCommand
    {
        private readonly SynServerBot _ssb;
        private readonly IrcManager _irc;
        private Dictionary<string, IBotCommand> _cmdList; 
        private readonly IrcUserLevel _userLevel = IrcUserLevel.Operator;
        private bool _isAsync = true;
        // For now only require the QL cmd name as an arg. Need to figure out how to show the
        // actual command args on IRC, however.
        private int _minArgs = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcQlCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcQlCmd(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
            _irc = irc;
            _cmdList = _ssb.CommandProcessor.Commands;
        }

        /// <summary>
        /// Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        public bool IsAsync
        {
            get { return _isAsync; }
        }

        /// <summary>
        /// Gets the minimum arguments.
        /// </summary>
        /// <value>
        /// The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>
        /// The user level.
        /// </value>
        public IrcUserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void DisplayArgLengthError(CmdArgs c)
        {
            _irc.SendIrcNotice(c.FromUser, string.Format("\u0002[ERROR]\u0002 The correct usage is: \u0002{0}{1}\u0002" +
                                                         " <ssb command> <ssb command args>",
                IrcCommandList.IrcCommandPrefix, c.CmdName));
            _irc.SendIrcNotice(c.FromUser, string.Format("<ssb command> is a cmd you'd normally issue in-game," +
                                                         " and <ssb cmd args> are its arguments, if required, i.e: {0}{1} {2} syncore",
                IrcCommandList.IrcCommandPrefix, c.CmdName, CommandList.CmdSeen));
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void Exec(CmdArgs c)
        {
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (Helpers.KeyExists(c.Args[1], _cmdList))
            {
                _irc.SendIrcNotice(c.FromUser, "\u0002[ERROR]\u0002 That is not valid command.");
                return;
            }
            if (_cmdList[c.Args[1]].IsIrcAccessAllowed)
            {
                _irc.SendIrcNotice(c.FromUser, "\u0002[ERROR]\u0002 That command can only be accessed from in-game.");
                return;
            }
            // See if the c passed here meets the actual IBotCommand's MinArgs
            // -2 to account for !ql in-game-cmd
            if ((c.Args.Length - 2) < _cmdList[c.Args[1]].MinArgs)
            {
                _irc.SendIrcNotice(c.FromUser, _cmdList[c.Args[1]].GetArgLengthErrorMessage(c));
                return;
            }

            var success = await _cmdList[c.Args[1]].ExecAsync(c);
            if (success)
            {
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    ReplaceQlColorsWithIrcColors(_cmdList[c.Args[1]].StatusMessage));
            }
            else
            {
                _irc.SendIrcNotice(c.FromUser,
                    ReplaceQlColorsWithIrcColors(_cmdList[c.Args[1]].StatusMessage));
            }

        }

        /// <summary>
        /// Removes the QL color characters from the input string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A string with the QL color characters removed.</returns>
        private string RemoveQlColorChars(string input)
        {
            return Regex.Replace(input, "\\^\\d+", string.Empty);
        }

        /// <summary>
        /// Replaces the QL colors with IRC colors.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A string with the QL colors replaced with IRC colors.</returns>
        private string ReplaceQlColorsWithIrcColors(string input)
        {
            return new StringBuilder(input)
                .Replace("^1", TextColor.IrcRed)
                .Replace("^2", TextColor.IrcTeal)
                .Replace("^3", TextColor.IrcDarkGrey)
                .Replace("^4", TextColor.IrcBlue)
                .Replace("^5", TextColor.IrcNavyBlue)
                .Replace("^6", TextColor.IrcMagenta)
                .Replace("^7", TextColor.IrcWhite)
                .ToString();
        }
    }
}
