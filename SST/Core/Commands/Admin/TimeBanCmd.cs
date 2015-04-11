using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Admin
{
    /// <summary>
    ///     Command: automatically ban a player for a specified time period.
    /// </summary>
    public class TimeBanCmd : IBotCommand
    {
        private readonly DbBans _banDb;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:TIMEBAN]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly DbUsers _userDb;
        private readonly UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimeBanCmd" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public TimeBanCmd(SynServerTool sst)
        {
            _sst = sst;
            _userDb = new DbUsers();
            _banDb = new DbBans();
        }

        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs
        {
            get { return _qlMinArgs + 1; }
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

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
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was successfully executed, otherwise
        ///     <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if ((!Helpers.GetArgVal(c, 1).Equals("add")) && (!Helpers.GetArgVal(c, 1).Equals("del")) &&
                (!Helpers.GetArgVal(c, 1).Equals("check")) &&
                (!Helpers.GetArgVal(c, 1).Equals("list")))
            {
                await DisplayArgLengthError(c);
                return false;
            }

            if (Helpers.GetArgVal(c, 1).Equals("add"))
            {
                return await EvalBanAddition(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("del"))
            {
                return await EvalBanDeletion(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("check"))
            {
                return await EvalBanCheck(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("list"))
            {
                await ListBans(c);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <add> <del> <check> <list>",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Adds the ban.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the ban was successfully added,
        ///     otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> AddBan(CmdArgs c)
        {
            // Kickban user immediately using internal QL command
            await _sst.QlCommands.CustCmdKickban(Helpers.GetArgVal(c, 2));

            if (_banDb.UserAlreadyBanned(Helpers.GetArgVal(c, 2)))
            {
                var deleted = await DeleteIfExpired(Helpers.GetArgVal(c, 2));
                if (deleted)
                {
                    StatusMessage =
                        string.Format("^5[TIMEBAN]^7 A previous ban for^3 {0}^7 has expired. Now removing." +
                                      " Re-add if you wish to re-ban.",
                            Helpers.GetArgVal(c, 2));
                    await SendServerTell(c, StatusMessage);
                    return false;
                }

                var banInfo = _banDb.GetBanInfo(Helpers.GetArgVal(c, 2));
                if (banInfo == null)
                {
                    StatusMessage =
                        "^1[ERROR]^3 Unable to retrieve ban information while attempting to add ban.";
                    await SendServerTell(c, StatusMessage);
                    return false;
                }
                StatusMessage = string.Format(
                    "^5[TIMEBAN]^7 Time-ban already exists for player: ^3{0}^7, who was banned by admin: ^3{1}^7 on ^1{2}^7." +
                    " Ban will expire on: ^2{3}.^7 Use ^3{4}{5} del {0}^7 to remove ban.",
                    Helpers.GetArgVal(c, 2), banInfo.BannedBy,
                    banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                    banInfo.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}",
                            c.CmdName, c.Args[1]))
                        : c.CmdName));

                await SendServerTell(c, StatusMessage);
                return false;
            }

            // length was already verified to be a double in Eval method
            var length = double.Parse(Helpers.GetArgVal(c, 3));
            var scale = Helpers.GetArgVal(c, 4);
            var expirationDate = ExpirationDateGenerator.GenerateExpirationDate(length, scale);

            if (
                _banDb.AddUserToDb(Helpers.GetArgVal(c, 2), c.FromUser, DateTime.Now, expirationDate,
                    BanType.AddedByAdmin) ==
                UserDbResult.Success)
            {
                StatusMessage = string.Format(
                    "^2[SUCCESS]^7 Added time-ban for player: {0}. Ban will expire in {1} {2} on {3}",
                    Helpers.GetArgVal(c, 2), Math.Truncate(length), scale,
                    expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo));
                await SendServerSay(c, StatusMessage);

                // UI: reflect changes
                _sst.UserInterface.RefreshCurrentBansDataSource();

                return true;
            }

            StatusMessage = string.Format(
                "^1[ERROR]^3 An error occurred while attempting to add a time-ban for player:^1 {0}",
                Helpers.GetArgVal(c, 2));
            await SendServerTell(c, StatusMessage);
            return false;
        }

        /// <summary>
        ///     Checks a specific user's ban information, if it exists.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task CheckBan(CmdArgs c)
        {
            var deleted = await DeleteIfExpired(Helpers.GetArgVal(c, 2));
            if (deleted)
            {
                StatusMessage =
                    string.Format("^5[TIMEBAN]^7 A previous ban for^3 {0}^7 has expired. Now removing.",
                        Helpers.GetArgVal(c, 2));
                await SendServerSay(c, StatusMessage);
                return;
            }

            var banInfo = _banDb.GetBanInfo(Helpers.GetArgVal(c, 2));
            StatusMessage = string.Format("^5[TIMEBAN]^7 {0}",
                ((banInfo != null)
                    ? (string.Format(
                        "Player: ^3{0}^7 was banned by admin:^3 {1}^7 on^2 {2}^7. Ban will expire on:^1 {3}",
                        Helpers.GetArgVal(c, 2), banInfo.BannedBy,
                        banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                        banInfo.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)))
                    : (string.Format("No time-ban exists for player:^3 {0}",
                        Helpers.GetArgVal(c, 2)))));
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Deletes the ban.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the ban deletion was
        ///     successful, otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> DelBan(CmdArgs c)
        {
            try
            {
                _banDb.DeleteUserFromDb(Helpers.GetArgVal(c, 2));

                // Unban immediately from QL's internal ban system
                await _sst.QlCommands.CmdUnban(Helpers.GetArgVal(c, 2));

                StatusMessage = string.Format("^2[SUCCESS]^7 Removed time-ban for player^2 {0}",
                    Helpers.GetArgVal(c, 2));

                await SendServerSay(c, StatusMessage);

                // UI: reflect changes
                _sst.UserInterface.RefreshCurrentBansDataSource();

                return true;
            }
            catch (Exception e)
            {
                Log.WriteCritical(string.Format(
                    "Problem encountered while trying to delete user {0} from ban database: {1}",
                    Helpers.GetArgVal(c, 2),
                    e.Message), _logClassType, _logPrefix);
            }

            StatusMessage = string.Format(
                "^1[ERROR]^3 An error occurred while attempting to remove time ban for player: ^1{0}",
                Helpers.GetArgVal(c, 2));

            await SendServerTell(c, StatusMessage);
            return false;
        }

        /// <summary>
        ///     Removes any expired bans.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>
        ///     <c>true</c> if the expired ban was deleted,
        ///     otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> DeleteIfExpired(string user)
        {
            BanInfo bInfo;
            if (_banDb.IsExistingBanStillValid(user, out bInfo)) return false;

            Log.Write(string.Format("Attempting to remove expired ban for player {0}",
                user), _logClassType, _logPrefix);

            var bManager = new BanManager(_sst);
            var result = await bManager.RemoveBan(bInfo);
            return result;
        }

        /// <summary>
        ///     Evaluates the ban addition command and executes it if all parameters are correct.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the ban addition evaluation successfully
        ///     passed, otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> EvalBanAddition(CmdArgs c)
        {
            // !timeban add user # scale
            if (c.Args.Length != (c.FromIrc ? 6 : 5))
            {
                StatusMessage =
                    string.Format(
                        "^1[ERROR]^3 Usage: {0}{1} <add> <name> <time> <scale> - name is without clan, time is a number," +
                        " scale: secs, mins, hours, days, months, or years.",
                        CommandList.GameCommandPrefix,
                        ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            double time;
            if (!double.TryParse(Helpers.GetArgVal(c, 3), out time) || time <= 0)
            {
                StatusMessage = "^1[ERROR]^3 Time must be a positive number!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (time > int.MaxValue)
            {
                // Just compare to int type's max size as in case of month and year
                // it has to be converted to int and can't be double anyway
                StatusMessage = "^1[ERROR]^3 Time is too large!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            var validScale = Helpers.ValidTimeScales.Contains(Helpers.GetArgVal(c, 4));
            if (!validScale)
            {
                StatusMessage = "^1[ERROR]^3 Scale must be: secs, mins, hours, days, months, OR years";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (_userDb.GetUserLevel(Helpers.GetArgVal(c, 2)) >= UserLevel.Admin)
            {
                StatusMessage = "^1[ERROR]^3 Users with user level admin or higher may not be time-banned!";
                await SendServerTell(c, StatusMessage);
                return false;
            }

            return await AddBan(c);
        }

        /// <summary>
        ///     Evaluates the ban check command and executes it if all parameters are correct.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the ban check evaluation
        ///     successfully passed, otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> EvalBanCheck(CmdArgs c)
        {
            if (c.Args.Length != (c.FromIrc ? 4 : 3))
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} <check> <name> - name is without the clan tag",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
                await SendServerTell(c, StatusMessage);
                return false;
            }

            await CheckBan(c);
            return true;
        }

        /// <summary>
        ///     Evaluates the ban deletion command and executes it if all paramters are correct.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the ban deletion check evaluation
        ///     successfully passed, otherwise <c>false</c>.
        /// </returns>
        private async Task<bool> EvalBanDeletion(CmdArgs c)
        {
            if (c.Args.Length != (c.FromIrc ? 4 : 3))
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Usage: {0}{1} <del> <name> - name is without the clan tag",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (!_banDb.UserAlreadyBanned(Helpers.GetArgVal(c, 2)))
            {
                StatusMessage = string.Format("^1[ERROR]^3 No timeban exists for user: ^1{0}",
                    Helpers.GetArgVal(c, 2));
                await SendServerTell(c, StatusMessage);
                return false;
            }

            return await DelBan(c);
        }

        /// <summary>
        ///     Lists all of the bans, if they exist.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task ListBans(CmdArgs c)
        {
            // First remove expired bans
            var bans = _banDb.GetAllBans();
            var sb = new StringBuilder();
            if (bans.Count > 0)
            {
                foreach (var user in bans.ToList())
                {
                    var deleted = await DeleteIfExpired(user.PlayerName);
                    if (deleted)
                    {
                        bans.Remove(user);
                    }
                    else
                    {
                        sb.Append(string.Format("{0}, ", user.PlayerName));
                    }
                }
            }

            StatusMessage = string.Format("^5[TIMEBAN]^7 {0}",
                ((bans.Count > 0)
                    ? (string.Format("Banned players: ^1{0}^7 - To see ban info: ^3{1}{2} check <player>",
                        sb.ToString().TrimEnd(',', ' '), CommandList.GameCommandPrefix,
                        ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName)))
                    : "No players are time-banned."));
            await SendServerSay(c, StatusMessage);
        }
    }
}