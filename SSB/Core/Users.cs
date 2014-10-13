using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using SSB.Config;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for user database operations.
    /// </summary>
    public class Users : IConfiguration
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.UserDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.UserDatabaseFilePath;
        private HashSet<string> _owners = new HashSet<string>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Users" /> class.
        /// </summary>
        public Users()
        {
            AllUsers = new Dictionary<string, UserLevel>();
            VerifyUserDb();
            LoadCfg();
            RetrieveAllUsers();
            AddOwnersToDb();
        }

        /// <summary>
        ///     Gets or sets all users.
        /// </summary>
        /// <value>
        ///     All users.
        /// </value>
        public Dictionary<string, UserLevel> AllUsers { get; set; }

        /// <summary>
        ///     Checks whether the configuration already exists.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if configuration exists, otherwise <c>false</c>
        /// </returns>
        public bool CfgExists()
        {
            return (File.Exists(Filepaths.ConfigurationFilePath));
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadCfg()
        {
            if (!CfgExists())
            {
                LoadDefaultCfg();
            }
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            _owners = cfgHandler.Owners;
        }

        /// <summary>
        ///     Loads the default configuration.
        /// </summary>
        public void LoadDefaultCfg()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.RestoreDefaultConfiguration();
        }

        /// <summary>
        ///     Saves the configuration.
        /// </summary>
        public void SaveCfg()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            cfgHandler.Owners = _owners;
            cfgHandler.WriteConfiguration();
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="accessLevel">The access level.</param>
        /// <param name="addedBy">The user who is performing the addition.</param>
        /// <param name="dateAdded">The date the user was added.</param>
        /// <returns><c>true</c>if successful, otherwise <c>false</c>.</returns>
        public DbResult AddUserToDb(string user, UserLevel accessLevel, string addedBy, string dateAdded)
        {
            if (VerifyUserDb())
            {
                if (DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return DbResult.UserAlreadyExists;
                }
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
                            cmd.Parameters.AddWithValue("@accesslevel", (long) accessLevel);
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            cmd.Parameters.AddWithValue("@dateadded", dateAdded);
                            cmd.ExecuteNonQuery();
                            AllUsers.Add(user, accessLevel);
                            return DbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to database: " + ex.Message);
                    return DbResult.InternalError;
                }
            }
            return DbResult.Unspecified;
        }

        /// <summary>
        ///     Deletes the user from database.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <param name="addedBy">The admin who originally added the user to be deleted.</param>
        /// <param name="addedByLevel">The access level of the admin who originally added the user to be deleted.</param>
        /// <returns><c>true</c> if the user was successfully deleted, <c>false</c> if unsuccessful.</returns>
        public DbResult DeleteUserFromDb(string user, string addedBy, UserLevel addedByLevel)
        {
            if (VerifyUserDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return DbResult.UserDoesntExist;
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            // Owners can delete anyone, regular admins can only delete users they have personally added.
                            cmd.CommandText = addedByLevel == UserLevel.Owner
                                ? "DELETE FROM users WHERE user = @user"
                                : "DELETE FROM users WHERE user = @user AND addedby = @addedby";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from the user database.", user));
                                AllUsers.Remove(user);
                                return DbResult.Success;
                            }
                            Debug.WriteLine(
                                "User: {0} exists in the database but cannot be deleted because user was not added by {1}",
                                user, addedBy);
                            return DbResult.UserNotAddedBySender;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding deleting user from database: " + ex.Message);
                    return DbResult.InternalError;
                }
            }
            return DbResult.Unspecified;
        }

        /// <summary>
        ///     Retrieves all users from database and populates AllUsers dictionary.
        /// </summary>
        public void RetrieveAllUsers()
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
                            cmd.CommandText = "SELECT * FROM users";
                            cmd.Prepare();

                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        AllUsers[(string) reader["user"]] = (UserLevel) reader["accesslevel"];
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to retrieve all users from database: " + ex.Message);
                }
            }
        }

        /// <summary>
        ///     Adds the owners (from the config file on the disk) to the database.
        /// </summary>
        private void AddOwnersToDb()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (string owner in _owners)
            {
                AddUserToDb(owner, UserLevel.Owner, "AUTO", date);
            }
        }

        /// <summary>
        ///     Creates the user database.
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
                        "CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, accesslevel INTEGER, addedby TEXT NOT NULL, dateadded DATETIME)";
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
        ///     Deletes the user database.
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
        ///     Checks whether the user already exists in the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user exists, otherwise <c>false</c>.</returns>
        private bool DoesUserExistInDb(string user)
        {
            try
            {
                using (var sqlcon = new SQLiteConnection(_sqlConString))
                {
                    sqlcon.Open();

                    using (var cmd = new SQLiteCommand(sqlcon))
                    {
                        cmd.CommandText = "SELECT * FROM users WHERE user = @user";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                Debug.WriteLine(string.Format(
                                    "User: {0} does not exist in the user database.", user));
                                return false;
                            }
                            Debug.WriteLine(string.Format("User: {0} already exists in the user database.",
                                user));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem checking if user exists in database: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        ///     Checks whether the user database exists.
        /// </summary>
        /// <returns><c>true</c>if the user database exists, otherwise <c>false</c>.</returns>
        private bool UserDbExists()
        {
            return (File.Exists(_sqlDbPath));
        }

        /// <summary>
        ///     Verifies the user database.
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

                    using (SQLiteDataReader sdr = cmd.ExecuteReader())
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