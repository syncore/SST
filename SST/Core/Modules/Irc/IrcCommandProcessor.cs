using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Modules.Irc
{
    /// <summary>
    ///     Class responsible for processing IRC commands.
    /// </summary>
    public class IrcCommandProcessor
    {
        private readonly IrcManager _irc;
        private readonly IrcCommandList _ircCmds;
        private readonly Dictionary<string, DateTime> _ircCommandUserTime;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:IRC]";
        private readonly SynServerTool _sst;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IrcCommandProcessor" /> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcCommandProcessor(SynServerTool sst, IrcManager irc)
        {
            _sst = sst;
            _irc = irc;
            _ircCmds = new IrcCommandList(_sst, _irc);
            _ircCommandUserTime = new Dictionary<string, DateTime>();
        }

        /// <summary>
        ///     Gets the commands.
        /// </summary>
        /// <value>
        ///     The Commands.
        /// </value>
        /// <remarks>
        ///     This is used by the IrcHelpCmd.
        /// </remarks>
        public Dictionary<string, IIrcCommand> Cmds
        {
            get { return _ircCmds.Commands; }
        }

        /// <summary>
        ///     Processes the IRC command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="msg">The full message text.</param>
        public async Task ProcessIrcCommand(string fromUser, string msg)
        {
            char[] sep = { ' ' };
            var args = msg.Split(sep);
            var cmdName = args[0].Substring(1);

            if (!CheckCommand(fromUser, cmdName, msg)) return;

            // See if command requires active server monitoring
            var c = new CmdArgs(args, cmdName, fromUser, msg, true);
            if (_ircCmds.Commands[cmdName].RequiresMonitoring &&
                (((!_sst.IsMonitoringServer && !_sst.IsInitComplete))))
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 This command requires that a server be monitored; your server" +
                    " is not currently being monitored.");

                Log.Write(string.Format(
                    "Could not execute IRC user {0}'s IRC command ({1}) because command requires active server" +
                    " monitoring and no server is being monitored at this time.",
                    c.FromUser, cmdName), _logClassType, _logPrefix);

                return;
            }
            // Check argument length
            var minArgs = _ircCmds.Commands[cmdName].IrcMinArgs;
            if (args.Length < minArgs)
            {
                _ircCmds.Commands[cmdName].DisplayArgLengthError(c);

                Log.Write(string.Format(
                    "IRC user {0} specified invalid # of args for {1}{2} command. Required: {3}," +
                    " specified: {4}, received: {5}. Ignoring.",
                    fromUser, IrcCommandList.IrcCommandPrefix, cmdName, minArgs, args.Length, msg),
                    _logClassType, _logPrefix);

                return;
            }
            // Execute
            if (_ircCmds.Commands[cmdName].IsAsync)
            {
                if (await _ircCmds.Commands[cmdName].ExecAsync(c))
                {
                    Log.Write(string.Format("Successfully executed IRC user {0}'s IRC command ({1}): {2}",
                        fromUser, cmdName, msg), _logClassType, _logPrefix);
                }
                else
                {
                    Log.Write(string.Format(
                    "Unsuccessful execution of IRC user {0}'s IRC command ({1}): {2}", fromUser,
                    cmdName, msg), _logClassType, _logPrefix);
                }
            }
            else
            {
                if (_ircCmds.Commands[cmdName].Exec(c))
                {
                    Log.Write(string.Format("Successfully executed IRC user {0}'s IRC command ({1}): {2}",
                        fromUser, cmdName, msg), _logClassType, _logPrefix);
                }
                else
                {
                    Log.Write(string.Format(
                    "Unsuccessful execution of IRC user {0}'s IRC command ({1}): {2}", fromUser,
                    cmdName, msg), _logClassType, _logPrefix);
                }
            }
        }

        /// <summary>
        ///     Determines whether the command can be executed without taking into account
        ///     the command's requirement for server monitoring nor the argument length.
        /// </summary>
        /// <param name="fromUser">The sender of the command..</param>
        /// <param name="commandName">Name of the command.</param>
        /// <param name="fullMessageText">The full message text.</param>
        /// <returns>
        ///     <c>true</c> if the command can be executed, without taking into account
        ///     the command's requirement for server monitoring nor the
        /// required argument length of the command, otherwise <c>false</c>.
        /// </returns>
        private bool CheckCommand(string fromUser, string commandName, string fullMessageText)
        {
            if (!Helpers.KeyExists(commandName, _ircCmds.Commands))
            {
                return false;
            }
            if (!SufficientTimeElapsed(fromUser))
            {
                Log.Write(string.Format(
                    "Sufficient time has not elapsed since {0}'s last command. Ignoring {1}{2} command.",
                    fromUser, IrcCommandList.IrcCommandPrefix, commandName), _logClassType, _logPrefix);

                return false;
            }
            _ircCommandUserTime[fromUser] = DateTime.Now;
            if (fullMessageText.Equals(IrcCommandList.IrcCommandPrefix))
            {
                Log.Write(
                    string.Format(
                        "IRC user {0} entered command prefix {1} without specifying command. Ignoring.",
                        fromUser, IrcCommandList.IrcCommandPrefix), _logClassType, _logPrefix);

                return false;
            }
            if (!UserHasReqLevel(fromUser, _ircCmds.Commands[commandName].UserLevel))
            {
                _irc.SendIrcNotice(fromUser,
                    "\u0002[ERROR]\u0002 You do not have permission to use that command.");

                Log.Write(
                    string.Format(
                        "IRC user {0} sent {1}{2} command but has permission less than {3} needed for {2}. Ingoring.",
                        fromUser, IrcCommandList.IrcCommandPrefix, commandName, Enum.GetName(typeof(IrcUserLevel),
                            _ircCmds.Commands[commandName].UserLevel)), _logClassType, _logPrefix);

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Checks whether sufficient time has elapsed since the user last issued an IRC command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if sufficient time has elapsed, otherwise <c>false</c>.</returns>
        private bool SufficientTimeElapsed(string user)
        {
            if (!Helpers.KeyExists(user, _ircCommandUserTime) ||
                _irc.GetIrcUserLevel(user) >= IrcUserLevel.Voice)
            {
                return true;
            }
            // 3.5 seconds between commands
            return _ircCommandUserTime[user].AddSeconds(3.5) < DateTime.Now;
        }

        /// <summary>
        ///     Checks whether the user has the required IRC access level to issue a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="requiredLevel">The required access level.</param>
        /// <returns><c>true</c> if the user has the required access level, otherwise <c>false</c>.</returns>
        private bool UserHasReqLevel(string user, IrcUserLevel requiredLevel)
        {
            return _irc.GetIrcUserLevel(user) >= requiredLevel;
        }
    }
}