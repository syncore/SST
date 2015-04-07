using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using SSB.Enums;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Database class responsible for tracking banned players.
    /// </summary>
    public class DbBans : CommonSqliteDb
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[DB]";
        private readonly string _sqlConString = "Data Source=" + Filepaths.BanDatabaseFilePath;
        private readonly string _sqlDbPath = Filepaths.BanDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbBans" /> class.
        /// </summary>
        public DbBans()
        {
            VerifyDb();
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="bannedBy">The admin who added the ban.</param>
        /// <param name="banAddDate">The date and time that the ban was added.</param>
        /// <param name="banExpirationDate">The date and time that the user's ban will expire.</param>
        /// <param name="banType">The type of ban.</param>
        /// <returns></returns>
        public UserDbResult AddUserToDb(string user, string bannedBy, DateTime banAddDate,
            DateTime banExpirationDate, BanType banType)
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
                                "INSERT INTO bannedusers(user, bannedBy, banAddDate, banExpirationDate, banType)" +
                                " VALUES(@user, @bannedBy, @banAddDate, @banExpirationDate, @banType)";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@bannedBy", bannedBy);
                            cmd.Parameters.AddWithValue("@banAddDate", banAddDate);
                            cmd.Parameters.AddWithValue("@banExpirationDate", banExpirationDate);
                            cmd.Parameters.AddWithValue("@banType", (long)banType);
                            cmd.ExecuteNonQuery();
                            Log.Write(string.Format(
                                "{0} successfully added to ban database by: {1} on: {2}. Time-ban expires on: {3}.",
                                user, bannedBy, banAddDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                                banExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)), _logClassType,
                                _logPrefix);
                            result = UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format("Problem adding player {0} to ban database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
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
                if (!DoesUserExistInDb(user.ToLowerInvariant())) return;
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText = "DELETE FROM bannedusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Log.Write(string.Format(
                                    "Deleted player: {0} from ban database.", user), _logClassType, _logPrefix);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem deleting player {0} from ban database: {1}",
                        user, ex.Message),
                        _logClassType, _logPrefix);
                }
            }
        }

        /// <summary>
        ///     Gets all of the bans in the ban database.
        /// </summary>
        /// <returns>The users as a list of <see cref="BanInfo" /> objects.</returns>
        public List<BanInfo> GetAllBans()
        {
            var allBans = new List<BanInfo>();
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
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return allBans;
                                }
                                while (reader.Read())
                                {
                                    var user = (string)reader["user"];
                                    var baninfo = GetBanInfo(user);
                                    if (baninfo != null)
                                    {
                                        allBans.Add(baninfo);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(string.Format("Problem retrieving all bans from ban database: {0}",
                        ex.Message), _logClassType, _logPrefix);
                }
            }
            return allBans;
        }

        /// <summary>
        ///     Gets the ban information.
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
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Log.Write(string.Format("GetBanInfo: Player {0} does not exist in ban database.",
                                        user), _logClassType, _logPrefix);
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    bInfo.PlayerName = user;
                                    bInfo.BannedBy = (string)reader["bannedBy"];
                                    bInfo.BanAddedDate = (DateTime)reader["banAddDate"];
                                    bInfo.BanExpirationDate = (DateTime)reader["banExpirationDate"];
                                    switch ((long)reader["banType"])
                                    {
                                        case (long)BanType.AddedByAdmin:
                                            bInfo.BanType = BanType.AddedByAdmin;
                                            break;

                                        case (long)BanType.AddedByEarlyQuit:
                                            bInfo.BanType = BanType.AddedByEarlyQuit;
                                            break;
                                    }
                                    Log.Write(string.Format(
                                        "Got ban info for player: {0} - added by: {1}, added on: {2}, expires: {3}, type: {4}",
                                        user, bInfo.BannedBy, bInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                                        bInfo.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                                        bInfo.BanType), _logClassType, _logPrefix);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteCritical(
                        string.Format("Problem getting ban info for player {0} from ban database: {1}",
                        user, ex.Message), _logClassType, _logPrefix);
                    bInfo = null;
                }
            }

            return bInfo;
        }

        /// <summary>
        ///     Determines whether the existing ban is still valid, if it exists, for the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="banInfo">The ban information.</param>
        /// <returns>
        ///     <c>true</c> if the existing ban's expiration date has not passed,
        ///     <c>false</c> is ban has expired or does not exist.
        /// </returns>
        public bool IsExistingBanStillValid(string user, out BanInfo banInfo)
        {
            banInfo = GetBanInfo(user);
            if (banInfo == null) return false;
            return DateTime.Now <= banInfo.BanExpirationDate;
        }

        /// <summary>
        ///     Removes any expired bans, and if ban was set by a module with ability
        ///     to set bans, reset any module-specific properties.
        /// </summary>
        /// <remarks>
        /// If access to the user interface or the underlying main bot class are needed
        /// (i.e. to send an unban command directly to the game), see the RemoveBan method
        /// in the BanManager class since SSB database classes are given
        /// no access to the main class nor the user interface.
        /// </remarks>
        public void RemoveExpiredBans()
        {
            var allExpiredBans = GetAllBans().Where
                (b => (b.BanExpirationDate != default(DateTime) &&
                       DateTime.Now > b.BanExpirationDate)).ToList();

            if (allExpiredBans.Count == 0) return;

            Log.Write(string.Format("Found {0} expired bans, will attempt to remove.",
                allExpiredBans.Count), _logClassType, _logPrefix);

            foreach (var ban in allExpiredBans)
            {
                if (ban.BanType == BanType.AddedByEarlyQuit)
                {
                    var eQuitDb = new DbQuits();
                    eQuitDb.DeleteUserFromDb(ban.PlayerName);
                }
                if (ban.BanType == BanType.AddedByPickupSubs)
                {
                    var pickupDb = new DbPickups();
                    pickupDb.ResetSubsUsedCount(ban.PlayerName);
                }
                if (ban.BanType == BanType.AddedByPickupNoShows)
                {
                    var pickupDb = new DbPickups();
                    pickupDb.ResetNoShowCount(ban.PlayerName);
                }

                DeleteUserFromDb(ban.PlayerName);
            }
        }

        /// <summary>
        ///     Checks whether the user is already banned.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the user is already banned, otherwise <c>false</c>.</returns>
        public bool UserAlreadyBanned(string user)
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
                        "CREATE TABLE bannedusers (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL," +
                        " bannedBy TEXT NOT NULL, banAddDate DATETIME, banExpirationDate DATETIME, banType INTEGER)";
                    using (var cmd = new SQLiteCommand(s, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Log.Write("Ban database created.", _logClassType, _logPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteCritical("Problem creating ban database: " + ex.Message,
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
                Log.WriteCritical("Unable to delete ban database: " + ex.Message,
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
                        cmd.CommandText = "SELECT * FROM bannedusers WHERE user = @user";
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
                Log.WriteCritical("Problem checking if user exists in ban database: " + ex.Message,
                    _logClassType, _logPrefix);
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
                        "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'bannedusers'";

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            return true;
                        }
                        Log.WriteCritical(
                            "bannedusers table not found in ban database... Creating ban database...",
                            _logClassType, _logPrefix);
                        CreateDb();
                        return false;
                    }
                }
            }
        }
    }
}