using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Class responsible for user database operations.
    /// </summary>
    public class DbRegistrationDates : CommonSqliteDb
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[DB]";
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
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@acctdate", registrationDate);
                            cmd.ExecuteNonQuery();
                            Log.Write(string.Format(
                                "{0} successfully added to registration date database with registration date: {1}",
                                user, registrationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)),
                                _logClassType, _logPrefix);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem adding player {0} to registration date database: {1}",
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
                            cmd.CommandText = "DELETE FROM regdates WHERE user = @user";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Deleted user: {0} from registration date database.", user), _logClassType,
                                    _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem deleting player {0} from registration date database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
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
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return date;
                                }
                                while (reader.Read())
                                {
                                    date = (DateTime) reader["acctdate"];
                                    Log.Write(string.Format(
                                        "Got registration date for player {0} from registration database; registration date: {1}",
                                        user, date.ToString("G", DateTimeFormatInfo.InvariantInfo)), _logClassType, _logPrefix);
                                    return date;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem getting registration date for player {0} from registration date database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
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

                    var s =
                        "CREATE TABLE regdates (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, acctdate DATETIME)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Log.Write("Registration database created.", _logClassType, _logPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem creating registration date database: " + ex.Message,
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
                Log.WriteCritical("Unable to delete registration date database: " + ex.Message,
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
                        cmd.CommandText = "SELECT * FROM regdates WHERE user = @user";
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
                    "Problem checking if player {0} exists in registration date database: {1}",
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
                    cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'regdates'";

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Log.Write(
                            "regdates table not found in registration date database... Creating registration date database...",
                            _logClassType, _logPrefix);
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}