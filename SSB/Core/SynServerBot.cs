using System;
using System.Diagnostics;
using System.IO;
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
        public double InitDelay = 6.5;
        private Timer _initTimer;
        private bool _isMonitoringServer;
        private volatile bool _isReadingConsole;
        private volatile int _oldLength;
        private Timer _qlProcessDetectionTimer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SynServerBot" /> main class.
        /// </summary>
        public SynServerBot()
        {
            GuiOptions = new GuiOptions();
            AppWideUiControls = new AppWideUiControls();
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
        ///     Gets the app-wide UI controls.
        /// </summary>
        /// <value>
        ///     The app-wide UI controls.
        /// </value>
        public AppWideUiControls AppWideUiControls { get; private set; }

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
        ///     Gets the GUI options.
        /// </summary>
        /// <value>
        ///     The GUI options.
        /// </value>
        public GuiOptions GuiOptions { get; private set; }

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
                AppWideUiControls.UpdateAppWideControls(value,
                    ServerInfo.CurrentServerId);
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
        /// Gets or sets the user interface.
        /// </summary>
        /// <value>
        /// The user interface.
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
                Debug.WriteLine(
                    "Auto server monitoring on start is enabled, but QL window not found. Won't allow.");
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
            StartDelayedInit(InitDelay);
            QlCommands.ClearQlWinConsole();
            Debug.WriteLine("SSB: Requesting server information.");
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
            Debug.WriteLine("SSB: Starting a thread to read QL console.");
            IsReadingConsole = true;
            var readConsoleThread = new Thread(ReadQlConsole) { IsBackground = true };
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
            Debug.WriteLine("SSB: Process detection timer did not exist; enabling.");
        }

        /// <summary>
        ///     Stops the console read thread.
        /// </summary>
        public void StopConsoleReadThread()
        {
            IsReadingConsole = false;
            Debug.WriteLine("SSB: Stopping QL console read thread.");
        }

        /// <summary>
        ///     Stops the monitoring of a server.
        /// </summary>
        public void StopMonitoring()
        {
            Debug.WriteLine("Got request to stop all monitoring and console reading.");
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
            Debug.WriteLine("User has auto monitor on start specified. Attempting to start monitoring.");

            // ReSharper disable once UnusedVariable
            // Synchronous
            var a = AttemptAutoMonitorStart();
        }

        /// <summary>
        ///     Gets the bot's name from the configuration file.
        /// </summary>
        private string GetAccountNameFromConfig()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.VerifyConfigLocation();
            cfgHandler.ReadConfiguration();
            
            return cfgHandler.Config.CoreOptions.accountName;
        }

        /// <summary>
        ///     Method that is executed to finalize the delayed initilization tasks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        private async void InitTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            QlCommands.ClearQlWinConsole();
            // Synchronous
            // ReSharper disable once UnusedVariable
            // Request the configstrings after the current players have already been gathered in order
            // to get an accurate listing of the teams. This will also take care of any players that might have
            // been initially missed by the 'players' command.
            var c = QlCommands.QlCmdConfigStrings();

            Debug.WriteLine("Requesting configstrings in delayed initilization step.");

            // Initiate modules such as MOTD and others that can't be started until after we're live
            Mod.Motd.Init();

            // Wait 2 sec then clear the internal console
            await Task.Delay(2 * 1000);
            QlCommands.ClearQlWinConsole();

            // Initialization is fully complete, we can accept user commands now.
            IsInitComplete = true;
            _initTimer.Enabled = false;
            _initTimer = null;
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
                Debug.WriteLine("SSB: Quake Live not found...Stopping all monitoring and process detection.");
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
                        Debug.WriteLine("[Console text buffer is almost met. AUTOMATICALLY CLEARING]");
                        // Auto-clear
                        QlCommands.ClearQlWinConsole();
                    }
                    _oldLength = textLength;
                }
            }
            else
            {
                Debug.WriteLine("Couldn't find Quake Live console text area");
            }
        }

        /// <summary>
        ///     Starts the delayed initialization steps.
        /// </summary>
        /// <param name="seconds">The number of seconds the timer should wait before executing.</param>
        private void StartDelayedInit(double seconds)
        {
            _initTimer = new Timer(seconds * 1000) { AutoReset = false, Enabled = true };
            _initTimer.Elapsed += InitTimerOnElapsed;
        }
    }
}