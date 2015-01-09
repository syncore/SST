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
    ///     Class responsible for user database operations.
    /// </summary>
    public class DbRegistrationDates : CommonSqliteDb
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.AccountDateDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.AccountDateDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbRegistrationDates" /> class.
        /// </summary>
        public DbRegistrationDates()
        {
            VerifyDb();
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="registrationDate">The user's QL account registration date.</param>
        public void AddUserToDb(string user, DateTime registrationDate)
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
                                "INSERT INTO regdates(user, acctdate) VALUES(@user, @acctdate)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            cmd.Parameters.AddWithValue("@acctdate", registrationDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to registration database: " + ex.Message);
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
                            cmd.CommandText = "DELETE FROM regdates WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user);
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from registration database.", user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem deleting user from registration database: " + ex.Message);
                }
            }
        }

        /// <summary>
        ///     Gets the user's registration date from the database if it exists.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's registration date.</returns>
        public DateTime GetRegistrationDate(string user)
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
                            cmd.CommandText = "SELECT * FROM regdates WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(string.Format(
                                        "User: {0} does not exist in the registration database.", user));
                                    return date;
                                }
                                while (reader.Read())
                                {
                                    date = (DateTime)reader["acctdate"];
                                    Debug.WriteLine(
                                        "Got registration date {0} for User: {1} from internal database.",
                                        date, user);
                                    return date;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem checking if user exists in registration database: " + ex.Message);
                }
            }
            return date;
        }

        /// <summary>
        ///     Creates the registration database.
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
                        "CREATE TABLE regdates (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, acctdate DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Registration database created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem creating registration database: " + ex.Message);
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
                Debug.WriteLine("Unable to delete registration database: " + ex.Message);
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
                        cmd.CommandText = "SELECT * FROM regdates WHERE user = @user";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                Debug.WriteLine(string.Format(
                                    "User: {0} does not exist in the registration database.", user));
                                return false;
                            }
                            Debug.WriteLine(
                                string.Format("User: {0} already exists in the registration database.",
                                    user));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem checking if user exists in registration database: " + ex.Message);
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
                    cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'regdates'";

                    using (SQLiteDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Debug.WriteLine("regdates table not found in DB... Creating DB...");
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}