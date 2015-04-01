using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using SSB.Config;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Class responsible for user database operations.
    /// </summary>
    public class DbUsers : CommonSqliteDb, IConfiguration
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.UserDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.UserDatabaseFilePath;
        private string _owner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbUsers" /> class.
        /// </summary>
        public DbUsers()
        {
            VerifyDb();
            LoadCfg();
            AddOwnerToDb();
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="accessLevel">The access level.</param>
        /// <param name="addedBy">The user who is performing the addition.</param>
        /// <param name="dateAdded">The date the user was added.</param>
        /// <returns><c>true</c>if successful, otherwise <c>false</c>.</returns>
        public UserDbResult AddUserToDb(string user, UserLevel accessLevel, string addedBy, string dateAdded)
        {
            if (VerifyDb())
            {
                if (DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return UserDbResult.UserAlreadyExists;
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
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@accesslevel", (long)accessLevel);
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            cmd.Parameters.AddWithValue("@dateadded", dateAdded);
                            cmd.ExecuteNonQuery();
                            return UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to database: " + ex.Message);
                    return UserDbResult.InternalError;
                }
            }
            return UserDbResult.Unspecified;
        }

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
        ///     Deletes the user from database.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <param name="addedBy">The admin who originally added the user to be deleted.</param>
        /// <param name="addedByLevel">The access level of the admin who originally added the user to be deleted.</param>
        /// <returns><c>true</c> if the user was successfully deleted, <c>false</c> if unsuccessful.</returns>
        public UserDbResult DeleteUserFromDb(string user, string addedBy, UserLevel addedByLevel)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return UserDbResult.UserDoesntExist;
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
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from the user database.", user));
                                return UserDbResult.Success;
                            }
                            Debug.WriteLine(
                                "User: {0} exists in the database but cannot be deleted because user was not added by {1}",
                                user, addedBy);
                            return UserDbResult.UserNotAddedBySender;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem deleting user from database: " + ex.Message);
                    return UserDbResult.InternalError;
                }
            }
            return UserDbResult.Unspecified;
        }

        /// <summary>
        ///     Gets all of the users in the users database.
        /// </summary>
        /// <returns>The users as a list of <see cref="User" /> objects.</returns>
        public List<User> GetAllUsers()
        {
            var allUsers = new List<User>();
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "SELECT * FROM users";
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine("GetAllUsers: No users found in database");
                                    return allUsers;
                                }
                                while (reader.Read())
                                {
                                    var user = (string)reader["user"];
                                    var level = GetUserLevel(user);
                                    allUsers.Add(new User { Name = user, AccessLevel = level });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem verifying database when trying to get all users: " +
                                    ex.Message);
                }
            }
            return allUsers;
        }

        /// <summary>
        ///     Gets the current admins on the server.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <returns>The current admins on the server as a comma-separated string, if any.</returns>
        public string GetCurrentAdminsOnServer(Dictionary<string, PlayerInfo> currentPlayers)
        {
            var sb = new StringBuilder();
            foreach (var player in currentPlayers)
            {
                if (GetUserLevel(player.Key) >= UserLevel.Admin)
                {
                    sb.Append(string.Format("{0}, ", player.Key));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Gets the users on the server who are of SuperUser access level or higher.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <returns>The current users on the server of SuperUser access level or higher, if any.</returns>
        public string GetSuperUsersOrHigherOnServer(Dictionary<string, PlayerInfo> currentPlayers)
        {
            var sb = new StringBuilder();
            foreach (var player in currentPlayers)
            {
                if (GetUserLevel(player.Key) >= UserLevel.SuperUser)
                {
                    sb.Append(string.Format("{0}, ", player.Key));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Gets the requested user's level.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's level.</returns>
        public UserLevel GetUserLevel(string user)
        {
            var level = UserLevel.None;
            if (VerifyDb())
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
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(string.Format(
                                        "UserDb.GetUserLevel: User: {0} does not exist in the user database.",
                                        user));
                                    return UserLevel.None;
                                }
                                while (reader.Read())
                                {
                                    Debug.WriteLine(
                                        "UserDb.GetUserLevel: Got user level for: {0}, level: {1}", user,
                                        (UserLevel)reader["accesslevel"]);
                                    level = (UserLevel)reader["accesslevel"];
                                    return level;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem checking if user exists in database: " + ex.Message);
                }
            }
            return level;
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
            _owner = cfgHandler.Config.CoreOptions.owner;
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
            cfgHandler.Config.CoreOptions.owner = _owner;
            cfgHandler.WriteConfiguration();
        }

        /// <summary>
        /// Public method for accessing protected <see cref="DoesUserExistInDb"/> method
        /// to check whether a user exists in the user database or not.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user exists in the user database, otherwise
        /// <c>false</c>.</returns>
        public bool UserExists(string user)
        {
            return DoesUserExistInDb(user);
        }

        /// <summary>
        ///     Creates the user database.
        /// </summary>
        protected override void CreateDb()
        {
            if (DbExists()) return;

            SQLiteConnection.CreateFile(_sqlDbPath);

            try
            {
                using (var sqlcon = new SQLiteConnection(_sqlConString))
                {
                    sqlcon.Open();

                    var s =
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
                DeleteDb();
            }
        }

        /// <summary>
        ///     Checks whether the user database exists.
        /// </summary>
        /// <returns><c>true</c>if the user database exists, otherwise <c>false</c>.</returns>
        protected override bool DbExists()
        {
            return (File.Exists(_sqlDbPath));
        }

        /// <summary>
        ///     Deletes the user database.
        /// </summary>
        protected override void DeleteDb()
        {
            if (!DbExists()) return;

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
        protected override bool DoesUserExistInDb(string user)
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
                        using (var reader = cmd.ExecuteReader())
                        {
                            return reader.HasRows;
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
        ///     Verifies the user database.
        /// </summary>
        protected override sealed bool VerifyDb()
        {
            if (!DbExists())
            {
                CreateDb();
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
                        CreateDb();
                        return false;
                    }
                }
            }
        }

        /// <summary>
        ///     Adds the owner (from the config file on the disk) to the database.
        /// </summary>
        private void AddOwnerToDb()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AddUserToDb(_owner, UserLevel.Owner, "AUTO", date);
        }
    }
}