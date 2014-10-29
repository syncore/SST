using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SSB.Ui;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     The main class for SSB.
    /// </summary>
    public class SynServerBot
    {
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
            CommandProcessor = new CommandProcessor(this);

            // Start reading the console
            StartConsoleReadThread();
            // Set the important details of the server
            InitServerInformation();
        }

        /// <summary>
        ///     Gets or sets the name of the account that is running the bot.
        /// </summary>
        /// <value>
        ///     The name of the account that is running the bot.
        /// </value>
        public string BotName { get; set; }

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
        /// Gets or sets a value indicating whether this instance is reading the console.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is reading console; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadingConsole
        {
            get { return _isReadingConsole; }
            set { _isReadingConsole = value; }
        }

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

        public void StopConsoleReadThread()
        {
            IsReadingConsole = false;
            Debug.WriteLine("...stopping QL console read thread.");
            MessageBox.Show("Stopped reading Quake Live events, because Quake Live is not detected.",
                "Stopped reading events", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
            // Initially get the player listing when we start. Synchronous since init.
            var q = QlCommands.QlCmdPlayers();
            // Get name of account running the bot.
            QlCommands.SendCvarReq("name", false);
            // Get the server's id
            QlCommands.SendCvarReq("serverinfo", true);
            // Enable developer mode
            QlCommands.EnableDeveloperMode();
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
                        // Standardize QL's annoying string formatting
                        var diffBuilder = new StringBuilder(received.Substring(_oldLength, lengthDifference));
                        diffBuilder.Replace("\"\r\n\r\n", "\"\r\n");
                        diffBuilder.Replace("\r\n\"\r\n", "\r\n");
                        var c = ConsoleTextProcessor.ProcessShortConsoleLines(diffBuilder.ToString());
                    }

                    // Detect when buffer is about to be full, in order to auto-clear.
                    // Win Edit controls can have a max of 30,000 characters, see:
                    // "Limits of Edit Controls" - http://msdn.microsoft.com/en-us/library/ms997530.aspx
                    // More info: Q3 source (win_syscon.c), Conbuf_AppendText method
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

        //private void ProcessServerCommand(string line)
        //{
        //    if (!line.StartsWith("serverCommand:", StringComparison.InvariantCultureIgnoreCase)) return;
        //    if (!SeqIdRegex.IsMatch(line)) return;
        //        Match m = SeqIdRegex.Match(line);
        //        string seq = m.Value.Replace("serverCommand: ", string.Empty);
        //        int seqNum;
        //        bool seqIsNum = (int.TryParse(seq, out seqNum));
        //    if (!seqIsNum) return;
        //    if (seqNum <= _lastScSeq) return;
        //    Debug.WriteLine("Got servercmd seq #: {0}. Last processed was: {1}", seqNum, _lastScSeq);
        //    _lastScSeq = seqNum;
        //}
    }
}