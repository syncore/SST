using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using SSB.Enum;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Database class responsible for tracking banned players.
    /// </summary>
    public class Bans : CommonSqliteDb
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.BanDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.BanDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bans" /> class.
        /// </summary>
        public Bans()
        {
            VerifyDb();
        }

        /// <summary>
        /// Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="bannedBy">The admin who added the ban.</param>
        /// <param name="banAddDate">The date and time that the ban was added.</param>
        /// <param name="banExpirationDate">The date and time that the user's ban will expire.</param>
        public UserDbResult AddUserToDb(string user, string bannedBy, DateTime banAddDate, DateTime banExpirationDate)
        {
            var result = UserDbResult.Unspecified;
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
                                "INSERT INTO bannedusers(user, bannedBy, banAddDate, banExpirationDate) VALUES(@user, @bannedBy, @banAddDate, @banExpirationDate)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            cmd.Parameters.AddWithValue("@bannedBy", bannedBy);
                            cmd.Parameters.AddWithValue("@banAddDate", banAddDate);
                            cmd.Parameters.AddWithValue("@banExpirationDate", banExpirationDate);
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine("AddUserToBanDb: {0} successfully added to ban DB by admin: {1} on: {2}. Time-ban expires on: {3}.",
                                user, bannedBy, banAddDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                                banExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo));
                            result = UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to ban database: " + ex.Message);
                    result = UserDbResult.InternalError;
                }
            }
            return result;
        }

        /// <summary>
        ///     Deletes the user from database.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        public void DeleteUserFromDb(string user)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return;
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "DELETE FROM bannedusers WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from ban database.", user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem deleting user from ban database: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets the ban information.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The ban information as a QL color-formatted string.</returns>
        public BanInfo GetBanInfo(string user)
        {
            var bInfo = new BanInfo();
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return null;
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "SELECT * FROM bannedusers WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine("GetBanInfo: User does not exist in bans database.");
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    bInfo.PlayerName = user;
                                    bInfo.BannedBy = (string)reader["bannedBy"];
                                    bInfo.BanAddedDate = (DateTime)reader["banAddDate"];
                                    bInfo.BanExpirationDate = (DateTime)reader["banExpirationDate"];
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem getting ban info from ban database: " + ex.Message);
                }
            }
            return bInfo;
        }

        /// <summary>
        /// Determines whether the existing ban is still valid, if it exists, for the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the existing ban's expiration date has not passed,
        /// <c>false</c> is ban has expired or does not exist. </returns>
        public bool IsExistingBanStillValid(string user)
        {
            var banInfo = GetBanInfo(user);
            if (banInfo == null) return false;
            return DateTime.Now <= banInfo.BanExpirationDate;
        }

        /// <summary>
        /// Gets all of the banned users.
        /// </summary>
        /// <returns>The banned users as a comma-separated string.</returns>
        public string GetAllBans()
        {
            var bans = new StringBuilder();
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "SELECT * FROM bannedusers";
                            cmd.Prepare();
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine("GetAllBans: No banned users found in database");
                                    return string.Empty;
                                }
                                while (reader.Read())
                                {
                                    bans.Append(string.Format("{0}, ", reader["user"]));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem checking if user exists in seen database: " + ex.Message);
                }
            }
            return bans.ToString().TrimEnd(',', ' ');
        }

        /// <summary>
        ///     Creates the database.
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
                    string s =
                        "CREATE TABLE bannedusers (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, bannedBy TEXT NOT NULL, banAddDate DATETIME, banExpirationDate DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Bans database created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem creating bans database: " + ex.Message);
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
        ///     Deletes the registration database.
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
                Debug.WriteLine("Unable to delete bans database: " + ex.Message);
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
                        cmd.CommandText = "SELECT * FROM bannedusers WHERE user = @user";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                Debug.WriteLine(string.Format(
                                    "User: {0} does not exist in the bans database.", user));
                                return false;
                            }
                            Debug.WriteLine(
                                string.Format("User: {0} already exists in the bans database.",
                                    user));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem checking if user exists in bans database: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Checks whether the user is already banned.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user is already banned, otherwise <c>false</c>.</returns>
        public bool UserAlreadyBanned(string user)
        {
            return DoesUserExistInDb(user);
        }

        /// <summary>
        ///     Verifies the registration database.
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
                    cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'bannedusers'";

                    using (SQLiteDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Debug.WriteLine("bannedusers table not found in DB... Creating DB...");
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}