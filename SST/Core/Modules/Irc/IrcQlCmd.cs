using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Modules.Irc
{
    /// <summary>
    /// IRC Command: Allow execution of various SST QL in-game commands via IRC.
    /// Serves as an interface for commands that would typically be executed in-game,
    /// but allow very highly privileged users to issue such commands from the IRC channel.
    /// </summary>
    public class IrcQlCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[IRCCMD:QL]";
        private readonly IrcUserLevel _userLevel = IrcUserLevel.Operator;
        private Dictionary<string, IBotCommand> _cmdList;
        private int _ircMinArgs = 2;
        private bool _isAsync = true;
        private bool _requiresMonitoring = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcQlCmd" /> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcQlCmd(SynServerTool sst, IrcManager irc)
        {
            _irc = irc;
            var cmds = new CommandList(sst);
            _cmdList = cmds.Commands;
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
            _irc.SendIrcNotice(c.FromUser, string.Format("\u0002[ERROR]\u0002 The correct usage is: \u0002{0}{1}\u0002" +
                                                         " <sst command> <sst command args>",
                IrcCommandList.IrcCommandPrefix, c.CmdName));
            _irc.SendIrcNotice(c.FromUser, string.Format("<sst command> is a cmd you'd normally issue in-game," +
                                                         " and <sst cmd args> are its arguments, if required, i.e: {0}{1} {2} syncore",
                IrcCommandList.IrcCommandPrefix, c.CmdName, CommandList.CmdSeen));
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
        ///     Not implemented for this command since it is to be run asynchronously
        ///  via <see cref="ExecAsync" />
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
            if (!Helpers.KeyExists(c.Args[1], _cmdList))
            {
                _irc.SendIrcNotice(c.FromUser, "\u0002[ERROR]\u0002 That is not valid command.");

                Log.Write(string.Format("{0} attempted to use IRC to QL interface but specified non-existent SST command ({1}) Ignoring.",
                    c.FromUser, c.Args[1]), _logClassType, _logPrefix);

                return false;
            }
            if (!_cmdList[c.Args[1]].IsIrcAccessAllowed)
            {
                _irc.SendIrcNotice(c.FromUser, "\u0002[ERROR]\u0002 That command can only be accessed from in-game.");

                Log.Write(string.Format(
                    "{0} attempted to use IRC to QL interface but specified SST command ({1}) that can only be issued from in-game Ignoring.",
                    c.FromUser, c.Args[1]), _logClassType, _logPrefix);

                return false;
            }
            // See if the c passed here meets the actual IBotCommand's MinArgs
            if ((c.Args.Length) < _cmdList[c.Args[1]].IrcMinArgs)
            {
                _irc.SendIrcNotice(c.FromUser,
                    ReplaceQlColorsWithIrcColors(_cmdList[c.Args[1]].GetArgLengthErrorMessage(c)));

                Log.Write(string.Format(
                    "{0} attempted to use IRC to QL interface but specified too few parameters for SST command ({1}) Ignoring.",
                   c.FromUser, c.Args[1]), _logClassType, _logPrefix);

                return false;
            }

            var success = await _cmdList[c.Args[1]].ExecAsync(c);
            if (success)
            {
                // Some commands send multiple lines; IRC messages cannot contain \r, \n, etc.
                if (_cmdList[c.Args[1]].StatusMessage.Contains(Environment.NewLine))
                {
                    SendSplitMessage(c, _cmdList[c.Args[1]].StatusMessage.Split(new[] { Environment.NewLine },
                         StringSplitOptions.RemoveEmptyEntries), true);
                }
                else
                {
                    _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    ReplaceQlColorsWithIrcColors(_cmdList[c.Args[1]].StatusMessage));
                }

                Log.Write(string.Format("Successfully executed {0}'s {1} command using IRC to QL interface",
                    c.FromUser, c.Args[1]), _logClassType, _logPrefix);
            }
            else
            {
                if (_cmdList[c.Args[1]].StatusMessage.Contains(Environment.NewLine))
                {
                    SendSplitMessage(c, _cmdList[c.Args[1]].StatusMessage.Split(new[] { Environment.NewLine },
                        StringSplitOptions.RemoveEmptyEntries), false);
                }
                else
                {
                    _irc.SendIrcNotice(c.FromUser,
                    ReplaceQlColorsWithIrcColors(_cmdList[c.Args[1]].StatusMessage));
                }

                Log.Write(string.Format("Unsuccessfully execution of {0}'s {1} command using IRC to QL interface",
                    c.FromUser, c.Args[1]), _logClassType, _logPrefix);
            }

            return true;
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

        /// <summary>
        /// Handles the sending of messages containing newline characters.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="msg">The array containing the messages, split by a newline character.
        /// </param>
        /// <param name="toChannel">if set to <c>true</c> then send the message to the IRC
        /// channel, otherwise send the message as an IRC notice to the user.</param>
        private void SendSplitMessage(CmdArgs c, string[] msg, bool toChannel)
        {
            foreach (var m in msg)
            {
                if (toChannel)
                {
                    _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                        ReplaceQlColorsWithIrcColors(m));
                }
                else
                {
                    _irc.SendIrcNotice(c.FromUser, ReplaceQlColorsWithIrcColors(m));
                }
            }
        }
    }
}