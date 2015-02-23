using System;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: allow the admin to request operator status.
    /// </summary>
    public class IrcOpMeCmd : IIrcCommand
    {
        private int _minArgs = 0;
        private bool _isAsync = false;
        private readonly IrcManager _irc;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcOpMeCmd" /> class.
        /// </summary>
        /// <param name="irc">The IRC interface.</param>
        public IrcOpMeCmd(IrcManager irc)
        {
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
            if (!c.FromUser.Equals(_irc.IrcSettings.ircAdminNickname, StringComparison.InvariantCultureIgnoreCase))
            {
                _irc.SendIrcNotice(c.FromUser,
                    "\u0002[ERROR]\u0002 You do not have permission to access this command.");
                return;
            }

            _irc.OpUser(c.FromUser);
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