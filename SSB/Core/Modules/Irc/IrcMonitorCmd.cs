using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Modules.Irc
{
    /// <summary>
    /// IRC command: start, stop, or reset QL server monitoring.
    /// </summary>
    public class IrcMonitorCmd : IIrcCommand
    {
        private readonly IrcManager _irc;
        private readonly SynServerBot _ssb;
        private int _ircMinArgs = 2;
        private bool _isAsync = true;
        private bool _requiresMonitoring = false;
        private IrcUserLevel _userLevel = IrcUserLevel.Operator;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcSayCmd"/> class.
        /// </summary>
        /// <param name="ssb">The main bot class.</param>
        /// <param name="irc">The IRC interface.</param>
        public IrcMonitorCmd(SynServerBot ssb, IrcManager irc)
        {
            _ssb = ssb;
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
            _irc.SendIrcNotice(c.FromUser,
                string.Format("\u0002[ERROR]\u0002 The correct usage is: \u0002{0}{1}\u0002 <start|stop|reset>",
                IrcCommandList.IrcCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <remarks>
        /// Not implemented for this command since it is to be run asynchronously via <see cref="ExecAsync"/>
        /// </remarks>
        public void Exec(CmdArgs c)
        {
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!c.Args[1].Equals("start") &&
                !c.Args[1].Equals("stop") && !c.Args[1].Equals("reset") 
                && !c.Args[1].Equals("status"))
            {
                DisplayArgLengthError(c);
                return;
            }
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                _irc.SendIrcNotice(c.FromUser,
                    string.Format("\u0002[ERROR]\u0002 A running instance of Quake Live could not be found."));
                return;
            }
            if (c.Args[1].Equals("start"))
            {
                if (_ssb.IsMonitoringServer)
                {
                    _irc.SendIrcNotice(c.FromUser,
                        string.Format("\u0002[ERROR]\u0002 Your QL server is already being monitored."));
                    return;
                }
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    "\u0002[SUCCESS]\u0002 Starting QL server monitoring.");
                await _ssb.BeginMonitoring();
            }
            else if (c.Args[1].Equals("stop"))
            {
                if (!_ssb.IsMonitoringServer)
                {
                    _irc.SendIrcNotice(c.FromUser,
                        string.Format("\u0002[ERROR]\u0002 No QL server is currently being monitored."));
                }

                _ssb.StopMonitoring();
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                    "\u0002[SUCCESS]\u0002 Stopped monitoring your QL server.");
            }
            else if (c.Args[1].Equals("reset"))
            {
                if (_ssb.IsMonitoringServer)
                {
                    _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                        "\u0002[SUCCESS]\u0002 Your QL server was being monitored; now stopping this monitoring.");
                    _ssb.StopMonitoring();
                }
                else
                {
                    _irc.SendIrcMessage(_irc.IrcSettings.ircChannel,
                        "\u0002[SUCCESS]\u0002 Your QL server was not being monitored; now starting monitoring.");
                    await _ssb.BeginMonitoring();
                }
            }
            else if (c.Args[1].Equals("status"))
            {
                _irc.SendIrcMessage(_irc.IrcSettings.ircChannel, string.Format("SSB {0}",
                    ((_ssb.IsMonitoringServer) ? string.Format(
                    "is monitoring your QL server at \u0002http://www.quakelive.com/#!join/{0}",
                    (string.IsNullOrEmpty(_ssb.ServerInfo.CurrentServerId)
                        ? "..."
                        : _ssb.ServerInfo.CurrentServerId)) : "is \u0002not\u0002 currently monitoring your QL server.")));
            }
        }
    }
}