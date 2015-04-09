using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using SSB.Config;
using SSB.Core.Commands.Modules;
using SSB.Ui;
using SSB.Util;
using Timer = System.Timers.Timer;

namespace SSB.Core
{
    /// <summary>
    ///     The main class for SSB.
    /// </summary>
    public class SynServerBot
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CORE]";
        private Timer _delayedInitTaskTimer;
        private bool _isMonitoringServer;
        private volatile bool _isReadingConsole;
        private volatile int _oldLength;
        private Timer _qlProcessDetectionTimer;
        public double InitDelay = 6.5;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SynServerBot" /> main class.
        /// </summary>
        /// <remarks>
        ///     Some notes:
        ///     All of the core functions of the bot including console text processing, player/server
        ///     event processing, modules, command processing, vote management, parsing, etc. are initialized in
        ///     this constructor and set as properties in this main class. The bot only allows one instance of itself
        ///     for the explicit reason that Quake Live can only have one running copy open at a time.
        ///     For this reason, the initilizated <see cref="SynServerBot" /> object is frequently passed around
        ///     the rest of the code almost entirely through constructor dependency injection and the properties are
        ///     directly accessed rather than constantly instantiating new classes, which also explains many of the public
        ///     methods. Once intilizated, the bot will then call the <see cref="CheckForAutoMonitoring" /> method which
        ///     reads the configuration to see if the user has specified whether server monitoring should begin on application
        ///     start. If Quake Live is running, we will check to see if the client is connected to a server. If connected, we will
        ///     retrieve the server information and players using built in QL commands. After that, we will start a timer that
        ///     waits for ~6.5s to perform any final initlization tasks to make sure all necessary information is present. 
        ///     This project initially started as a VERY simple proof of concept and expanded dramatically from there, so refactoring
        ///     in various places is almost certainly in order.
        /// </remarks>
        public SynServerBot()
        {
            // Core
            ServerInfo = new ServerInfo();
            QlCommands = new QlCommands(this);
            Parser = new Parser();
            QlWindowUtils = new QlWindowUtils();
            ConsoleTextProcessor = new ConsoleTextProcessor(this);
            ServerEventProcessor = new ServerEventProcessor(this);
            VoteManager = new VoteManager();

            //Set the name of the bot
            AccountName = GetAccountNameFromConfig();
            // Hook up modules
            Mod = new ModuleManager(this);
            // Hook up command listener
            CommandProcessor = new CommandProcessor(this);

            // Check if we should begin monitoring a server immediately
            CheckForAutoMonitoring();
        }

        /// <summary>
        ///     Gets or sets the name of the account that is running the bot.
        /// </summary>
        /// <value>
        ///     The name of the account that is running the bot.
        /// </value>
        public string AccountName { get; set; }

        /// <summary>
        ///     Gets the command processor.
        /// </summary>
        /// <value>
        ///     The command processor.
        /// </value>
        public CommandProcessor CommandProcessor { get; private set; }

        /// <summary>
        ///     Gets the console text processor.
        /// </summary>
        /// <value>
        ///     The console text processor.
        /// </value>
        public ConsoleTextProcessor ConsoleTextProcessor { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether a server disconnection scan is pending.
        /// </summary>
        /// <value>
        ///     <c>true</c> a server disconnection scan is pending; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisconnectionScanPending { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether initialization has completed.
        /// </summary>
        /// <value>
        ///     <c>true</c> if initialization has completed; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitComplete { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether SSB is currently monitoring a QL server.
        /// </summary>
        /// <value>
        ///     <c>true</c> if SSB is monitoring a QL server; otherwise, <c>false</c>.
        /// </value>
        public bool IsMonitoringServer
        {
            get { return _isMonitoringServer; }
            set
            {
                _isMonitoringServer = value;
                // UI
                UserInterface.UpdateMonitoringStatusUi(value,
                    ServerInfo.CurrentServerAddress);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is reading the console.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is reading console; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadingConsole
        {
            get { return _isReadingConsole; }
            set { _isReadingConsole = value; }
        }

        /// <summary>
        ///     Gets the module manager.
        /// </summary>
        /// <value>
        ///     The module manager.
        /// </value>
        public ModuleManager Mod { get; private set; }

        /// <summary>
        ///     Gets the Parser.
        /// </summary>
        /// <value>
        ///     The text parser.
        /// </value>
        public Parser Parser { get; private set; }

        /// <summary>
        ///     Gets the QlCommands.
        /// </summary>
        /// <value>
        ///     The QlCommands.
        /// </value>
        public QlCommands QlCommands { get; private set; }

        /// <summary>
        ///     Gets the QlWindowUtils
        /// </summary>
        /// <value>
        ///     The QL window utils.
        /// </value>
        public QlWindowUtils QlWindowUtils { get; private set; }

        /// <summary>
        ///     Gets the server event processor.
        /// </summary>
        /// <value>
        ///     The server event processor.
        /// </value>
        public ServerEventProcessor ServerEventProcessor { get; private set; }

        /// <summary>
        ///     Gets the server information.
        /// </summary>
        /// <value>
        ///     The server information.
        /// </value>
        public ServerInfo ServerInfo { get; private set; }

        /// <summary>
        ///     Gets or sets the user interface.
        /// </summary>
        /// <value>
        ///     The user interface.
        /// </value>
        public UserInterface UserInterface { get; set; }

        /// <summary>
        ///     Gets the vote manager.
        /// </summary>
        /// <value>
        ///     The vote manager.
        /// </value>
        public VoteManager VoteManager { get; private set; }

        /// <summary>
        ///     Attempts to automatically start server monitoring on application launch,
        ///     if the user has this option specified in the SSB configuration file.
        /// </summary>
        public async Task AttemptAutoMonitorStart()
        {
            var qlw = new QlWindowUtils();
            if (!qlw.QuakeLiveConsoleWindowExists())
            {
                Log.Write(
                    "User has enabled 'auto server monitoring on program start', but QL instance not found. Won't allow.",
                    _logClassType, _logPrefix);

                MessageBox.Show(
                    @"Could not auto-start server monitoring because a running instance of Quake Live was not found!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            await BeginMonitoring();
        }

        /// <summary>
        ///     Attempt to start monitoring the server, per the user's request.
        /// </summary>
        public async Task BeginMonitoring()
        {
            IsInitComplete = false;
            // We might've been previously monitoring without restarting the application,
            // so also reset any server information.
            ServerInfo.Reset();
            // Start timer to continuously detect if QL process is running
            StartProcessDetectionTimer();
            // Start reading the console
            StartConsoleReadThread();

            // Hide console text if user has option enabled
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            if (cfgHandler.Config.CoreOptions.hideAllQlConsoleText)
            {
                QlCommands.DisableConsolePrinting();
            }

            // Are we connected?
            await CheckQlServerConnectionExists();
            if (!ServerInfo.IsQlConnectedToServer)
            {
                MessageBox.Show(
                    @"Could not detect connection to a Quake Live server, monitoring cannot begin!",
                    @"Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Get player listing and perform initilization tasks
            GetServerInformation();

            // We're live
            IsMonitoringServer = true;
        }

        /// <summary>
        ///     Sends commands to Quake Live to verify that a server connection exists.
        /// </summary>
        public async Task CheckQlServerConnectionExists()
        {
            QlCommands.ClearQlWinConsole();
            await QlCommands.CheckMainMenuStatus();
            await QlCommands.CheckCmdStatus();
            QlCommands.QlCmdClear();
        }

        /// <summary>
        ///     Gets the server address.
        /// </summary>
        public void CheckServerAddress()
        {
            QlCommands.RequestServerAddress();
            QlCommands.QlCmdClear();
        }

        /// <summary>
        ///     Gets the server information.
        /// </summary>
        public void GetServerInformation()
        {
            // First and foremost, clear the console and get the player listing.
            QlCommands.ClearQlWinConsole();
            // Re-focus the window
            Win32Api.SwitchToThisWindow(QlWindowUtils.QlWindowHandle, true);
            // Disable developer mode if it's already set, so we can get accurate player listing.
            QlCommands.DisableDeveloperMode();
            // Initially get the player listing when we start. Synchronous since initilization.
            // ReSharper disable once UnusedVariable
            var q = QlCommands.QlCmdPlayers();
            // Get the server's id
            QlCommands.SendToQl("serverinfo", true);
            // Enable developer mode
            QlCommands.EnableDeveloperMode();
            // Delay some initilization tasks and complete initilization
            StartDelayedInitTasks(InitDelay);
            QlCommands.ClearQlWinConsole();
            Log.Write("Requesting server information.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Reloads the initialization step.
        /// </summary>
        /// <remarks>
        ///     This is primarily designed to be accessed via an admin command from QL.
        /// </remarks>
        public void ReloadInit()
        {
            IsInitComplete = false;
            GetServerInformation();
            QlCommands.ClearQlWinConsole();
        }

        /// <summary>
        ///     Starts the console read thread.
        /// </summary>
        public void StartConsoleReadThread()
        {
            if (IsReadingConsole) return;
            Log.Write("Starting QL console read thread.", _logClassType, _logPrefix);
            IsReadingConsole = true;
            var readConsoleThread = new Thread(ReadQlConsole) {IsBackground = true};
            readConsoleThread.Start();
        }

        /// <summary>
        ///     Hook up the process up the detection timer.
        /// </summary>
        public void StartProcessDetectionTimer()
        {
            if (_qlProcessDetectionTimer != null) return;
            _qlProcessDetectionTimer = new Timer(15000);
            _qlProcessDetectionTimer.Elapsed += QlProcessDetectionTimerOnElapsed;
            _qlProcessDetectionTimer.Enabled = true;
            Log.Write("Process detection timer did not exist; enabling.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Stops the console read thread.
        /// </summary>
        public void StopConsoleReadThread()
        {
            IsReadingConsole = false;
            Log.Write("Terminating QL console read thread.", _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Stops the monitoring of a server.
        /// </summary>
        public void StopMonitoring()
        {
            Log.Write("Got request to stop all monitoring and console reading.", _logClassType, _logPrefix);
            IsMonitoringServer = false;
            StopConsoleReadThread();
            ServerInfo.IsQlConnectedToServer = false;
            ServerInfo.Reset();
        }

        /// <summary>
        ///     Checks the user's configuration to see if automatic server monitoring
        ///     should occur on application launch, and attempts to automatically monitor
        ///     the server if possible.
        /// </summary>
        private void CheckForAutoMonitoring()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            if (!cfgHandler.Config.CoreOptions.autoMonitorServerOnStart) return;

            Log.Write(
                "User has 'auto monitor on start' specified. Attempting to start monitoring if possible.",
                _logClassType, _logPrefix);

            // ReSharper disable once UnusedVariable
            // Synchronous
            var a = AttemptAutoMonitorStart();
        }

        /// <summary>
        ///     Method that is executed to finalize the delayed initilization tasks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private async void DelayedInitTaskTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            QlCommands.ClearQlWinConsole();
            // Synchronous
            // ReSharper disable once UnusedVariable
            // Request the configstrings after the current players have already been gathered in order
            // to get an accurate listing of the teams. This will also take care of any players that might have
            // been initially missed by the 'players' command.
            var c = QlCommands.QlCmdConfigStrings();

            Log.Write("Performing delayed initilization tasks.", _logClassType, _logPrefix);

            // Initiate modules such as MOTD and others that can't be started until after we're live
            Mod.Motd.Init();

            // Get IP
            CheckServerAddress();

            // Wait 2 sec then clear the internal console
            await Task.Delay(2*1000);
            QlCommands.ClearQlWinConsole();

            // Update UI status bar with IP
            UserInterface.UpdateMonitoringStatusUi(true, ServerInfo.CurrentServerAddress);

            // Initialization is fully complete, we can accept user commands now.
            IsInitComplete = true;
            _delayedInitTaskTimer.Enabled = false;
            _delayedInitTaskTimer = null;
        }

        /// <summary>
        ///     Gets the bot's name from the configuration file.
        /// </summary>
        private string GetAccountNameFromConfig()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();

            return cfgHandler.Config.CoreOptions.accountName;
        }

        /// <summary>
        ///     Method that runs when the QL Process Detection Timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private void QlProcessDetectionTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var qlWindExists = QlWindowUtils.QlWindowHandle != IntPtr.Zero;
            // Quake Live not found
            if (!qlWindExists)
            {
                Log.Write(
                    "Instance of Quake Live no longer found. Will terminate all server monitoring and process detection.",
                    _logClassType, _logPrefix);

                StopMonitoring();
                _qlProcessDetectionTimer.Enabled = false;
                _qlProcessDetectionTimer = null;
            }
        }

        /// <summary>
        ///     Reads the QL console window.
        /// </summary>
        private void ReadQlConsole()
        {
            var consoleWindow = QlWindowUtils.GetQuakeLiveConsoleWindow();
            var cText = QlWindowUtils.GetQuakeLiveConsoleTextArea(consoleWindow,
                QlWindowUtils.GetQuakeLiveConsoleInputArea(consoleWindow));
            if (cText != IntPtr.Zero)
            {
                while (IsReadingConsole)
                {
                    var textLength = Win32Api.SendMessage(cText, Win32Api.WM_GETTEXTLENGTH, IntPtr.Zero,
                        IntPtr.Zero);
                    if ((textLength == 0) || (ConsoleTextProcessor.OldWholeConsoleLineLength == textLength))
                        continue;

                    // Entire console window text
                    var entireBuffer = new StringBuilder(textLength + 1);
                    Win32Api.SendMessage(cText, Win32Api.WM_GETTEXT, new IntPtr(textLength + 1), entireBuffer);
                    var received = entireBuffer.ToString();
                    ConsoleTextProcessor.ProcessEntireConsoleText(received, textLength);

                    var lengthDifference = Math.Abs(textLength - _oldLength);

                    if (received.Length > lengthDifference)
                    {
                        // Bounds checking
                        int start;
                        int length;

                        if (_oldLength > received.Length)
                        {
                            start = 0;
                            length = received.Length;
                        }
                        else
                        {
                            start = _oldLength;
                            length = lengthDifference;
                        }

                        // Standardize QL's annoying string formatting
                        var diffBuilder = new StringBuilder(received.Substring(start, length));
                        diffBuilder.Replace("\"\r\n\r\n", "\"\r\n");
                        diffBuilder.Replace("\r\n\"\r\n", "\r\n");
                        diffBuilder.Replace("\r\n\r\n", "\r\n");
                        ConsoleTextProcessor.ProcessShortConsoleLines(diffBuilder.ToString());
                    }

                    // Detect when buffer is about to be full, in order to auto-clear.
                    // Win Edit controls can have a max of 30,000 characters, see:
                    // "Limits of Edit Controls" - http://msdn.microsoft.com/en-us/library/ms997530.aspx
                    // More info: Q3 source (win_syscon.c), Conbuf_AppendText
                    int begin, end;
                    Win32Api.SendMessage(cText, Win32Api.EM_GETSEL, out begin, out end);
                    if ((begin >= 29300) && (end >= 29300))
                    {
                        Log.Write("Console buffer is almost full. Automatically clearing.",
                            _logClassType, _logPrefix);

                        // Auto-clear
                        QlCommands.ClearQlWinConsole();
                    }
                    _oldLength = textLength;
                }
            }
            else
            {
                Log.WriteCritical("Unable to get necessary console handle.", _logClassType, _logPrefix);
            }
        }

        /// <summary>
        ///     Starts the delayed initialization steps.
        /// </summary>
        /// <param name="seconds">The number of seconds the timer should wait before executing.</param>
        private void StartDelayedInitTasks(double seconds)
        {
            _delayedInitTaskTimer = new Timer(seconds*1000) {AutoReset = false, Enabled = true};
            _delayedInitTaskTimer.Elapsed += DelayedInitTaskTimerOnElapsed;
        }
    }
}