using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using SSB.Enums;
using SSB.Model;
using SSB.Model.QlRanks;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Database class responsible for QLRanks elo data for players.
    /// </summary>
    public class DbElo : CommonSqliteDb
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[DB]";
        private readonly string _sqlConString = "Data Source=" + Filepaths.EloDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.EloDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbElo" /> class.
        /// </summary>
        public DbElo()
        {
            VerifyDb();
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastUpdatedDate">The date and time on which the elo data was last updated.</param>
        /// <param name="caElo">The CA elo.</param>
        /// <param name="ctfElo">The CTF elo.</param>
        /// <param name="duelElo">The Duel elo.</param>
        /// <param name="ffaElo">The FFA elo.</param>
        /// <param name="tdmElo">The TDM elo.</param>
        /// <returns>A <see cref="UserDbResult" /> value indicating the result of the attempted user addition.</returns>
        public UserDbResult AddUserToDb(string user, DateTime lastUpdatedDate, long caElo, long ctfElo,
            long duelElo, long ffaElo, long tdmElo)
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
                                "INSERT INTO elo(user, lastUpdated, caElo, ctfElo, duelElo, ffaElo, tdmElo) VALUES(@user," +
                                " @lastUpdatedDate, @caElo, @ctfElo, @duelElo, @ffaElo, @tdmElo)";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@lastUpdatedDate", lastUpdatedDate);
                            cmd.Parameters.AddWithValue("@caElo", caElo);
                            cmd.Parameters.AddWithValue("@ctfElo", ctfElo);
                            cmd.Parameters.AddWithValue("@duelElo", duelElo);
                            cmd.Parameters.AddWithValue("@ffaElo", ffaElo);
                            cmd.Parameters.AddWithValue("@tdmElo", tdmElo);
                            cmd.ExecuteNonQuery();
                            Log.Write(string.Format(
                                "{0} successfully added to Elo database. Elo last updated:" +
                                " {1}. CA: {2} | CTF: {3} | DUEL: {4} | FFA: {5} | TDM: {6}",
                                user, lastUpdatedDate.ToString("G", DateTimeFormatInfo.InvariantInfo), caElo,
                                ctfElo, duelElo, ffaElo, tdmElo), _logClassType, _logPrefix);
                            result = UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format("Problem adding player {0} to Elo database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                    result = UserDbResult.InternalError;
                }
            }
            return result;
        }

        /// <summary>
        ///     Gets the elo data for a given player.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The player's elo data as an <see cref="EloData" /> object.</returns>
        public EloData GetEloData(string user)
        {
            var eloData = new EloData();
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
                            cmd.CommandText = "SELECT * FROM elo WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    eloData.LastUpdatedDate = (DateTime)reader["lastUpdated"];
                                    eloData.CaElo = (long)reader["caElo"];
                                    eloData.CtfElo = (long)reader["ctfElo"];
                                    eloData.DuelElo = (long)reader["duelElo"];
                                    eloData.FfaElo = (long)reader["ffaElo"];
                                    eloData.TdmElo = (long)reader["tdmElo"];
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem getting Elo data for player {0} from Elo database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                }
            }
            return eloData;
        }

        /// <summary>
        ///     Updates a player's elo data in the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastUpdatedDate">The date and time on which the elo data was last updated.</param>
        /// <param name="caElo">The CA elo.</param>
        /// <param name="ctfElo">The CTF elo.</param>
        /// <param name="duelElo">The Duel elo.</param>
        /// <param name="ffaElo">The FFA elo.</param>
        /// <param name="tdmElo">The TDM elo.</param>
        public void UpdateEloData(string user, DateTime lastUpdatedDate, long caElo, long ctfElo, long duelElo,
            long ffaElo, long tdmElo)
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
                                "UPDATE elo SET lastUpdated = @lastUpdatedDate, caElo = @caElo, ctfElo = @ctfElo," +
                                " duelElo = @duelElo, ffaElo = @ffaElo, tdmElo = @tdmElo WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@lastUpdatedDate", lastUpdatedDate);
                            cmd.Parameters.AddWithValue("@caElo", caElo);
                            cmd.Parameters.AddWithValue("@ctfElo", ctfElo);
                            cmd.Parameters.AddWithValue("@duelElo", duelElo);
                            cmd.Parameters.AddWithValue("@ffaElo", ffaElo);
                            cmd.Parameters.AddWithValue("@tdmElo", tdmElo);
                            int total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Successfully updated QLRanks Elo database data for user: {0}. Elo last updated: {1}." +
                                    " CA: {2} | CTF: {3} | DUEL: {4} | FFA: {5} | TDM: {6}",
                                    user, lastUpdatedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                                    caElo, ctfElo, duelElo, ffaElo, tdmElo), _logClassType, _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format(
                        "Problem updating QLRanks Elo data for player {0} in Elo database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                }
            }
        }

        /// <summary>
        ///     Checks whether the user is already banned.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user is already banned, otherwise <c>false</c>.</returns>
        public bool UserAlreadyExists(string user)
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
                        "CREATE TABLE elo (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL," +
                        " lastUpdated DATETIME, caElo INTEGER, ctfElo INTEGER, duelElo INTEGER," +
                        " ffaElo INTEGER, tdmElo INTEGER)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Log.Write("Elo database created.", _logClassType, _logPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem creating Elo database: " + ex.Message,
                    _logClassType, _logPrefix);
                DeleteDb();
            }
        }

        /// <summary>
        ///     Checks whether the ban database exists.
        /// </summary>
        /// <returns><c>true</c>if the user database exists, otherwise <c>false</c>.</returns>
        protected override bool DbExists()
        {
            return (File.Exists(_sqlDbPath));
        }

        /// <summary>
        ///     Deletes the ban database.
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
                Log.WriteCritical("Unable to delete Elo database: " + ex.Message,
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
                        cmd.CommandText = "SELECT * FROM elo WHERE user = @user";
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
                Log.WriteCritical(string.Format("Problem checking if player {0} exists in Elo database: {1}",
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
                    cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'elo'";

                    using (SQLiteDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Log.WriteCritical("elo table not found in Elo database... Creating Elo database...",
                            _logClassType, _logPrefix);
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}