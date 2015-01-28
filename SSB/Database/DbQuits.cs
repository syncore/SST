using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using SSB.Enum;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Database class responsible for tracking players who leave games early.
    /// </summary>
    public class DbQuits : CommonSqliteDb
    {
        private readonly string _sqlConString = "Data Source=" + Filepaths.QuitDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.QuitDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbQuits" /> class.
        /// </summary>
        public DbQuits()
        {
            VerifyDb();
        }

        /// <summary>
        /// Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="doublePenalty">if set to <c>true</c> double the penalty
        /// for particularly egregious early quits (i.e. during countdown).</param>
        /// <returns>The result of the addition operation as an <see cref="UserDbResult"/> enum value.</returns>
        public UserDbResult AddUserToDb(string user, bool doublePenalty)
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
                                "INSERT INTO earlyquitters(user, numQuits) VALUES(@user, @numQuits)";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@numQuits", (doublePenalty ? 2 : 1));
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine(
                                string.Format("AddEarlyQuitDb: {0} successfully added to early quitter DB",
                                    user));
                            result = UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding user to early quitter database: " + ex.Message);
                    result = UserDbResult.InternalError;
                }
            }
            return result;
        }

        /// <summary>
        /// Decrements the user quit count by a given amount.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="amount">The amount of quits by which to decrement.</param>
        /// <remarks>This is typically used when an admin chooses to forgive a number of early quits for a user.</remarks>
        public void DecrementUserQuitCount(string user, int amount)
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
                            cmd.CommandText =
                                "UPDATE earlyquitters SET numQuits = @newQuitCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());

                            cmd.Parameters.AddWithValue("@newQuitCount", (GetUserQuitCount(user) - amount));
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(
                                    "Decremented early quit count for: {0} by {1} from early quitter database.",
                                    user, amount);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem decrementing user quit count for {0} in early quitters database: {1}", user,
                        ex.Message);
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
                            cmd.CommandText = "DELETE FROM earlyquitters WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from early quitter database.", user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem deleting user from early quitter database: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets all of the users who have quit early.
        /// </summary>
        /// <returns>The early quitters as a comma-separated string.</returns>
        public string GetAllQuits()
        {
            var quits = new StringBuilder();
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "SELECT * FROM earlyquitters";
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine("GetAllQuits: No earlyquit users found in database");
                                    return string.Empty;
                                }
                                while (reader.Read())
                                {
                                    var player = (string)reader["user"];
                                    quits.Append(string.Format("{0}({1}), ", player, GetUserQuitCount(player)));
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
            return quits.ToString().TrimEnd(',', ' ');
        }

        /// <summary>
        ///     Gets the user quit count.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user quit counts</returns>
        public long GetUserQuitCount(string user)
        {
            long quitCount = 0;
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return 0;
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "SELECT * FROM earlyquitters WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetUserQuitCount: User does not exist in early quitter database.");
                                    return 0;
                                }
                                while (reader.Read())
                                {
                                    quitCount = (long)reader["numQuits"];
                                    Debug.WriteLine("GetUserQuitCount: {0} has {1} early quits.", user,
                                        quitCount);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem getting quit count from early quitters database: " + ex.Message);
                }
            }
            return quitCount;
        }

        /// <summary>
        /// Increments the user quit count.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="doublePenalty">if set to <c>true</c> double the penalty
        /// for particularly egregious early quits (i.e. during countdown).</param>
        public void IncrementUserQuitCount(string user, bool doublePenalty)
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
                            cmd.CommandText =
                                "UPDATE earlyquitters SET numQuits = @newQuitCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());

                            cmd.Parameters.AddWithValue("@newQuitCount", ((doublePenalty ? GetUserQuitCount(user) * 2
                                : GetUserQuitCount(user) + 1)));
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine("{0} Incremented early quit count for: {1} from early quitter database.",
                                    doublePenalty ? "[DOUBLE PENALTY]" : "", user);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem incrementing user quit count for {0} in early quitters database: {1}", user,
                        ex.Message);
                }
            }
        }

        /// <summary>
        ///     Initializes the database.
        /// </summary>
        public void InitDb()
        {
            // Initialize so DB gets created on first run
            VerifyDb();
        }

        /// <summary>
        ///     Checks whether the user already exists in the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user already exists, otherwise <c>false</c>.</returns>
        public bool UserExistsInDb(string user)
        {
            return DoesUserExistInDb(user);
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
                        "CREATE TABLE earlyquitters (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, numQuits INTEGER)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("EarlyQuitter database created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem creating early quitter database: " + ex.Message);
                DeleteDb();
            }
        }

        /// <summary>
        ///     Checks whether the early quit database exists.
        /// </summary>
        /// <returns><c>true</c>if the user database exists, otherwise <c>false</c>.</returns>
        protected override bool DbExists()
        {
            return (File.Exists(_sqlDbPath));
        }

        /// <summary>
        ///     Deletes the early quit database.
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
                Debug.WriteLine("Unable to delete early quitter database: " + ex.Message);
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
                        cmd.CommandText = "SELECT * FROM earlyquitters WHERE user = @user";
                        cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            return reader.HasRows;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem checking if user exists in early quitter database: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        ///     Verifies the quit database.
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
                        "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'earlyquitters'";

                    using (SQLiteDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Debug.WriteLine("earlyquitters table not found in DB... Creating DB...");
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}