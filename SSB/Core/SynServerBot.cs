﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using SSB.Model;
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

        /// <summary>
        ///     Initializes a new instance of the <see cref="SynServerBot" /> main class.
        /// </summary>
        public SynServerBot()
        {
            CurrentPlayers = new Dictionary<string, PlayerInfo>();
            GuiOptions = new GuiOptions();
            GuiControls = new GuiControls();
            QlCommands = new QlCommands(this);
            Parser = new Parser();
            QlWindowUtils = new QlWindowUtils();
            ConsoleTextProcessor = new ConsoleTextProcessor(this);
            ServerEventProcessor = new ServerEventProcessor(this);
            BotCommands = new BotCommands(this);

            // Start reading the console
            StartConsoleReadThread();

            // First and foremost, clear the console and get the player listing (TODO: maybe have a method of general Init events)
            QlCommands.ClearQlWinConsole();
            QlCommands.QlCmdPlayers();
        }

        /// <summary>
        ///     Gets the bot commands.
        /// </summary>
        /// <value>
        ///     The bot commands.
        /// </value>
        public BotCommands BotCommands { get; private set; }

        /// <summary>
        ///     Gets the console text processor.
        /// </summary>
        /// <value>
        ///     The console text processor.
        /// </value>
        public ConsoleTextProcessor ConsoleTextProcessor { get; private set; }

        /// <summary>
        ///     Gets the current players.
        /// </summary>
        /// <value>
        ///     The current players.
        /// </value>
        public Dictionary<string, PlayerInfo> CurrentPlayers { get; private set; }

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
        ///     Starts the console read thread.
        /// </summary>
        public void StartConsoleReadThread()
        {
            if (_isReadingConsole) return;
            Debug.WriteLine("...starting a thread to read QL console.");
            _isReadingConsole = true;
            var readConsoleThread = new Thread(ReadQlConsole) {IsBackground = true};
            readConsoleThread.Start();
        }

        public void StopConsoleReadThread()
        {
            _isReadingConsole = false;
            Debug.WriteLine("...stopping QL console read thread.");
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
                while (_isReadingConsole)
                {
                    int textLength = Win32Api.SendMessage(cText, Win32Api.WM_GETTEXTLENGTH, IntPtr.Zero,
                        IntPtr.Zero);
                    if ((textLength == 0) || (ConsoleTextProcessor.OldWholeConsoleLineLength == textLength))
                        continue;

                    // Entire console window text
                    var test = new StringBuilder(textLength + 1);
                    Win32Api.SendMessage(cText, Win32Api.WM_GETTEXT, new IntPtr(textLength + 1), test);
                    string received = test.ToString();
                    ConsoleTextProcessor.ProcessEntireConsoleText(received, textLength);

                    // Only last line of text within entire console window
                    string n = test.ToString();
                    int absoluteLastNewLine = n.LastIndexOf("\r\n", StringComparison.Ordinal);
                    int secondtoLastNewLine = absoluteLastNewLine > 0
                        ? n.LastIndexOf("\r\n", absoluteLastNewLine - 1, StringComparison.Ordinal)
                        : -1;
                    if (secondtoLastNewLine < n.Length)
                    {
                        //Issue where window was just cleared & first char is cut off
                        ConsoleTextProcessor.ProcessLastLineOfConsole(
                            secondtoLastNewLine == -1
                                ? n.Substring(0)
                                : n.Substring((secondtoLastNewLine + 2)), textLength);
                    }
                }
            }
            else
            {
                Debug.WriteLine("Couldn't find Quake Live console text area");
            }
        }
    }
}