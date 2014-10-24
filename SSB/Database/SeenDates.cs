using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Class responsible for tracking dates and times of users seen on the server.
    /// </summary>
    public class SeenDates : CommonSqliteDb
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.SeenDateDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.SeenDateDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SeenDates" /> class.
        /// </summary>
        public SeenDates()
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
                            cmd.Parameters.AddWithValue("@user", user);
                            cmd.Parameters.AddWithValue("@seendate", seenDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to seen database: " + ex.Message);
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
                            cmd.Parameters.AddWithValue("@user", user);
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from seen database.", user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem deleting user from seen database: " + ex.Message);
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
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(string.Format(
                                        "User: {0} does not exist in the seen database.", user));
                                    return date;
                                }
                                while (reader.Read())
                                {
                                    date = (DateTime)reader["seendate"];
                                    Debug.WriteLine(
                                        "Got last seen date {0} for User: {1} from internal database.",
                                        date, user);
                                    return date;
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
            return date;
        }

        /// <summary>
        /// Updates the last seen date.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastSeenDate">The last seen date.</param>
        public void UpdateLastSeenDate(string user, DateTime lastSeenDate)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    Debug.WriteLine(string.Format("Updating {0} in seen database: {0} did not exist in database, attempting to add.", user));
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
                                cmd.CommandText = "UPDATE seendates SET seendate = @seendate WHERE user = @user";
                                cmd.Prepare();
                                cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                                cmd.Parameters.AddWithValue("@seendate", lastSeenDate);
                                cmd.ExecuteNonQuery();
                                Debug.WriteLine("Updated existing user {0}'s last seen time to: {1}", user, lastSeenDate);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Problem updating user: {0} in seen database: {1}", user, ex.Message);
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

                    string s =
                        "CREATE TABLE seendates (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, seendate DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Seen database created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem creating seenn database: " + ex.Message);
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
                Debug.WriteLine("Unable to delete seen database: " + ex.Message);
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
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                Debug.WriteLine(string.Format(
                                    "User: {0} does not exist in the seen database.", user));
                                return false;
                            }
                            Debug.WriteLine(
                                string.Format("User: {0} already exists in the seen database.",
                                    user));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem checking if user exists in seen database: " + ex.Message);
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
                    cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'seendates'";

                    using (SQLiteDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Debug.WriteLine("seendates table not found in DB... Creating DB...");
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}