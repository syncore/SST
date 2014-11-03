using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Account date limiter. Kick player if player does not meet account registration date requirements.
    /// </summary>
    public class AccountDateLimit : IModule
    {
        private readonly SynServerBot _ssb;
        private int _minModuleArgs = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountDateLimit" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccountDateLimit(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the account date limit is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the account date limit is active; otherwise, <c>false</c>.
        /// </value>
        public static bool IsModuleActive { get; set; }

        /// <summary>
        ///     Gets or sets the minimum days that an account must be registered.
        /// </summary>
        /// <value>
        ///     The minimum days that an account must be registered.
        /// </value>
        public static int MinimumDaysRequired { get; set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinModuleArgs
        {
            get { return _minModuleArgs; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <days> ^7 - days must be >0",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AccountDateLimitArg));
        }

        /// <summary>
        ///     Evaluates the account date limit command.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }

            // Disable account date limiter
            if (c.Args[2].Equals("off"))
            {
                await DisableAccountDateLimiter();
                return;
            }
            int days;
            bool isValidNum = ((int.TryParse(c.Args[2], out days) && days > 0));
            if ((!isValidNum))
            {
                await DisplayArgLengthError(c);
                return;
            }
            IsModuleActive = true;
            MinimumDaysRequired = days;
            await _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]^7 Account date limit ^2ON.^7 Players with accounts registered in the last^1 {0}^7 days may not play.",
                    days));

            await RunUserDateCheck(_ssb.ServerInfo.CurrentPlayers);
        }

        /// <summary>
        ///     Runs the user date check on all current players.
        /// </summary>
        /// <param name="players">The players.</param>
        public async Task RunUserDateCheck(Dictionary<string, PlayerInfo> players)
        {
            // ToList() call on foreach to copy contents to separate list
            // because .NET modifies collection during enumeration under the hood and otherwise causes
            // "Collection was modified; enumeration operation may not execute" error, see:
            // http://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute
            foreach (var player in players.ToList())
            {
                await RunUserDateCheck(player.Key);
            }
        }

        /// <summary>
        ///     Runs the user date check on a given player.
        /// </summary>
        /// <param name="user">The user.</param>
        public async Task RunUserDateCheck(string user)
        {
            var qlDateChecker = new QlAccountDateChecker();
            DateTime date = await qlDateChecker.GetUserRegistrationDate(user);
            await VerifyUserDate(user, date);
        }

        /// <summary>
        ///     Disables the account date limiter.
        /// </summary>
        private async Task DisableAccountDateLimiter()
        {
            IsModuleActive = false;
            await _ssb.QlCommands.QlCmdSay(
                "^2[SUCCESS]^7 Account date limit ^1OFF^7. Players who registered on any date can play.");
        }

        /// <summary>
        ///     Verifies the user's registration date and kicks the user if the requirement is not met.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="regDate">The user's registration date.</param>
        private async Task VerifyUserDate(string user, DateTime regDate)
        {
            if (regDate == default(DateTime)) return;
            DateTime now = DateTime.Now;
            if ((now - regDate).TotalDays < MinimumDaysRequired)
            {
                Debug.WriteLine(
                    "User {0} has created account within the last {1} days. Date created: {2}. Kicking...",
                    user, MinimumDaysRequired, regDate);
                await _ssb.QlCommands.CustCmdKickban(user);
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[=> KICK]: ^1{0}^7 (QL account date:^1 {1}^7)'s account is too new and does not meet the limit of^2 {2}^7 days",
                        user, regDate.ToString("d"), MinimumDaysRequired));
            }
        }
    }
}