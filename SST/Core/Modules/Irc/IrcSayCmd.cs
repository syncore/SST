using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Modules.Irc
{
    /// <summary>
    /// IRC command: send a message from IRC to the game instance.
    /// </summary>
    public class IrcSayCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[IRCCMD:SAY]";
        private readonly SynServerTool _sst;
        private int _ircMinArgs = 2;
        private bool _isAsync = true;
        private bool _requiresMonitoring = true;
        private IrcUserLevel _userLevel = IrcUserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcSayCmd"/> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcSayCmd(SynServerTool sst, IrcManager irc)
        {
            _sst = sst;
            _irc = irc;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs
        {
            get { return _ircMinArgs; }
        }

        /// <summary>
        /// Gets a value that determines whether this command is to be executed asynchronously.
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
            _irc.SendIrcNotice(c.FromUser, string.Format("\u0002[ERROR]\u0002 The correct usage is: \u0002{0}{1}\u0002 message",
                IrcCommandList.IrcCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed,
        /// otherwise returns <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Not implemented for this command since it is to be run asynchronously via <see cref="ExecAsync" />
        /// </remarks>
        public bool Exec(CmdArgs c)
        {
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
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            var msg = c.Text.Substring((IrcCommandList.IrcCommandPrefix.Length + c.CmdName.Length) + 1);
            await _sst.QlCommands.QlCmdSay(string.Format("^4[IRC]^3 {0}:^7 {1}", c.FromUser, msg));

            Log.Write(string.Format("Sent {0}'s message ({1}) to QL client from IRC.",
                c.FromUser, msg), _logClassType, _logPrefix);

            return true;
        }
    }
}