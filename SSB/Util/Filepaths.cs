﻿using System;
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
        private const string ConfigurationFile = "ssbconfig.cfg";
        private const string SeenDateDatabaseFile = "seendate.db";
        private const string UserDatabaseFile = "ssbusers.db";

        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "data");
        
        private static readonly string _accountDateDatabaseFilePath = Path.Combine(DataDirectory,
            AccountDateDatabaseFile);

        private static readonly string _configurationFilePath = Path.Combine(DataDirectory, ConfigurationFile);

        private static readonly string _seenDateDatabaseFilePath = Path.Combine(DataDirectory,
            SeenDateDatabaseFile);
        
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
    }
}