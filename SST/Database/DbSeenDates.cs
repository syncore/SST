using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using SST.Model;
using SST.Util;

namespace SST.Database
{
    /// <summary>
    ///     Class responsible for tracking dates and times of users seen on the server.
    /// </summary>
    public class DbSeenDates : CommonSqliteDb
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[DB:LASTSEEN]";
        private readonly string _sqlConString = "Data Source=" + Filepaths.SeenDateDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.SeenDateDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbSeenDates" /> class.
        /// </summary>
        public DbSeenDates()
        {
            VerifyDb();
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="seenDate">The date and time the user was last seen on the server.</param>
        public void AddUserToDb(string user, DateTime seenDate)
        {
            if (VerifyDb())
            {
                if (DoesUserExistInDb(user.ToLowerInvariant()))
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
                            cmd.CommandText =
                                "INSERT INTO seendates(user, seendate) VALUES(@user, @seendate)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@seendate", seenDate);
                            cmd.ExecuteNonQuery();
                            Log.Write(string.Format(
                                "{0} successfully added to last seen date database with last seen date date: {1}",
                                user, seenDate.ToString("G", DateTimeFormatInfo.InvariantInfo)),
                                _logClassType, _logPrefix);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem adding player {0} to last seen date database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                }
            }
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
                            cmd.CommandText = "DELETE FROM seendates WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Deleted user: {0} from last seen date database.", user), _logClassType,
                                    _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem deleting player {0} from last seen date database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                }
            }
        }

        /// <summary>
        ///     Gets the user's last seen date and time from the database if it exists.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's last seen date and time.</returns>
        public DateTime GetLastSeenDate(string user)
        {
            var date = new DateTime();
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "SELECT * FROM seendates WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return date;
                                }
                                while (reader.Read())
                                {
                                    date = (DateTime)reader["seendate"];
                                    Log.Write(string.Format(
                                        "Got last seen date for player {0} from last seen date database; last seen: {1}",
                                        user, date.ToString("G", DateTimeFormatInfo.InvariantInfo)),
                                        _logClassType, _logPrefix);
                                    return date;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem getting last seen date for player {0} from last seen date database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                }
            }
            return date;
        }

        /// <summary>
        ///     Updates the last seen date.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastSeenDate">The last seen date.</param>
        public void UpdateLastSeenDate(string user, DateTime lastSeenDate)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    Log.Write(string.Format(
                        "Updating player {0} in last seen date database; player didn't exist; attempting to add.",
                        user), _logClassType, _logPrefix);
                    AddUserToDb(user, DateTime.Now);
                }
                else
                {
                    try
                    {
                        using (var sqlcon = new SQLiteConnection(_sqlConString))
                        {
                            sqlcon.Open();

                            using (var cmd = new SQLiteCommand(sqlcon))
                            {
                                cmd.CommandText =
                                    "UPDATE seendates SET seendate = @seendate WHERE user = @user";
                                cmd.Prepare();
                                cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                                cmd.Parameters.AddWithValue("@seendate", lastSeenDate);
                                cmd.ExecuteNonQuery();
                                Log.Write(string.Format(
                                    "Updated player {0}'s last seen time to: {1} in last seen date database.",
                                    user,
                                    lastSeenDate.ToString("G", DateTimeFormatInfo.InvariantInfo)),
                                    _logClassType, _logPrefix);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteCritical(string.Format(
                            "Problem updating last seen date for player {0} in last seen date database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                    }
                }
            }
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

                    var s =
                        "CREATE TABLE seendates (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, seendate DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Log.Write("Last seen date database created.", _logClassType, _logPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem creating last seen date database: " + ex.Message,
                    _logClassType, _logPrefix);
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
                Log.WriteCritical("Unable to delete last seen date database: " + ex.Message,
                    _logClassType, _logPrefix);
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
                        cmd.CommandText = "SELECT * FROM seendates WHERE user = @user";
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
                    "Problem checking if player {0} exists in last seen date database: {1}",
                    user, ex.Message), _logClassType, _logPrefix);
            }
            return false;
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
                    cmd.CommandText =
                        "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'seendates'";

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Log.Write(
                            "seendates table not found in last seen date database... Creating last seen date database...",
                            _logClassType, _logPrefix);
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}