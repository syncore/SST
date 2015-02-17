using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: automatically ban a player for a specified time period.
    /// </summary>
    public class TimeBanCmd : IBotCommand
    {
        private readonly DbBans _banDb;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _userDb;

        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimeBanCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public TimeBanCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _userDb = new DbUsers();
            _banDb = new DbBans();
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <add> <del> <check> <list>",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if ((!c.Args[1].Equals("add")) && (!c.Args[1].Equals("del")) && (!c.Args[1].Equals("check")) && (!c.Args[1].Equals("list")))
            {
                await DisplayArgLengthError(c);
                return;
            }

            if (c.Args[1].Equals("add"))
            {
                await EvalBanAddition(c);
            }
            if (c.Args[1].Equals("del"))
            {
                await EvalBanDeletion(c);
            }
            if (c.Args[1].Equals("check"))
            {
                await EvalBanCheck(c);
            }
            if (c.Args[1].Equals("list"))
            {
                await ListBans(c);
            }
        }

        /// <summary>
        /// Adds the ban.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task AddBan(CmdArgs c)
        {
            // !timeban add user # scale

            // Kickban user immediately
            await _ssb.QlCommands.CustCmdKickban(c.Args[2]);

            if (_banDb.UserAlreadyBanned(c.Args[2]))
            {
                // Ban has previously expired; delete it
                await RemoveAnyExpiredBans(c.Args[2]);

                var banInfo = _banDb.GetBanInfo(c.Args[2]);
                if (banInfo == null) return;
                await _ssb.QlCommands.QlCmdSay(
                    string.Format("^5[TIMEBAN]^7 Time-ban already exists for player: ^3{0}^7, who was banned by admin: ^3{1}^7 on ^1{2}^7." +
                                  " Ban will expire on: ^2{3}.^7 Use ^3{4}{5} del {0}^7 to remove ban.",
                    c.Args[2], banInfo.BannedBy, banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                    banInfo.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                    CommandProcessor.BotCommandPrefix, c.CmdName));
                return;
            }

            // length was already verified to be a double in Eval method
            var length = double.Parse(c.Args[3]);
            var scale = c.Args[4];
            var expirationDate = ExpirationDateGenerator.GenerateExpirationDate(length, scale);

            if (_banDb.AddUserToDb(c.Args[2], c.FromUser, DateTime.Now, expirationDate, BanType.AddedByAdmin) ==
                UserDbResult.Success)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^2[SUCCESS]^7 Added time-ban for player: ^2{0}^7. Ban will expire in ^2{1}^7 {2} on^2 {3}",
                            c.Args[2], Math.Truncate(length), scale,
                            expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));
            }
            else
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 An error occurred while attempting to add a time-ban for player: {0}",
                c.Args[2]));
            }
        }

        /// <summary>
        /// Checks a specific user's ban information, if it exists.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task CheckBan(CmdArgs c)
        {
            await RemoveAnyExpiredBans(c.Args[2]);

            var banInfo = _banDb.GetBanInfo(c.Args[2]);
            await _ssb.QlCommands.QlCmdSay(string.Format("^5[TIMEBAN]^7 {0}",
                ((banInfo != null) ? (string.Format("Player: ^3{0}^7 was banned by admin:^3 {1}^7 on^2 {2}^7. Ban will expire on:^1 {3}",
                c.Args[2], banInfo.BannedBy, banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                banInfo.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo))) : (string.Format("No time-ban exists for player:^3 {0}",
                c.Args[2])))));
        }

        /// <summary>
        /// Deletes the ban.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task DelBan(CmdArgs c)
        {
            try
            {
                _banDb.DeleteUserFromDb(c.Args[2]);
                await _ssb.QlCommands.QlCmdSay(string.Format("^2[SUCCESS]^7 Removed time-ban for player {0}", c.Args[2]));
                // Unban immediately from QL's internal ban system
                await _ssb.QlCommands.SendToQlAsync(string.Format("unban {0}", c.Args[2]), false);
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception encountered while trying to delete user {0} from ban database: {1}", c.Args[2], e.Message);
            }
            await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 An error occurred while attempting to remove time ban for player: {0}",
                c.Args[2]));
        }

        /// <summary>
        /// Evaluates the ban addition command and executes it if all parameters are correct.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalBanAddition(CmdArgs c)
        {
            // !timeban add user # scale
            if (c.Args.Length != 5)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
             "^1[ERROR]^3 Usage: {0}{1} <add> <name> <time> <scale> - name is without clan, time is a number, scale: secs, mins, hours, days, months, or years.",
             CommandProcessor.BotCommandPrefix, c.CmdName));
                return;
            }
            double time;
            if (!double.TryParse(c.Args[3], out time))
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Time must be a positive number!");
                return;
            }
            if (time <= 0)
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Time must be a positive number!");
                return;
            }
            if (time > int.MaxValue)
            {
                // Just compare to int type's max size as in case of month and year
                // it has to be converted to int and can't be double anyway
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Time is too large!");
                return;
            }
            bool validScale = Helpers.ValidTimeScales.Contains(c.Args[4]);
            if (!validScale)
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Scale must be: secs, mins, hours, days, months, OR years");
                return;
            }
            if (_userDb.GetUserLevel(c.Args[2]) >= UserLevel.Admin)
            {
                await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 Users with user level admin or higher may not be time-banned!");
                return;
            }
            await AddBan(c);
        }

        /// <summary>
        /// Evaluates the ban check command and executes it if all parameters are correct.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalBanCheck(CmdArgs c)
        {
            if (c.Args.Length != 3)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} <check> <name> - name is without the clan tag",
                    CommandProcessor.BotCommandPrefix, c.CmdName));
            }
            else
            {
                await CheckBan(c);
            }
        }

        /// <summary>
        /// Evaluates the ban deletion command and executes it if all paramters are correct.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task EvalBanDeletion(CmdArgs c)
        {
            if (c.Args.Length != 3)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format(
             "^1[ERROR]^3 Usage: {0}{1} <del> <name> - name is without the clan tag",
             CommandProcessor.BotCommandPrefix, c.CmdName));
                return;
            }
            if (!_banDb.UserAlreadyBanned(c.Args[2]))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 No timeban exists for user: {0}",
                        c.Args[2]));
            }
            else
            {
                await DelBan(c);
            }
        }

        /// <summary>
        /// Lists all of the bans, if they exist.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task ListBans(CmdArgs c)
        {
            // First remove expired bans
            var bans = _banDb.GetAllBans();
            if (bans.Length > 0)
            {
                if (bans.IndexOf(',') > -1)
                {
                    string[] bannedUsers = bans.Split(',');
                    foreach (var user in bannedUsers)
                    {
                        await RemoveAnyExpiredBans(user.Trim());
                    }
                }
            }

            await _ssb.QlCommands.QlCmdSay(string.Format("^5[TIMEBAN]^7 {0}",
                ((!string.IsNullOrEmpty(bans)) ? (string.Format("Banned players: ^1{0}^7 - To see ban info: ^3{1}{2} check <player>",
                bans, CommandProcessor.BotCommandPrefix, c.CmdName)) : "No players are time-banned.")));
        }

        /// <summary>
        /// Removes any expired bans.
        /// </summary>
        /// <param name="user">The user.</param>
        private async Task RemoveAnyExpiredBans(string user)
        {
            if (!_banDb.IsExistingBanStillValid(user))
            {
                var bInfo = _banDb.GetBanInfo(user);
                var pAutoBanner = new PlayerAutoBanner(_ssb);
                await pAutoBanner.RemoveBan(user, bInfo);
            }
        }
    }
}