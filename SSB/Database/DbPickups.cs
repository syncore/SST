using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using SSB.Enums;
using SSB.Model;
using SSB.Util;

namespace SSB.Database
{
    /// <summary>
    ///     Database class responsible for tracking pickup games and users' pickup games.
    /// </summary>
    public class DbPickups : CommonSqliteDb
    {
        private readonly string _sqlConString = "Data Source=" +
                                                Filepaths
                                                    .PickupGameDatabaseFilePath;

        private readonly string _sqlDbPath =
            Filepaths.PickupGameDatabaseFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbPickups" /> class.
        /// </summary>
        public DbPickups()
        {
            VerifyDb();
        }

        /// <summary>
        ///     Adds the pickup game to the database.
        /// </summary>
        /// <param name="pInfo">The pickup game information.</param>
        /// <remarks>Note: The game is not added to the database until both red & blue teams are full after
        /// captains have picked players.</remarks>
        public void AddPickupGame(PickupInfo pInfo)
        {
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "INSERT INTO pickupgames(redTeam, blueTeam, redCaptain, blueCaptain, subs, noShows, startDate) VALUES(@redTeam, @blueTeam, @redCaptain, " +
                                "@blueCaptain, @subs, @noShows, @startDate)";
                            cmd.Parameters.AddWithValue("@redTeam",
                                pInfo.RedTeam);
                            cmd.Parameters.AddWithValue("@blueTeam",
                                pInfo.BlueTeam);
                            cmd.Parameters.AddWithValue("@redCaptain",
                                pInfo.RedCaptain);
                            cmd.Parameters.AddWithValue("@blueCaptain",
                                pInfo.BlueCaptain);
                            cmd.Parameters.AddWithValue("@subs", pInfo.Subs);
                            cmd.Parameters.AddWithValue("@noShows",
                                pInfo.NoShows);
                            cmd.Parameters.AddWithValue("@startDate",
                                pInfo.StartDate);
                            // default end time. Use UpdatePickupEndTime to change
                            cmd.Parameters.AddWithValue("@endDate",
                                default(DateTime));
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine(
                                "AddPickupGame: Successfully added pickup game: Red team: {0}, Blue Team: {1}, Red captain: {2}, Blue captain:" +
                                " {3}, Subs: {4}, No-shows: {5}, Starting at: {6} to pickup database.",
                                pInfo.RedTeam, pInfo.BlueTeam, pInfo.RedCaptain,
                                pInfo.BlueCaptain, pInfo.Subs,
                                pInfo.NoShows,
                                pInfo.StartDate.ToString("G",
                                    DateTimeFormatInfo.InvariantInfo));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding pickup game to database: " +
                                    ex.Message);
                }
            }
        }

        /// <summary>
        ///     Adds the user to the database.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="lastPlayedDate">The date and time of the user's last played pickup game</param>
        /// <returns>The result of the addition operation as an <see cref="UserDbResult" /> enum value.</returns>
        public UserDbResult AddUserToDb(string user, DateTime lastPlayedDate)
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
                                "INSERT INTO pickupusers(user, subsUsed, noShows, gamesStarted, gamesFinished, lastPlayedDate) VALUES(@user, " +
                                "@subsUsed, @noShows, @gamesStarted, @gamesFinished, @lastPlayedDate)";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@subsUsed", 0);
                            cmd.Parameters.AddWithValue("@noShows", 0);
                            cmd.Parameters.AddWithValue("@gamesStarted", 0);
                            cmd.Parameters.AddWithValue("@gamesFinished", 0);
                            cmd.Parameters.AddWithValue("@lastPlayedDate",
                                lastPlayedDate);
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine(
                                string.Format(
                                    "AddUserToPickupDb: {0} successfully added to pickup user DB",
                                    user));
                            result = UserDbResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem adding user to pickup user database: " +
                        ex.Message);
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
                            cmd.CommandText =
                                "DELETE FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "Deleted user: {0} from pickup users database.",
                                    user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem deleting user from pickup users database: " +
                        ex.Message);
                }
            }
        }

        /// <summary>
        ///     Gets the information for the last pickup played.
        /// </summary>
        /// <returns>The information for the last pickup game played, if any.</returns>
        public PickupInfo GetLastPickupInfo()
        {
            var pInfo = new PickupInfo();
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "SELECT * FROM pickupgames ORDER BY startDate DESC LIMIT 1";
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetLastPickup: No games exist in the pickup database.");
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    pInfo.RedTeam = (string)reader["redTeam"];
                                    pInfo.BlueTeam = (string)reader["blueTeam"];
                                    pInfo.RedCaptain = (string)reader["redCaptain"];
                                    pInfo.BlueCaptain =
                                        (string)reader["blueCaptain"];
                                    pInfo.Subs = (string)reader["subs"];
                                    pInfo.NoShows = (string)reader["noShows"];
                                    pInfo.StartDate =
                                        (DateTime)reader["startDate"];
                                    //var startDateStr = ((startDate !=
                                    //                     default(DateTime))
                                    //    ? startDate.ToString("G",
                                    //        DateTimeFormatInfo.InvariantInfo)
                                    //    : string.Empty);
                                    //pugInfo.Append(
                                    //    string.Format(
                                    //        "^7{0}: ^1{1} (cap: {2}), ^5{3} (cap: {4}),^3 Subs: {5}^6 No-shows: {6}",
                                    //        startDateStr, redTeam, redCapt,
                                    //        blueTeam, blueCapt, subs, noShows));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem getting last pickup game: info from pickup database: " +
                        ex.Message);
                }
            }
            return pInfo;
        }

        /// <summary>
        ///     Gets the ten users with the most played games.
        /// </summary>
        /// <returns>The ten users with the most played games as comma-separated string.</returns>
        public string GetTopTenUsers()
        {
            var users = new StringBuilder();
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "SELECT * FROM pickupusers ORDER BY gamesFinished DESC LIMIT 10";
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetTopTenUsers: No users found in pickup usersdatabase");
                                    return string.Empty;
                                }
                                while (reader.Read())
                                {
                                    var player = (string)reader["user"];
                                    var gamesPlayed =
                                        GetUserGamesPlayedCount(player);
                                    users.Append(string.Format("{0}({1}), ",
                                        player, gamesPlayed));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem checking if user exists in seen database: " +
                        ex.Message);
                }
            }
            return users.ToString().TrimEnd(',', ' ');
        }

        /// <summary>
        ///     Gets the user's games played count.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's games played count.</returns>
        public long GetUserGamesPlayedCount(string user)
        {
            long gamesCount = 0;
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
                            cmd.CommandText =
                                "SELECT * FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetGamesPlayedCount: User does not exist in pickup users database.");
                                    return 0;
                                }
                                while (reader.Read())
                                {
                                    gamesCount = (long)reader["gamesFinished"];
                                    Debug.WriteLine(
                                        "GetGamesPlayedCount: {0} has finished {1} pickup games.",
                                        user, gamesCount);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem getting games played count from pickup users database: " +
                        ex.Message);
                }
            }
            return gamesCount;
        }

        /// <summary>
        ///     Gets the user's no show count.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's no show count.</returns>
        public long GetUserNoShowCount(string user)
        {
            long noShowCount = 0;
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
                            cmd.CommandText =
                                "SELECT * FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetNoShowCount: User does not exist in pickup users database.");
                                    return 0;
                                }
                                while (reader.Read())
                                {
                                    noShowCount = (long)reader["noShows"];
                                    Debug.WriteLine(
                                        "GetNoShowCount: {0} has failed to show up for {1} pickup games.",
                                        user,
                                        noShowCount);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem getting no show count from pickup users database: " +
                        ex.Message);
                }
            }
            return noShowCount;
        }

        /// <summary>
        ///     Gets the user's pickup information.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's pickup information as a string.</returns>
        public string GetUserPickupInfo(string user)
        {
            var userInfo = new StringBuilder();
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    return string.Empty;
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "SELECT * FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetPickupInfo: User does not exist in pickup users database.");
                                    return string.Empty;
                                }
                                while (reader.Read())
                                {
                                    var subCount = (long)reader["subsUsed"];
                                    var noShowCount = (long)reader["noShows"];
                                    var gamesStartedCount =
                                        (long)reader["gamesStarted"];
                                    var gamesFinishedCount =
                                        (long)reader["gamesFinished"];
                                    var completionPercentage =
                                        ((gamesFinishedCount != 0)
                                            ? (gamesStartedCount /
                                               gamesFinishedCount * 100)
                                            : 0);
                                    var lastPlayedGameDate =
                                        (DateTime)reader["lastPlayedDate"];
                                    var lastPlayedStr = lastPlayedGameDate.ToString("G",
                                        DateTimeFormatInfo.InvariantInfo);
                                    userInfo.Append(
                                        string.Format(
                                            "^3subs used: {0}^7 | ^1no-shows: {1}^7 | ^2games: started {2}, finished {3}^7 (^5{4}%^7), last: {5}",
                                            subCount, noShowCount,
                                            gamesStartedCount,
                                            gamesFinishedCount,
                                            completionPercentage,
                                            lastPlayedStr));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem getting user: {0}'s info from pickup users database: {1}",
                        user, ex.Message);
                }
            }
            return userInfo.ToString();
        }

        /// <summary>
        ///     Gets the user's subs used count.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's subs used count.</returns>
        public long GetUserSubsUsedCount(string user)
        {
            long subsUsedCount = 0;
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
                            cmd.CommandText =
                                "SELECT * FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "GetSubsUsedCount: User does not exist in pickup users database.");
                                    return 0;
                                }
                                while (reader.Read())
                                {
                                    subsUsedCount = (long)reader["subsUsed"];
                                    Debug.WriteLine(
                                        "GetSubsUsedCount: {0} has used {1} substitute players.",
                                        user,
                                        subsUsedCount);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem getting subs used count from pickup users database: " +
                        ex.Message);
                }
            }
            return subsUsedCount;
        }

        /// <summary>
        /// Increments the user's games finished count.
        /// </summary>
        /// <param name="user">The user.</param>
        public void IncrementUserGamesFinishedCount(string user)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    AddUserToDb(user, DateTime.Now);
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        long gameFinishedCount = 0;

                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "SELECT * FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "IncrementUserGamesFinishedCount: user not found");
                                    return;
                                }
                                while (reader.Read())
                                {
                                    gameFinishedCount = (long)reader["gamesFinished"];
                                }
                            }
                        }

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupusers SET gamesFinished = @newGamesFinishedCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@newGamesFinishedCount",
                                gameFinishedCount + 1);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format("Incremented games finished count for: {0} in pickup users database.",
                                    user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem incrementing games finished count for {0} in pickup users database: {1}",
                        user,
                        ex.Message);
                }
            }
        }

        /// <summary>
        /// Increments the user's games started count.
        /// </summary>
        /// <param name="user">The user.</param>
        public void IncrementUserGamesStartedCount(string user)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    AddUserToDb(user, DateTime.Now);
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        long gameStartedCount = 0;

                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "SELECT * FROM pickupusers WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Debug.WriteLine(
                                        "IncrementUserGamesStartedCount: user not found");
                                    return;
                                }
                                while (reader.Read())
                                {
                                    gameStartedCount = (long)reader["gamesStarted"];
                                }
                            }
                        }

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupusers SET gamesStarted = @newGamesStartedCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@newGamesStartedCount",
                                gameStartedCount + 1);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format("Incremented games started count for: {0} in pickup users database.",
                                    user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem incrementing games started count for {0} in pickup users database: {1}",
                        user,
                        ex.Message);
                }
            }
        }

        /// <summary>
        ///     Increments the user's no-show count.
        /// </summary>
        /// <param name="user">The user.</param>
        public void IncrementUserNoShowCount(string user)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    AddUserToDb(user, DateTime.Now);
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupusers SET noShows = @newNoShowCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@newNoShowCount",
                                GetUserNoShowCount(user) + 1);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format("Incremented no-show count for: {0} in pickup users database.",
                                    user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem incrementing no-show count for {0} in pickup users database: {1}",
                        user,
                        ex.Message);
                }
            }
        }

        /// <summary>
        ///     Increments the user's subs used count.
        /// </summary>
        /// <param name="user">The user.</param>
        public void IncrementUserSubsUsedCount(string user)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    AddUserToDb(user, DateTime.Now);
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupusers SET subsUsed = @newSubsUsedCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@newSubsUsedCount",
                                GetUserSubsUsedCount(user) + 1);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format("Incremented subs used count for: {0} in pickup users database.",
                                    user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem incrementing subs used count for {0} in pickup users database: {1}",
                        user,
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
        /// Resets the no show count for a user to zero.
        /// </summary>
        /// <param name="user">The user.</param>
        public void ResetNoShowCount(string user)
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
                                "UPDATE pickupusers SET noShows = @noShows WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@noShows", 0);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(
                                    "{0} Reset no-shows count for: {1} to zero in pickup users database.",
                                    user);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem resetting no-show count for {0} in pickup users database: {1}",
                        user, ex.Message);
                }
            }
        }

        /// <summary>
        /// Resets the subs used count for a user to zero.
        /// </summary>
        /// <param name="user">The user.</param>
        public void ResetSubsUsedCount(string user)
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
                                "UPDATE pickupusers SET subsUsed = @newSubsUsedCount WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("@newSubsUsedCount", 0);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(
                                    "{0} Reset subs used count for: {1} to zero in pickup users database.",
                                    user);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem resetting subs used count for {0} in pickup users database: {1}",
                        user, ex.Message);
                }
            }
        }

        /// <summary>
        ///     Updates the most recent pickup game.
        /// </summary>
        /// <param name="pInfo">The pickup information.</param>
        public void UpdateMostRecentPickupGame(PickupInfo pInfo)
        {
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupgames SET redTeam = @redTeam, blueTeam = @blueTeam, redCaptain = @redCaptain, " +
                                "blueCaptain = @blueCaptain, subs = @subs, noShows = @noShows, startDate = @startDate WHERE id IN (SELECT id FROM pickupgames ORDER BY startDate DESC LIMIT 1)";
                            cmd.Parameters.AddWithValue("@redTeam", pInfo.RedTeam);
                            cmd.Parameters.AddWithValue("@blueTeam", pInfo.BlueTeam);
                            cmd.Parameters.AddWithValue("@redCaptain",
                                pInfo.RedCaptain);
                            cmd.Parameters.AddWithValue("@blueCaptain",
                                pInfo.BlueCaptain);
                            cmd.Parameters.AddWithValue("@subs", pInfo.Subs);
                            cmd.Parameters.AddWithValue("@noShows", pInfo.NoShows);
                            cmd.Parameters.AddWithValue("@startDate", pInfo.StartDate);
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine(
                                "AddPickupGame: Successfully UPDATED most recent pickup game: Red team: {0}, Blue Team: {1}, Red captain: {2}, Blue captain:" +
                                " {3}, Subs: {4}, No-shows: {5}, Starting at: {6} in pickup database.",
                                pInfo.RedTeam, pInfo.BlueTeam, pInfo.RedCaptain,
                                pInfo.BlueCaptain, pInfo.Subs, pInfo.NoShows,
                                pInfo.StartDate.ToString("G", DateTimeFormatInfo.InvariantInfo));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Problem adding pickup game to database: " +
                                    ex.Message);
                }
            }
        }

        /// <summary>
        ///     Updates the last pickup game's end time.
        /// </summary>
        /// <param name="endDate">The end date.</param>
        public void UpdatePickupEndTime(DateTime endDate)
        {
            if (VerifyDb())
            {
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupgames SET endDate = @newEndDate WHERE id IN (SELECT id FROM pickupgames ORDER BY startDate DESC LIMIT 1)";
                            cmd.Parameters.AddWithValue("@newEndDate", endDate);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format(
                                    "AddPickupGame: Successfully updated last pickup game's end date to: {0} in pickup database.",
                                    endDate.ToString("G",
                                        DateTimeFormatInfo.InvariantInfo)));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem updating last pickup game's end date in pickup database: " +
                        ex.Message);
                }
            }
        }

        /// <summary>
        /// Updates the pickup user's last played date.
        /// </summary>
        /// <param name="user">The user.</param>
        public void UpdateUserLastPlayedDate(string user)
        {
            if (VerifyDb())
            {
                if (!DoesUserExistInDb(user.ToLowerInvariant()))
                {
                    AddUserToDb(user, DateTime.Now);
                }
                try
                {
                    using (var sqlcon = new SQLiteConnection(_sqlConString))
                    {
                        sqlcon.Open();

                        using (var cmd = new SQLiteCommand(sqlcon))
                        {
                            cmd.CommandText =
                                "UPDATE pickupusers SET lastPlayedDate = @newLastPlayedDate WHERE user = @user";
                            cmd.Parameters.AddWithValue("@user", user.ToLowerInvariant());
                            cmd.Parameters.AddWithValue("newLastPlayedDate", DateTime.Now);
                            var total = cmd.ExecuteNonQuery();
                            if (total > 0)
                            {
                                Debug.WriteLine(string.Format("Updated last played date for: {0} in pickup users database.",
                                    user));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Problem updating last played date for {0} in pickup users database: {1}",
                        user,
                        ex.Message);
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
                    var userstr =
                        "CREATE TABLE pickupusers (id INTEGER PRIMARY KEY AUTOINCREMENT, user TEXT NOT NULL, subsUsed INTEGER," +
                        " noShows INTEGER, gamesStarted INTEGER, gamesFinished INTEGER, lastPlayedDate DATETIME)";
                    using (var cmd = new SQLiteCommand(userstr, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Pickup users table created.");
                    }
                    var gamestr =
                        "CREATE TABLE pickupgames (id INTEGER PRIMARY KEY AUTOINCREMENT, redTeam TEXT NOT NULL, blueTeam TEXT NOT NULL, redCaptain TEXT NOT NULL," +
                        " blueCaptain TEXT NOT NULL, subs TEXT NOT NULL, noShows TEXT NOT NULL, startDate DATETIME, endDate DATETIME)";
                    using (var cmd = new SQLiteCommand(gamestr, sqlcon))
                    {
                        cmd.ExecuteNonQuery();
                        Debug.WriteLine("Pickup games table created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Problem creating Pickup database: " +
                                ex.Message);
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
        ///     Deletes the pickup users database.
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
                Debug.WriteLine("Unable to delete pickup users database: " +
                                ex.Message);
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
                        cmd.CommandText =
                            "SELECT * FROM pickupusers WHERE user = @user";
                        cmd.Parameters.AddWithValue("@user",
                            user.ToLowerInvariant());
                        using (var reader = cmd.ExecuteReader())
                        {
                            return reader.HasRows;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Problem checking if user exists in early quitter database: " +
                    ex.Message);
            }
            return false;
        }

        /// <summary>
        ///     Verifies the pickup database.
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

                var usersTableExists = false;
                var gamesTableExists = false;

                using (var cmd = new SQLiteCommand(sqlcon))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText =
                        "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'pickupusers'";

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            usersTableExists = true;
                        }
                    }
                }
                using (var cmd = new SQLiteCommand(sqlcon))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText =
                        "SELECT * FROM sqlite_master WHERE type = 'table' AND name = 'pickupgames'";

                    using (var sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            gamesTableExists = true;
                        }
                    }
                }

                if (usersTableExists && gamesTableExists)
                {
                    return true;
                }
                Debug.WriteLine(
                    "Users or games table not found in pickup database... Creating DB tables...");
                CreateDb();
                return false;
            }
        }
    }
}