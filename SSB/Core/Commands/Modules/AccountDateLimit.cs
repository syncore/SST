using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Config;
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
        public const string NameModule = "accountdate";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:ACCOUNTDATE]";
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountDateLimit" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccountDateLimit(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            LoadConfig();
        }

        /// <summary>
        ///     Gets or sets the minimum days that an account must be registered.
        /// </summary>
        /// <value>
        ///     The minimum days that an account must be registered.
        /// </value>
        public static uint MinimumDaysRequired { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets the minimum module arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum module arguments for the IRC command.
        /// </value>
        public int IrcMinModuleArgs
        {
            get { return _qlMinModuleArgs + 1; }
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
        ///     Gets the name of the module.
        /// </summary>
        /// <value>
        ///     The name of the module.
        /// </value>
        public string ModuleName
        {
            get { return NameModule; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinModuleArgs
        {
            get { return _qlMinModuleArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Enables the account date limiter.
        /// </summary>
        /// <param name="days">The days.</param>
        /// <remarks>
        ///     This is for use with the auto Init() method and the UI and does not produce a message.
        /// </remarks>
        public async Task EnableAccountDateLimiter(uint days)
        {
            MinimumDaysRequired = days;
            UpdateConfig(true);
            await RunUserDateCheck(_ssb.ServerInfo.CurrentPlayers);
        }

        /// <summary>
        ///     Evaluates the account date limit command.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c>if the command evaluation was successful,
        ///     otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < (c.FromIrc ? IrcMinModuleArgs : _qlMinModuleArgs))
            {
                await DisplayArgLengthError(c);
                return false;
            }

            // Disable account date limiter
            if (Helpers.GetArgVal(c, 2).Equals("off"))
            {
                await DisableAccountDateLimiter(c);
                return true;
            }
            uint days;
            var isValidNum = ((uint.TryParse(Helpers.GetArgVal(c, 2), out days) && days != 0));
            if ((!isValidNum))
            {
                await DisplayArgLengthError(c);
                return false;
            }

            await EnableAccountDateLimiter(c, days);
            return true;
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
            return (string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <days> ^7 - days must be >0",
                CommandList.GameCommandPrefix, c.CmdName,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule)));
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            Active = _configHandler.Config.AccountDateOptions.minimumDaysRequired != 0 &&
                     _configHandler.Config.AccountDateOptions.isActive;
            MinimumDaysRequired =
                _configHandler.Config.AccountDateOptions.minimumDaysRequired;

            Log.Write(string.Format("Active: {0}, minimum days required: {1}",
                (Active ? "YES" : "NO"), MinimumDaysRequired), _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Runs the user date check on all current players.
        /// </summary>
        /// <param name="players">The players.</param>
        public async Task RunUserDateCheck(Dictionary<string, PlayerInfo> players)
        {
            var qlDateChecker = new QlAccountDateChecker();
            foreach (var player in players.ToList())
            {
                var date = await qlDateChecker.GetUserRegistrationDate(player.Key);
                await VerifyUserDate(player.Key, date);
            }
        }

        /// <summary>
        ///     Runs the user date check on a given player.
        /// </summary>
        /// <param name="user">The user.</param>
        public async Task RunUserDateCheck(string user)
        {
            var qlDateChecker = new QlAccountDateChecker();
            var date = await qlDateChecker.GetUserRegistrationDate(user);
            await VerifyUserDate(user, date);
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Updates the configuration.
        /// </summary>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            _configHandler.Config.AccountDateOptions.isActive = active;
            _configHandler.Config.AccountDateOptions.minimumDaysRequired = MinimumDaysRequired;
            _configHandler.WriteConfiguration();

            // Reflect changes in UI
            _ssb.UserInterface.PopulateModAccountDateUi();
        }

        /// <summary>
        ///     Disables the account date limiter.
        /// </summary>
        private async Task DisableAccountDateLimiter(CmdArgs c)
        {
            UpdateConfig(false);
            StatusMessage =
                "^2[SUCCESS]^7 Account date limit ^2OFF.^7 Players who registered on any date ^2CAN^7 play.";
            await SendServerSay(c, StatusMessage);
            Log.Write(string.Format("Received {0} request from {1} to disable account date limiter module. Disabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Enables the account date limiter.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="days">The minimum amount of days.</param>
        private async Task EnableAccountDateLimiter(CmdArgs c, uint days)
        {
            MinimumDaysRequired = days;
            UpdateConfig(true);
            StatusMessage = string.Format(
                "^2[SUCCESS]^7 Account date limit ^2ON^7. Players with accounts registered in the last^1 {0} ^7days ^1CANNOT^7 play.",
                days);
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Received {0} request from {1} to enable account date limiter module. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);

            await RunUserDateCheck(_ssb.ServerInfo.CurrentPlayers);
        }

        /// <summary>
        ///     Verifies the user's registration date and kicks the user if the requirement is not met.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="regDate">The user's registration date.</param>
        private async Task VerifyUserDate(string user, DateTime regDate)
        {
            if (regDate == default(DateTime)) return;
            if ((DateTime.Now - regDate).TotalDays < MinimumDaysRequired)
            {
                await _ssb.QlCommands.CustCmdKickban(user);
                await _ssb.QlCommands.QlCmdSay(string.Format(
                    "^3[=> KICK]: ^1{0}^7 (QL account date:^1 {1}^7)'s account is too new and does not meet the limit of^2 {2} ^7days",
                    user, regDate.ToString("d"), MinimumDaysRequired));
                Log.Write(string.Format(
                    "Player {0}'s account is newer than minimum of {1} days that is required. Date created: {2}. Kicking player.",
                    user, MinimumDaysRequired,
                    regDate.ToString("G", DateTimeFormatInfo.InvariantInfo)), _logClassType, _logPrefix);
            }
        }
    }
}