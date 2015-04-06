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
        public static readonly Type LogClassType = MethodBase.GetCurrentMethod().DeclaringType;
        public static readonly ILog Logger = LogManager.GetLogger(LogClassType);
        private static readonly ConfigHandler CfgHandler;

        static Log()
        {
            CfgHandler = new ConfigHandler();
        }

        /// <summary>
        /// Programatically sets up the logger without using an XML configuration file on disk.
        /// </summary>
        public static void Configure()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var rollingPatternLayout = new PatternLayout
            {
                //ConversionPattern = "%date [%thread] %level %logger - %message%newline%exception"
                ConversionPattern = "[%level] %date{G} [%thread] - %message%newline%exception"
            };

            rollingPatternLayout.ActivateOptions();

            var rollingLevelRangeFilter = new LevelRangeFilter
            {
                LevelMin = Level.Info,
                LevelMax = Level.Fatal
            };

            var rollingFileAppender = new RollingFileAppender
            {
                File = @"log\ssbdebug.log",
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
                ConversionPattern = "[%level] %date{G} [%thread] - %logger: %message%newline%exception"
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
        }

        /// <summary>
        ///     Logs the current message to both the loggers specified for handling
        ///  information and debug messages.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="type">The type of class that initiated the log request.</param>
        public static void LogInfoAndDebug(string msg, Type type)
        {
            ILog logger = LogManager.GetLogger(type);
            // debug output
            logger.Debug(msg);
            
            CfgHandler.ReadConfiguration();
            if (CfgHandler.Config.CoreOptions.logSsbEventsToDisk)
            {
                // disk file
                logger.Info(msg);    
            }
        }
    }
}