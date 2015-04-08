using System;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    ///     IRC command: allow the admin to request operator status.
    /// </summary>
    public class IrcOpMeCmd : IIrcCommand
    {
        private int _ircMinArgs = 0;
        private bool _isAsync = false;
        private readonly IrcManager _irc;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.None;
        private bool _requiresMonitoring = false;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[IRCCMD:OPME]";

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
        /// Gets a value indicating whether this command requires
        /// the bot to be monitoring a server before it can be used.
        /// </summary>
        /// <value>
        /// <c>true</c> if this command requires the bot to be monitoring
        /// a server; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresMonitoring
        {
            get { return _requiresMonitoring; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs
        {
            get { return _ircMinArgs; }
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
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed,
        /// otherwise returns <c>false</c>.
        /// </returns>
        public bool Exec(CmdArgs c)
        {
            if (!c.FromUser.Equals(_irc.IrcSettings.ircAdminNickname, StringComparison.InvariantCultureIgnoreCase))
            {
                _irc.SendIrcNotice(c.FromUser,
                    "\u0002[ERROR]\u0002 You do not have permission to access this command.");
                
                Log.Write(string.Format("{0} requested IRC operator status in {1}, but lacks permission. Ignoring.",
                    c.FromUser, _irc.IrcSettings.ircChannel), _logClassType, _logPrefix);

                return false;
            }

            _irc.OpUser(c.FromUser);
            
            Log.Write(string.Format("Giving operator status to {0} in {1}",
                c.FromUser, _irc.IrcSettings.ircChannel), _logClassType, _logPrefix);
            
            return true;
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed,
        /// otherwise returns <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     Not implemented, as this is not an async command.
        /// </remarks>
        public Task<bool> ExecAsync(CmdArgs c)
        {
            return null;
        }
    }
}