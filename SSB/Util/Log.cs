using System;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using SSB.Config;

namespace SSB.Util
{
    /// <summary>
    /// Logger utility class.
    /// </summary>
    public static class Log
    {
        private static readonly Type LogClassType = MethodBase.GetCurrentMethod().DeclaringType;
        public static readonly ILog Logger = LogManager.GetLogger(LogClassType);
        private static readonly string _logPrefix = "[CORE]";
        private static readonly ConfigHandler CfgHandler;

        /// <summary>
        /// Initializes the <see cref="Log"/> class.
        /// </summary>
        static Log()
        {
            CfgHandler = new ConfigHandler();
            Configure();
        }

        /// <summary>
        /// Writes the specified message to various loggers.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="type">The type of class that initiated the log request.</param>
        /// <param name="prefix">The manually-specified class prefix.</param>
        public static void Write(string msg, Type type, string prefix)
        {
            ILog logger = LogManager.GetLogger(type);

            // debug output
#if DEBUG
            logger.Debug(string.Format("{0} {1}", prefix, msg));
#endif

            CfgHandler.ReadConfiguration();
            if (CfgHandler.Config.CoreOptions.logSsbEventsToDisk)
            {
                // disk file
                logger.Info(string.Format("{0} {1}", prefix, msg));
            }
        }

        /// <summary>
        /// Writes a critical message that bypasses a configuration read and thus
        /// the user's 'log to disk' setting. Typically used for logging high priority messages
        /// and/or fatal errors.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <param name="type">The type.</param>
        /// <param name="prefix">The prefix.</param>
        public static void WriteCritical(string msg, Type type, string prefix)
        {
            ILog logger = LogManager.GetLogger(type);

            // debug output
#if DEBUG
            logger.Debug(string.Format("{0} {1}", prefix, msg));
#endif

            logger.Info(string.Format("{0} {1}", prefix, msg));
        }

        /// <summary>
        /// Programatically sets up the logger without using an XML configuration file on disk.
        /// </summary>
        private static void Configure()
        {
            var dirCreated = Filepaths.CreateLogDirectory();

            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var rollingPatternLayout = new PatternLayout
            {
                //ConversionPattern = "%date [%thread] %level %logger - %message%newline%exception"
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
                File = ((dirCreated) ?
                string.Format(@"{0}\ssbdebug.log", Filepaths.LogDirectoryName) : "ssbdebug.log"),
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

            var debugAppender = new DebugAppender { Layout = debugPatternLayout };
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