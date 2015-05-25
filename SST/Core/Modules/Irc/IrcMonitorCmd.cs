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
    /// IRC command: start, stop, or reset QL server monitoring.
    /// </summary>
    public class IrcMonitorCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly int _ircMinArgs = 2;
        private readonly bool _isAsync = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[IRCCMD:MONITOR]";
        private readonly SynServerTool _sst;
        private readonly IrcUserLevel _userLevel = IrcUserLevel.Operator;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcMonitorCmd"/> class.
        /// </summary>
        /// <param name="sst">The main tool class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcMonitorCmd(SynServerTool sst, IrcManager irc)
        {
            RequiresMonitoring = false;
            _sst = sst;
            _irc = irc;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
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
        /// Gets a value indicating whether this command requires the bot to be monitoring a server
        /// before it can be used.
        /// </summary>
        /// <value>
        /// <c>true</c> if this command requires the bot to be monitoring a server; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresMonitoring { get; private set; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        public IrcUserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public void DisplayArgLengthError(Cmd c)
        {
            _irc.SendIrcNotice(c.FromUser,
                string.Format(
                    "\u0002[ERROR]\u0002 The correct usage is: \u0002{0}{1}\u0002 <start|stop|reset|status>",
                    IrcCommandList.IrcCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise returns <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Not implemented for this command since it is to be run asynchronously via <see cref="ExecAsync"/>
        /// </remarks>
        public bool Exec(Cmd c)
        {
            return true;
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public async Task<bool> ExecAsync(Cmd c)
        {
            if (!c.Args[1].Equals("start") &&
                !c.Args[1].Equals("stop") && !c.Args[1].Equals("reset")
                && !c.Args[1].Equals("status"))
            {
                DisplayArgLengthError(c);
                return false;
            }
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                _irc.SendIrcNotice(c.FromUser,
                    "[ERROR] A running instance of Quake Live could not be found.");

                Log.Write(string.Format(
                    "{0} attempted to use {1} command but a running instance of Quake Live could not be found. Ignoring.",
                    c.FromUser, c.CmdName), _logClassType, _logPrefix);

                return false;
            }
            if (c.Args[1].Equals("start"))
            {
                if (_sst.IsMonitoringServer)
                {
                    _irc.SendIrcNotice(c.FromUser,
                        "[ERROR] Your QL server is already being monitored.");

                    Log.Write(string.Format(
                    "{0} attempted to start server monitoring but server is already being monitored. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                    return false;
                }
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    "\u0002[SUCCESS]\u0002 Attempting to start QL server monitoring.");
                await _sst.BeginMonitoring();
                // Give it time to complete initilization, then show status.
                await Task.Delay(11000);
                ShowMonitorStatus();
            }
            else if (c.Args[1].Equals("stop"))
            {
                if (!_sst.IsMonitoringServer)
                {
                    _irc.SendIrcNotice(c.FromUser,
                        "[ERROR] No QL server is currently being monitored.");

                    Log.Write(string.Format(
                    "{0} attempted to stop server monitoring but server is not currently being monitored. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                    return false;
                }

                _sst.StopMonitoring();
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    "\u0002[SUCCESS]\u0002 Stopped monitoring your QL server.");
            }
            else if (c.Args[1].Equals("reset"))
            {
                if (_sst.IsMonitoringServer)
                {
                    Log.Write(string.Format(
                    "{0} reset server monitoring for actively monitored server; now stopping server monitoring.",
                    c.FromUser), _logClassType, _logPrefix);

                    _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                        "\u0002[SUCCESS]\u0002 Your QL server was being monitored; now stopping this monitoring.");

                    _sst.StopMonitoring();
                }
                else
                {
                    Log.Write(string.Format(
                    "{0} reset server monitoring for non-actively monitored server; now starting server monitoring.",
                    c.FromUser), _logClassType, _logPrefix);

                    _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                        "\u0002[SUCCESS]\u0002 Your QL server was not being monitored; now starting monitoring.");

                    await _sst.BeginMonitoring();
                }
            }
            else if (c.Args[1].Equals("status"))
            {
                ShowMonitorStatus();
            }

            return true;
        }

        /// <summary>
        /// Shows the monitoring status.
        /// </summary>
        private void ShowMonitorStatus()
        {
            _irc.SendIrcMessage(_irc.IrcSettings.ircChannel, string.Format("SST {0}",
                ((_sst.IsMonitoringServer)
                    ? string.Format(
                        "is monitoring your QL server at \u0002{0}",
                        (string.IsNullOrEmpty(_sst.ServerInfo.CurrentServerAddress)
                            ? "..."
                            : _sst.ServerInfo.CurrentServerAddress))
                    : "is \u0002not\u0002 currently monitoring your QL server.")));
        }
    }
}
