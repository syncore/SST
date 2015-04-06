using System;
using System.Diagnostics;
using System.IO;

namespace SSB.Util
{
    /// <summary>
    ///     Class for various filepaths.
    /// </summary>
    public static class Filepaths
    {
        private const string AccountDateDatabaseFile = "acctdate.db";
        private const string BanDatabaseFile = "bannedusers.db";
        private const string ConfigurationFile = "ssbconfig.cfg";
        private const string EloDatabaseFile = "elo.db";
        private const string PickupGameDatabaseFile = "pickups.db";
        private const string QuitDatabaseFile = "earlyquits.db";
        private const string SeenDateDatabaseFile = "seendate.db";
        private const string UserDatabaseFile = "ssbusers.db";

        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "data");

        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "log");
        
        private static readonly string _accountDateDatabaseFilePath = Path.Combine(DataDirectory,
            AccountDateDatabaseFile);

        private static readonly string _configurationFilePath = Path.Combine(DataDirectory, ConfigurationFile);

        private static readonly string _eloDatabaseFilePath = Path.Combine(DataDirectory, EloDatabaseFile);

        private static readonly string _pickupGameDatabaseFilePath = Path.Combine(DataDirectory,
            PickupGameDatabaseFile);
        
        private static readonly string _quitDatabaseFilePath = Path.Combine(DataDirectory, QuitDatabaseFile);

        private static readonly string _seenDateDatabaseFilePath = Path.Combine(DataDirectory,
            SeenDateDatabaseFile);

        private static readonly string _banDatabaseFilePath = Path.Combine(DataDirectory, BanDatabaseFile);
        
        private static readonly string _userDatabaseFilePath = Path.Combine(DataDirectory, UserDatabaseFile);


        /// <summary>
        /// Gets the account date database file path.
        /// </summary>
        /// <value>
        /// The account date database file path.
        /// </value>
        public static string AccountDateDatabaseFilePath
        {
            get { return _accountDateDatabaseFilePath; }
        }

        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        /// <value>
        /// The configuration path.
        /// </value>
        public static string ConfigurationFilePath
        {
            get { return _configurationFilePath; }
        }

        /// <summary>
        /// Gets the elo database file path.
        /// </summary>
        /// <value>
        /// The elo database file path.
        /// </value>
        public static string EloDatabaseFilePath
        {
            get { return _eloDatabaseFilePath; }
        }

        /// <summary>
        /// Gets the pickup game database file path.
        /// </summary>
        /// <value>
        /// The pickup game database file path.
        /// </value>
        public static string PickupGameDatabaseFilePath
        {
            get { return _pickupGameDatabaseFilePath; }
        }

        /// <summary>
        /// Gets the quit database file path.
        /// </summary>
        /// <value>
        /// The quit database file path.
        /// </value>
        public static string QuitDatabaseFilePath
        {
            get { return _quitDatabaseFilePath; }
        }

        /// <summary>
        /// Gets the seen date database file path.
        /// </summary>
        /// <value>
        /// The seen date database file path.
        /// </value>
        public static string SeenDateDatabaseFilePath
        {
            get { return _seenDateDatabaseFilePath; }
        }

        /// <summary>
        /// Gets the ban database file path.
        /// </summary>
        /// <value>
        /// The ban database file path.
        /// </value>
        public static string BanDatabaseFilePath
        {
            get { return _banDatabaseFilePath; }
        }

        /// <summary>
        /// Gets the user database path.
        /// </summary>
        /// <value>
        /// The user database path.
        /// </value>
        public static string UserDatabaseFilePath
        {
            get { return _userDatabaseFilePath; }
        }

        /// <summary>
        /// Creates the data directory.
        /// </summary>
        public static void CreateDataDirectory()
        {
            if (Directory.Exists(DataDirectory)) return;
            try
            {
                Directory.CreateDirectory(DataDirectory);
                Debug.WriteLine("Created data directory at: " + DataDirectory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to create data directory: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates the log directory.
        /// </summary>
        public static void CreateLogDirectory()
        {
            if (Directory.Exists(LogDirectory)) return;
            try
            {
                Directory.CreateDirectory(LogDirectory);
                Debug.WriteLine("Created log directory at: " + LogDirectory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to create log directory: " + ex.Message);
            }
        }
    }
}