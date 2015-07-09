namespace SST.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using SST.Config;
    using SST.Core.Commands.Modules;
    using SST.Ui;
    using SST.Util;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// The main class for SST.
    /// </summary>
    public class SynServerTool
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
        /// Initializes a new instance of the <see cref="SynServerTool" /> main class.
        /// </summary>
        /// <param name="isRestart">if set to <c>true</c> then the application was
        /// launched for the process of restarting previous monitoring.</param>
        /// <remarks>
        /// Some notes/thoughts: All of the core functions of the bot including console text
        /// processing, player/server event processing, modules, command processing, vote
        /// management, parsing, etc. are initialized in this constructor and set as properties in
        /// this main class. The bot only allows one instance of itself for the explicit reason that
        /// Quake Live can only have one running copy open at a time. For this reason, this
        /// initilizated <see cref="SynServerTool" /> object is frequently passed around the rest of
        /// the code almost entirely through constructor injection and the properties are directly
        /// accessed rather than constantly instantiating new classes. In this application, access
        /// to state among most parts is crucial, and unfortunately that leads to some unavoidable
        /// tight coupling. Once intilizated, the bot will then call the
        /// <see cref="CheckForAutoMonitoring" /> method which reads the configuration to see if the
        /// user has specified whether server monitoring should begin on application start. If Quake
        /// Live is running, we will check to see if the client is connected to a server. If
        /// connected, we will retrieve the server information and players using built in QL
        /// commands. After that, we will start a timer that waits for ~6.5s to perform any final
        /// initlization tasks to make sure all necessary information is present. This project
        /// initially started as a VERY simple proof of concept and expanded dramatically from
        /// there, so refactoring in various places is almost certainly in order. For example, a
        /// user interface was not initially planned (the tool was going to only be command-driven
        /// in-game), but was later added during development for ease of use.
        /// </remarks>
        public SynServerTool(bool isRestart)
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

            // If being launched as restart then automatically try to start monitoring and skip the check.
            if (isRestart)
            {
                // ReSharper disable once UnusedVariable (synchronous)
                var a = AttemptAutoMonitorStart();
            }
            else
            {
                // Otherwise, check if we should begin monitoring a server immediately per user's settings.
                CheckForAutoMonitoring();
            }
        }

        /// <summary>
        /// Gets or sets the name of the account that is running the bot.
        /// </summary>
        /// <value>The name of the account that is running the bot.</value>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets the command processor.
        /// </summary>
        /// <value>The command processor.</value>
        public CommandProcessor CommandProcessor { get; private set; }

        /// <summary>
        /// Gets the console text processor.
        /// </summary>
        /// <value>The console text processor.</value>
        public ConsoleTextProcessor ConsoleTextProcessor { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a server disconnection scan is pending.
        /// </summary>
        /// <value><c>true</c> a server disconnection scan is pending; otherwise, <c>false</c>.</value>
        public bool IsDisconnectionScanPending { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether initialization has completed.
        /// </summary>
        /// <value><c>true</c> if initialization has completed; otherwise, <c>false</c>.</value>
        public bool IsInitComplete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SST is currently monitoring a QL server.
        /// </summary>
        /// <value><c>true</c> if SST is monitoring a QL server; otherwise, <c>false</c>.</value>
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
        /// Gets or sets a value indicating whether this instance is reading the console.
        /// </summary>
        /// <value><c>true</c> if this instance is reading console; otherwise, <c>false</c>.</value>
        public bool IsReadingConsole
        {
            get { return _isReadingConsole; }
            set { _isReadingConsole = value; }
        }

        /// <summary>
        /// Gets the module manager.
        /// </summary>
        /// <value>The module manager.</value>
        public ModuleManager Mod { get; private set; }

        /// <summary>
        /// Gets the Parser.
        /// </summary>
        /// <value>The text parser.</value>
        public Parser Parser { get; private set; }

        /// <summary>
        /// Gets the QlCommands.
        /// </summary>
        /// <value>The QlCommands.</value>
        public QlCommands QlCommands { get; private set; }

        /// <summary>
        /// Gets the QlWindowUtils
        /// </summary>
        /// <value>The QL window utils.</value>
        public QlWindowUtils QlWindowUtils { get; private set; }

        /// <summary>
        /// Gets the server event processor.
        /// </summary>
        /// <value>The server event processor.</value>
        public ServerEventProcessor ServerEventProcessor { get; private set; }

        /// <summary>
        /// Gets the server information.
        /// </summary>
        /// <value>The server information.</value>
        public ServerInfo ServerInfo { get; private set; }

        public DateTime MonitoringStartedTime { get; private set; }
        
        /// <summary>
        /// Gets or sets the user interface.
        /// </summary>
        /// <value>The user interface.</value>
        public UserInterface UserInterface { get; set; }

        /// <summary>
        /// Gets the vote manager.
        /// </summary>
        /// <value>The vote manager.</value>
        public VoteManager VoteManager { get; private set; }

        /// <summary>
        /// Attempts to automatically start server monitoring on application launch, if the user has
        /// this option specified in the SST configuration file.
        /// </summary>
        public async Task AttemptAutoMonitorStart()
        {
            if (!QlWindowUtils.QuakeLiveConsoleWindowExists())
            {
                Log.Write(
                    "Attempt to auto-start server monitoring on program start failed: QL instance not found.",
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
        /// Attempt to start monitoring the server, per the user's request.
        /// </summary>
        public async Task BeginMonitoring()
        {
            IsInitComplete = false;
            // We might've been previously monitoring without restarting the application, so also
            // reset any server information.
            ServerInfo.Reset();
            // Start timer to continuously detect if QL process is running
            StartProcessDetectionTimer();
            // Start reading the console
            StartConsoleReadThread();

            // Hide console text if user has option enabled
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();
            if (cfg.CoreOptions.hideAllQlConsoleText)
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
        /// Sends commands to Quake Live to verify that a server connection exists.
        /// </summary>
        public async Task CheckQlServerConnectionExists()
        {
            QlCommands.ClearQlWinConsole();
            await QlCommands.CheckMainMenuStatus();
            await QlCommands.CheckCmdStatus();
            QlCommands.QlCmdClear();
        }

        /// <summary>
        /// Handles the situation where the user disables developer mode while the server is being monitored.
        /// </summary>
        public void HandleDevModeDisabled()
        {
            StopMonitoring();
            MessageBox.Show(
                @"SST requires developer mode to be enabled! Now stopping monitoring.",
                @"Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Calls the restarter executable to re-launch SST with the restart parameter.
        /// </summary>
        public void RestartSst()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "RestartSST.exe" });
            }
            catch (Exception ex)
            {
                Log.WriteCritical(string.Format("Error launching SST process restarter: {0}",
                    ex), _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Stops the monitoring of a server.
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
        /// Terminates on zmalloc crash.
        /// </summary>
        public void TerminateOnZmallocCrash()
        {
            StopMonitoring();

            // If IRC connection exists then notify the channel and admin that QL has crashed
            if (Mod.Irc.IsConnectedToIrc)
            {
                Mod.Irc.IrcManager.SendIrcMessage(Mod.Irc.IrcManager.IrcSettings.ircChannel,
                    "\u0003\u0002[ALERT]\u0002 Quake Live has crashed due to the Z_Malloc memory allocation error.");

                Mod.Irc.IrcManager.SendIrcMessage(Mod.Irc.IrcManager.IrcSettings.ircAdminNickname,
                    "\u0003\u0002[ALERT]\u0002 Quake Live has crashed due to the Z_Malloc memory allocation error.");
            }

            MessageBox.Show(
                @"Quake Live has crashed due to the memory allocation bug which happens after your server has been running for too long.
                " +
                @" Click 'OK' to terminate.",
                @"Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            var procs = Process.GetProcesses();
            foreach (var proc in procs.Where(proc =>
                proc.ProcessName.Equals("quakelive", StringComparison.InvariantCultureIgnoreCase) ||
                proc.ProcessName.Equals("quakelive_steam", StringComparison.InvariantCultureIgnoreCase)))
            {
                proc.Kill();
            }
            Application.Exit();
        }

        /// <summary>
        /// Gets the server address.
        /// </summary>
        private void CheckServerAddress()
        {
            QlCommands.RequestServerAddress();
            QlCommands.QlCmdClear();
        }

        /// <summary>
        /// Starts the console read thread.
        /// </summary>
        private void StartConsoleReadThread()
        {
            if (IsReadingConsole)
            {
                return;
            }
            Log.Write("Starting QL console read thread.", _logClassType, _logPrefix);
            IsReadingConsole = true;
            var readConsoleThread = new Thread(ReadQlConsole) { IsBackground = true };
            readConsoleThread.Start();
        }

        /// <summary>
        /// Hook up the process up the detection timer.
        /// </summary>
        private void StartProcessDetectionTimer()
        {
            if (_qlProcessDetectionTimer != null)
            {
                return;
            }
            _qlProcessDetectionTimer = new Timer(15000);
            _qlProcessDetectionTimer.Elapsed += (sender, args) =>
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
            };
            _qlProcessDetectionTimer.Enabled = true;
            Log.Write("Process detection timer did not exist; enabling.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Stops the console read thread.
        /// </summary>
        private void StopConsoleReadThread()
        {
            IsReadingConsole = false;
            Log.Write("Terminating QL console read thread.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Checks the user's configuration to see if automatic server monitoring should occur on
        /// application launch, and attempts to automatically monitor the server if possible.
        /// </summary>
        private void CheckForAutoMonitoring()
        {
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();
            if (!cfg.CoreOptions.autoMonitorServerOnStart)
            {
                return;
            }

            Log.Write(
                "User has 'auto monitor on start' specified. Attempting to start monitoring if possible.",
                _logClassType, _logPrefix);

            // ReSharper disable once UnusedVariable (synchronous)
            var a = AttemptAutoMonitorStart();
        }

        /// <summary>
        /// Gets the bot's name from the configuration file.
        /// </summary>
        private string GetAccountNameFromConfig()
        {
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();

            return cfg.CoreOptions.accountName;
        }

        /// <summary>
        /// Gets the server information.
        /// </summary>
        private void GetServerInformation()
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
            PerformDelayedInitTasks(InitDelay);

            Log.Write("Requesting server information.", _logClassType, _logPrefix);
        }

        /// <summary>
        /// Reads the QL console window.
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
                    {
                        continue;
                    }

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

                    // Detect when buffer is about to be full, in order to auto-clear. Win Edit
                    // controls can have a max of 30,000 characters, see: "Limits of Edit Controls"
                    // - http://msdn.microsoft.com/en-us/library/ms997530.aspx More info: Q3 source
                    // (win_syscon.c), Conbuf_AppendText
                    int begin, end;
                    Win32Api.SendMessage(cText, Win32Api.EM_GETSEL, out begin, out end);
                    if ((begin >= 29300) && (end >= 29300))
                    {
                        Log.Write("Clearing nearly full conbuf.",
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
        /// Performs the delayed initialization steps.
        /// </summary>
        /// <param name="secondsToWait">The number of seconds the timer should wait before executing.</param>
        private void PerformDelayedInitTasks(double secondsToWait)
        {
            _delayedInitTaskTimer = new Timer(secondsToWait * 1000) { AutoReset = false, Enabled = true };

            // Finalize the delayed initilization tasks
            _delayedInitTaskTimer.Elapsed += async (sender, args) =>
            {
                Log.Write("Performing delayed initilization tasks.", _logClassType, _logPrefix);

                QlCommands.ClearQlWinConsole();

                // Initiate modules such as MOTD and others that can't be started until after we're live
                Mod.Motd.Init();

                // Get IP
                CheckServerAddress();

                // Send configstrings request in order to get an accurate listing of the teams.
                // Strangely, this appears to not register w/ QL at various times, so send it a few
                // different times.
                for (var i = 1; i < 4; i++)
                {
                    await QlCommands.SendToQlDelayedAsync("configstrings", true, (i * 3));
                }

                // Update UI status bar with IP
                await Task.Delay(2000);
                UserInterface.UpdateMonitoringStatusUi(true, ServerInfo.CurrentServerAddress);
                QlCommands.QlCmdClear();

                // Done
                QlCommands.ClearBothQlConsoles();
                QlCommands.SendToQl("echo ^4***^5SST is now ^2LOADED^4***", false);
                QlCommands.SendToQl("print ^4***^5SST is now ^2LOADED^4***", false);
                Log.Write("SST is now loaded on the server.", _logClassType, _logPrefix);
                IsInitComplete = true;
                MonitoringStartedTime = DateTime.Now;
                _delayedInitTaskTimer.Enabled = false;
                _delayedInitTaskTimer = null;
            };
        }
    }
}
