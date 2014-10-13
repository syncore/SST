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
        private const string ConfigurationFile = "ssbconfig.cfg";
        private const string UserDatabaseFile = "ssbusers.db";

        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "data");
        
        private static readonly string _configurationFilePath = Path.Combine(DataDirectory, ConfigurationFile);

        private static readonly string _userDatabaseFilePath = Path.Combine(DataDirectory, UserDatabaseFile);


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