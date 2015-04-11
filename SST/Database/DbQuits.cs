using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SST.Core;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Database
{
    /// <summary>
    ///     Database class responsible for tracking players who leave games early.
    /// </summary>
    public class DbQuits : CommonSqliteDb
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[DB:EARLYQUIT]";
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
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="doublePenalty">
        ///     if set to <c>true</c> double the penalty
        ///     for particularly egregious early quits (i.e. during countdown).
        /// </param>
        /// <returns>The result of the addition operation as an <see cref="UserDbResult" /> enum value.</returns>
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
                            Log.Write(
                                string.Format("{0} successfully added to early quitter database",
                                    user), _logClassType, _logPrefix);
                            result = UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem adding player {0} to early quitter database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                    result = UserDbResult.InternalError;
                }
            }
            return result;
        }

        /// <summary>
        ///     Decrements the user quit count by a given amount.
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
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Decremented early quit count for player {0} by {1} in early quitter database.",
                                    user, amount), _logClassType, _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format(
                            "Problem decrementing quit count for player {0} in early quitter database: {1}",
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
                            cmd.CommandText = "DELETE FROM earlyquitters WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Deleted user: {0} from early quitter database.", user), _logClassType,
                                    _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem deleting player {0} from earliy quitter database: {1}",
                            user, ex.Message), _logClassType, _logPrefix);
                }
            }
        }

        /// <summary>
        ///     Gets all of the users who have quit early.
        /// </summary>
        /// <returns>The early quitters as a list of <see cref="EarlyQuitter" /> objects.</returns>
        public List<EarlyQuitter> GetAllQuitters()
        {
            var allQuitters = new List<EarlyQuitter>();
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
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return allQuitters;
                                }
                                while (reader.Read())
                                {
                                    var player = (string) reader["user"];
                                    var quitCount = GetUserQuitCount(player);
                                    allQuitters.Add(new EarlyQuitter {Name = player, QuitCount = quitCount});
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        "Problem getting all quitters from early quitter database." + ex.Message,
                        _logClassType, _logPrefix);
                }
            }
            return allQuitters;
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
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return 0;
                                }
                                while (reader.Read())
                                {
                                    quitCount = (long) reader["numQuits"];
                                    Log.Write(
                                        string.Format(
                                            "Got early quit count for player {0} from early quitter database; early quits: {1}"
                                            , user, quitCount), _logClassType, _logPrefix);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem getting early quit count for player {0} from earliy quitter database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                }
            }
            return quitCount;
        }

        /// <summary>
        ///     Increments the user quit count.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="doublePenalty">
        ///     if set to <c>true</c> double the penalty
        ///     for particularly egregious early quits (i.e. during countdown).
        /// </param>
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

                            cmd.Parameters.AddWithValue("@newQuitCount",
                                ((doublePenalty
                                    ? GetUserQuitCount(user)*2
                                    : GetUserQuitCount(user) + 1)));
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "{0} Incremented early quit count for: {1} in early quitter database.",
                                    doublePenalty ? "DOUBLE PENALTY - " : "", user), _logClassType, _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem incrementing early quit count for player {0} in earliy quitter database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
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
        ///     Removes an early quit-related ban.
        /// </summary>
        /// <param name="sst">The main class.</param>
        /// <param name="user">The user.</param>
        public async Task RemoveQuitRelatedBan(SynServerTool sst, string user)
        {
            var banDb = new DbBans();
            if (banDb.UserAlreadyBanned(user))
            {
                var bi = banDb.GetBanInfo(user);
                if (bi.BanType == BanType.AddedByEarlyQuit)
                {
                    banDb.DeleteUserFromDb(user);

                    if (sst.IsMonitoringServer)
                    {
                        await sst.QlCommands.CmdUnban(user);
                    }
                    Log.Write(string.Format("Removed early quit-related ban for player {0}.",
                        user), _logClassType, _logPrefix);
                }
            }
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
                    var s =
                        "CREATE TABLE earlyquitters (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, numQuits INTEGER)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Log.Write("Early quitter database created.", _logClassType, _logPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem creating early quitter database: " + ex.Message,
                    _logClassType, _logPrefix);
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
                Log.WriteCritical("Unable to delete early quitter database: " + ex.Message,
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
                        cmd.CommandText = "SELECT * FROM earlyquitters WHERE user = @user";
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
                    "Problem checking if player {0} exists in earliy quitter database: {1}",
                    user, ex.Message), _logClassType, _logPrefix);
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

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Log.Write(
                            "earlyquitters table not found in early quitter database... Creating early quitter database...",
                            _logClassType, _logPrefix);
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}