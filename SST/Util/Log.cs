using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace SST.Util
{
    /// <summary>
    ///     Logger utility class.
    /// </summary>
    public static class Log
    {
        private static readonly string _logPrefix = "[CORE]";
        private static bool _logToDisk;
        private static bool _logToSstConsole;
        private static readonly List<string> DeferredMessages;
        private static readonly Type LogClassType = MethodBase.GetCurrentMethod().DeclaringType;
        public static readonly ILog Logger = LogManager.GetLogger(LogClassType);

        /// <summary>
        ///     Initializes the <see cref="Log" /> class.
        /// </summary>
        static Log()
        {
            DeferredMessages = new List<string>();
            Configure();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether log events should be written to
        ///     the activity log in the UI.
        /// </summary>
        /// <value>
        ///     <c>true</c> if log events should be written to the activity log in the UI,
        ///     otherwise <c>false</c>.
        /// </value>
        public static bool LogToSstConsole
        {
            get { return _logToSstConsole; }
            set
            {
                if (_logToSstConsole && !value)
                {
                    WriteCritical("Disabled logging of SST debug events to activity log.",
                        LogClassType, _logPrefix);
                }
                else if (!_logToSstConsole && value)
                {
                    WriteCritical("Enabled logging of SST debug events to activity log.",
                        LogClassType, _logPrefix);
                }
                _logToSstConsole = value;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether log events should be written to disk.
        /// </summary>
        /// <value>
        ///     <c>true</c> if log events should be written to disk, otherwise <c>false</c>.
        /// </value>
        public static bool LogToDisk
        {
            get { return _logToDisk; }
            set
            {
                if (_logToDisk && !value)
                {
                    WriteCritical("Disk logging is now disabled for SST debug events.",
                        LogClassType, _logPrefix);
                }
                else if (!_logToDisk && value)
                {
                    WriteCritical("Disk logging is now enabled for SST debug events.",
                        LogClassType, _logPrefix);
                }
                _logToDisk = value;
            }
        }

        /// <summary>
        ///     Gets or sets the UI console activity log.
        /// </summary>
        /// <value>
        ///     The UI console activityl og.
        /// </value>
        public static TextBox LogUiConsole { get; set; }

        /// <summary>
        ///     Writes the specified message to various loggers.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="type">The type of class that initiated the log request.</param>
        /// <param name="prefix">The manually-specified class prefix.</param>
        public static void Write(string msg, Type type, string prefix)
        {
            var logger = LogManager.GetLogger(type);

            // debug output
#if DEBUG
            logger.Debug(string.Format("{0} {1}", prefix, msg));
#endif
            if (LogToDisk)
            {
                // disk file
                logger.Info(string.Format("{0} {1}", prefix, msg));
            }

            if (LogUiConsole == null)
            {
                DeferredMessages.Add(string.Format("{0} {1} {2}",
                    Environment.NewLine, prefix, msg));
            }

            if (LogToSstConsole)
            {
                LogUiConsole.InvokeIfRequired(
                    c => { c.AppendText(string.Format("{0} {1} {2}", Environment.NewLine, prefix, msg)); });
            }
        }

        /// <summary>
        ///     Writes a critical message that bypasses the user's 'log to disk' setting.
        ///     Typically used for logging high priority messages such as exceptions
        ///     and/or fatal errors.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="prefix">The prefix.</param>
        public static void WriteCritical(string msg, Type type, string prefix)
        {
            var logger = LogManager.GetLogger(type);

            // debug output
#if DEBUG
            logger.Debug(string.Format("{0} {1}", prefix, msg));
#endif

            logger.Info(string.Format("{0} {1}", prefix, msg));

            if (LogUiConsole == null)
            {
                DeferredMessages.Add(string.Format("{0} {1} {2}",
                    Environment.NewLine, prefix, msg));
            }
        }

        /// <summary>
        ///     Shows the deferred messages that accrued prior
        ///     to initilization of the UI log console.
        /// </summary>
        public static void ShowDeferredMessages()
        {
            if (LogToSstConsole)
            {
                foreach (var msg in DeferredMessages)
                {
                    // Closure: complier compliance
                    var msg1 = msg;

                    LogUiConsole.InvokeIfRequired(c => { c.AppendText(msg1); });
                }
            }

            DeferredMessages.Clear();
        }

        /// <summary>
        ///     Programatically sets up the logger without using an XML configuration file on disk.
        /// </summary>
        private static void Configure()
        {
            var dirCreated = Filepaths.CreateLogDirectory();

            var hierarchy = (Hierarchy) LogManager.GetRepository();

            var rollingPatternLayout = new PatternLayout
            {
                ConversionPattern = "[%level] %date{G} [%thread] %message%newline%exception"
            };

            rollingPatternLayout.ActivateOptions();

            var rollingLevelRangeFilter = new LevelRangeFilter
            {
                LevelMin = Level.Info,
                LevelMax = Level.Fatal
            };

            var rollingFileAppender = new RollingFileAppender
            {
                File = ((dirCreated)
                    ? string.Format(@"{0}\sstdebug.log", Filepaths.LogDirectoryName)
                    : "sstdebug.log"),
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                MaxSizeRollBackups = 3,
                MaximumFileSize = "4MB",
                StaticLogFileName = true,
                Layout = rollingPatternLayout
            };

            rollingFileAppender.AddFilter(rollingLevelRangeFilter);
            rollingFileAppender.ActivateOptions();
            hierarchy.Root.AddAppender(rollingFileAppender);

            var debugPatternLayout = new PatternLayout
            {
                ConversionPattern = "[%level] %date{G} [%thread] (%logger) %message%newline%exception"
            };
            debugPatternLayout.ActivateOptions();

            var debugLevelRangeFilter = new LevelRangeFilter
            {
                LevelMin = Level.Debug,
                LevelMax = Level.Debug
            };

            var debugAppender = new DebugAppender {Layout = debugPatternLayout};
            debugAppender.AddFilter(debugLevelRangeFilter);
            debugAppender.ActivateOptions();
            hierarchy.Root.AddAppender(debugAppender);

            hierarchy.Root.Level = Level.Debug;
            hierarchy.Configured = true;

            if (!dirCreated)
            {
                WriteCritical("Problem creating log directory!", LogClassType, _logPrefix);
            }
        }
    }
}