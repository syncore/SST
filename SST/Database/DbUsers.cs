using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using SST.Config;
using SST.Config.Core;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Database
{
    /// <summary>
    /// Class responsible for user database operations.
    /// </summary>
    public class DbUsers : CommonSqliteDb, IConfiguration
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[DB:USERS]";
        private readonly string _sqlConString = "Data Source=" + Filepaths.UserDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.UserDatabaseFilePath;
        private string _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbUsers"/> class.
        /// </summary>
        public DbUsers()
        {
            VerifyDb();
            LoadCfg();
            AddOwnerToDb();
        }

        /// <summary>
        /// Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="accessLevel">The access level.</param>
        /// <param name="addedBy">The user who is performing the addition.</param>
        /// <param name="dateAdded">The date the user was added.</param>
        /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
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
                                "INSERT INTO users(user, accesslevel, addedby, dateadded) VALUES(@user," +
                                " @accesslevel, @addedby, @dateadded)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@accesslevel", (long)accessLevel);
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            cmd.Parameters.AddWithValue("@dateadded", dateAdded);
                            cmd.ExecuteNonQuery();
                            Log.Write(string.Format(
                                "{0} successfully added to user database. Access level: {1}, added by: {2} on: {3}",
                                user, Enum.GetName(typeof(UserLevel), accessLevel), addedBy, dateAdded), _logClassType, _logPrefix);
                            return UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem adding player {0} to user database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                    return UserDbResult.InternalError;
                }
            }
            return UserDbResult.Unspecified;
        }

        /// <summary>
        /// Checks whether the configuration already exists.
        /// </summary>
        /// <returns><c>true</c> if configuration exists, otherwise <c>false</c></returns>
        public bool CfgExists()
        {
            return (File.Exists(Filepaths.ConfigurationFilePath));
        }

        /// <summary>
        /// Deletes the user from database.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <param name="addedBy">The admin who originally added the user to be deleted.</param>
        /// <param name="addedByLevel">
        /// The access level of the admin who originally added the user to be deleted.
        /// </param>
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
                            // Owners can delete anyone, regular admins can only delete users they
                            // have personally added.
                            cmd.CommandText = addedByLevel == UserLevel.Owner
                                ? "DELETE FROM users WHERE user = @user"
                                : "DELETE FROM users WHERE user = @user AND addedby = @addedby";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@addedby", addedBy);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Deleted user: {0} from user database.", user), _logClassType, _logPrefix);
                                return UserDbResult.Success;
                            }
                            Log.Write(string.Format(
                                "Player: {0} exists in the database but cannot be deleted because player was not added by {1}",
                                user, addedBy), _logClassType, _logPrefix);
                            return UserDbResult.UserNotAddedBySender;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem deleting player {0} from last seen date database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                    return UserDbResult.InternalError;
                }
            }
            return UserDbResult.Unspecified;
        }

        /// <summary>
        /// Gets all of the users in the users database.
        /// </summary>
        /// <returns>The users as a list of <see cref="User"/> objects.</returns>
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
                    Log.WriteCritical("Problem getting all users from user database: " +
                                    ex.Message, _logClassType, _logPrefix);
                }
            }
            return allUsers;
        }

        /// <summary>
        /// Gets the current admins on the server.
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
        /// Gets the users on the server who are of SuperUser access level or higher.
        /// </summary>
        /// <param name="currentPlayers">The current players.</param>
        /// <returns>
        /// The current users on the server of SuperUser access level or higher, if any.
        /// </returns>
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
        /// Gets the requested user's level.
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
                                    return UserLevel.None;
                                }
                                while (reader.Read())
                                {
                                    level = (UserLevel)reader["accesslevel"];
                                    return level;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem getting user level for player {0} from user database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                }
            }
            return level;
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        public void LoadCfg()
        {
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();
            _owner = cfg.CoreOptions.owner;
        }

        /// <summary>
        /// Loads the default configuration.
        /// </summary>
        public void LoadDefaultCfg()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.RestoreDefaultConfiguration();
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        public void SaveCfg()
        {
            var cfgHandler = new ConfigHandler();
            var cfg = cfgHandler.ReadConfiguration();
            cfg.CoreOptions.owner = _owner;
            cfgHandler.WriteConfiguration(cfg);
        }

        /// <summary>
        /// Public method for accessing protected <see cref="DoesUserExistInDb"/> method to check
        /// whether a user exists in the user database or not.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user exists in the user database, otherwise <c>false</c>.</returns>
        public bool UserExists(string user)
        {
            return DoesUserExistInDb(user);
        }

        /// <summary>
        /// Creates the user database.
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
                        "CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL," +
                        " accesslevel INTEGER, addedby TEXT NOT NULL, dateadded DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Log.Write("User database created.", _logClassType, _logPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem creating user database" + ex.Message, _logClassType, _logPrefix);
                DeleteDb();
            }
        }

        /// <summary>
        /// Checks whether the user database exists.
        /// </summary>
        /// <returns><c>true</c> if the user database exists, otherwise <c>false</c>.</returns>
        protected override bool DbExists()
        {
            return (File.Exists(_sqlDbPath));
        }

        /// <summary>
        /// Deletes the user database.
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
                Log.WriteCritical("Unable to delete user database: " + ex.Message,
                    _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Checks whether the user already exists in the database.
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
                Log.WriteCritical(string.Format(
                    "Problem checking if player {0} exists in user database: {1}",
                    user, ex.Message), _logClassType, _logPrefix);
            }
            return false;
        }

        /// <summary>
        /// Verifies the user database.
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
                        Log.Write(
                            "users table not found in user database... Creating user database...",
                            _logClassType, _logPrefix);
                        CreateDb();
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the owner (from the config file on the disk) to the database.
        /// </summary>
        private void AddOwnerToDb()
        {
            if (_owner.Equals(CoreOptions.defaultUnsetOwnerName,
                StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AddUserToDb(_owner, UserLevel.Owner, "AUTO", date);
        }
    }
}
