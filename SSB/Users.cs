using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace SSB
{
    /// <summary>
    /// Class responsible for user database operations.
    /// </summary>
    public class Users : IConfiguration
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.UserDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.UserDatabaseFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Users"/> class.
        /// </summary>
        public Users()
        {
            Admins = new List<string>();
            VerifyUserDb();
            LoadCfg();
        }

        /// <summary>
        /// Gets or sets the admins.
        /// </summary>
        /// <value>
        /// The admins.
        /// </value>
        public List<string> Admins { get; set; }

        /// <summary>
        /// Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="accessLevel">The access level.</param>
        /// <param name="addedBy">The user who is performing the addition.</param>
        /// <param name="dateAdded">The date the user was added.</param>
        public void AddUserToDb(string user, int accessLevel, string addedBy, string dateAdded)
        {
            if (VerifyUserDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "INSERT INTO users(user, accesslevel, addedby, dateadded) VALUES(@user, @accesslevel, @addedby, @dateadded)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            cmd.Parameters.AddWithValue("@accesslevel", accessLevel);
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            cmd.Parameters.AddWithValue("@dateadded", dateAdded);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to database: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Checks whether the configuration already exists.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if configuration exists, otherwise <c>false</c>
        /// </returns>
        public bool CfgExists()
        {
            return (File.Exists(Filepaths.ConfigurationFilePath));
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        public void LoadCfg()
        {
            if (!CfgExists())
            {
                LoadDefaultCfg();
            }
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            Admins = cfgHandler.InitialAdminUsers;
        }

        /// <summary>
        /// Loads the default configuration.
        /// </summary>
        public void LoadDefaultCfg()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        public void SaveCfg()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            cfgHandler.InitialAdminUsers = Admins;
            cfgHandler.WriteConfiguration();
        }

        /// <summary>
        /// Creates the user database.
        /// </summary>
        private void CreateUserDb()
        {
            if (UserDbExists()) return;

            SQLiteConnection.CreateFile(_sqlDbPath);

            try
            {
                using (var sqlcon = new SQLiteConnection(_sqlConString))
                {
                    sqlcon.Open();

                    string s =
                        "CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, addedby TEXT NOT NULL, accesslevel INTEGER, addedby TEXT NOT NULL, date DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("User database created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem creating user database: " + ex.Message);
                DeleteUserDb();
            }
        }

        /// <summary>
        /// Deletes the user database.
        /// </summary>
        private void DeleteUserDb()
        {
            if (!UserDbExists()) return;

            try
            {
                File.Delete(_sqlDbPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to delete user database: " + ex.Message);
            }
        }

        /// <summary>
        /// Checks whether the user database exists.
        /// </summary>
        /// <returns><c>true</c>if the user database exists, otherwise <c>false</c>.</returns>
        private bool UserDbExists()
        {
            return (File.Exists(_sqlDbPath));
        }

        /// <summary>
        /// Verifies the user database.
        /// </summary>
        private bool VerifyUserDb()
        {
            if (!UserDbExists())
            {
                CreateUserDb();
                return true;
            }

            using (var sqlcon = new SQLiteConnection(_sqlConString))
            {
                sqlcon.Open();

                using (var cmd = new SQLiteCommand(sqlcon))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'users'";

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Debug.WriteLine("Users table not found in DB... Creating DB...");
                        CreateUserDb();
                        return false;
                    }
                }
            }
        }
    }
}