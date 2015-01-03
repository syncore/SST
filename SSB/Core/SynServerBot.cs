using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
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
        private Timer _initTimer;
        private volatile bool _isReadingConsole;
        private volatile int _oldLength;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SynServerBot" /> main class.
        /// </summary>
        public SynServerBot()
        {
            GuiOptions = new GuiOptions();
            GuiControls = new GuiControls();
            ServerInfo = new ServerInfo();
            QlCommands = new QlCommands(this);
            Parser = new Parser();
            QlWindowUtils = new QlWindowUtils();
            ConsoleTextProcessor = new ConsoleTextProcessor(this);
            ServerEventProcessor = new ServerEventProcessor(this);
            VoteManager = new VoteManager();

            // Start reading the console
            StartConsoleReadThread();
            // Set the important details of the server
            InitServerInformation();
            // Hook up modules
            Mod = new ModuleManager(this);
            // Hook up command listener
            CommandProcessor = new CommandProcessor(this);
            // Get name of account running the bot
            QlCommands.ClearQlWinConsole();
            // Delay some initilization tasks and complete initilization
            StartDelayedInit(6.5);
        }

        /// <summary>
        ///     Gets or sets the name of the account that is running the bot.
        /// </summary>
        /// <value>
        ///     The name of the account that is running the bot.
        /// </value>
        public string BotName { get; set; }

        /// <summary>
        /// Gets the command processor.
        /// </summary>
        /// <value>
        /// The command processor.
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
        ///     Gets the GUI controls.
        /// </summary>
        /// <value>
        ///     The GUI controls.
        /// </value>
        public GuiControls GuiControls { get; private set; }

        /// <summary>
        ///     Gets the GUI options.
        /// </summary>
        /// <value>
        ///     The GUI options.
        /// </value>
        public GuiOptions GuiOptions { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether initialization has completed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialization has completed; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitComplete { get; set; }

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
        /// Gets the module manager.
        /// </summary>
        /// <value>
        /// The module manager.
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
        /// Gets the vote manager.
        /// </summary>
        /// <value>
        /// The vote manager.
        /// </value>
        public VoteManager VoteManager { get; private set; }

        /// <summary>
        ///     Starts the console read thread.
        /// </summary>
        public void StartConsoleReadThread()
        {
            if (IsReadingConsole) return;
            Debug.WriteLine("...starting a thread to read QL console.");
            IsReadingConsole = true;
            var readConsoleThread = new Thread(ReadQlConsole) { IsBackground = true };
            readConsoleThread.Start();
        }

        /// <summary>
        /// Stops the console read thread.
        /// </summary>
        public void StopConsoleReadThread()
        {
            IsReadingConsole = false;
            Debug.WriteLine("...stopping QL console read thread.");
            MessageBox.Show(@"Stopped reading Quake Live events, because Quake Live is not detected.",
                @"Stopped reading events", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// Get name of account running the bot.
        /// </summary>
        public void RetrieveBotAccount()
        {
            QlCommands.SendToQl("name", false);
        }

        /// <summary>
        ///     Initializes the server information.
        /// </summary>
        private void InitServerInformation()
        {
            // First and foremost, clear the console and get the player listing.
            QlCommands.ClearQlWinConsole();
            // Re-focus the window
            Win32Api.SwitchToThisWindow(QlWindowUtils.QlWindowHandle, true);
            // Disable developer mode if it's already set, so we can get accurate player listing.
            QlCommands.DisableDeveloperMode();
            // Initially get the player listing when we start. Synchronous since initilization.
            // ReSharper disable once UnusedVariable
            Task q = QlCommands.QlCmdPlayers();
            
            //QlCommands.SendToQl("name", false);
            // Get the server's id
            QlCommands.SendToQl("serverinfo", true);
            // Enable developer mode
            QlCommands.EnableDeveloperMode();
        }

        /// <summary>
        /// Method that is executed to finalize the delayed initilization tasks.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private void InitTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            QlCommands.ClearQlWinConsole();
            // Synchronous
            // ReSharper disable once UnusedVariable
            // Request the configstrings after the current players have already been gathered in order
            // to get an accurate listing of the teams. This will also take care of any players that might have
            // been initially missed by the 'players' command.
            Task c = QlCommands.QlCmdConfigStrings();
            
            Debug.WriteLine("Requesting configstrings in delayed initilization step.");
            // Initialization is fully complete, we can accept user commands now.
            IsInitComplete = true;
            _initTimer.Enabled = false;
            _initTimer = null;
        }

        /// <summary>
        ///     Reads the QL console window.
        /// </summary>
        private void ReadQlConsole()
        {
            IntPtr consoleWindow = QlWindowUtils.GetQuakeLiveConsoleWindow();
            IntPtr cText = QlWindowUtils.GetQuakeLiveConsoleTextArea(consoleWindow,
                QlWindowUtils.GetQuakeLiveConsoleInputArea(consoleWindow));
            if (cText != IntPtr.Zero)
            {
                while (IsReadingConsole)
                {
                    int textLength = Win32Api.SendMessage(cText, Win32Api.WM_GETTEXTLENGTH, IntPtr.Zero,
                        IntPtr.Zero);
                    if ((textLength == 0) || (ConsoleTextProcessor.OldWholeConsoleLineLength == textLength))
                        continue;

                    // Entire console window text
                    var entireBuffer = new StringBuilder(textLength + 1);
                    Win32Api.SendMessage(cText, Win32Api.WM_GETTEXT, new IntPtr(textLength + 1), entireBuffer);
                    string received = entireBuffer.ToString();
                    ConsoleTextProcessor.ProcessEntireConsoleText(received, textLength);

                    int lengthDifference = Math.Abs(textLength - _oldLength);

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
        /// Starts the delayed initialization steps.
        /// </summary>
        /// <param name="seconds">The number of seconds the timer should wait before executing.</param>
        private void StartDelayedInit(double seconds)
        {
            _initTimer = new Timer(seconds * 1000) { AutoReset = false, Enabled = true };
            _initTimer.Elapsed += InitTimerOnElapsed;
        }
    }
}