using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Add a user to the tool's internal user database.
    /// </summary>
    public class AddUserCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:ADDUSER]";
        private readonly int _qlMinArgs = 3;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Admin;
        private readonly DbUsers _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserCmd" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public AddUserCmd(SynServerTool sst)
        {
            _sst = sst;
            _users = new DbUsers();
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

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
        /// <param name="c">The command args</param>
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
            if (!Helpers.GetArgVal(c, 2).Equals("1") && !Helpers.GetArgVal(c, 2).Equals("2") &&
                !Helpers.GetArgVal(c, 2).Equals("3"))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if ((Helpers.GetArgVal(c, 2).Equals("3")))
            {
                var userLevel = IsIrcOwner(c) ? UserLevel.Owner : _users.GetUserLevel(c.FromUser);
                if (userLevel != UserLevel.Owner)
                {
                    StatusMessage = string.Format("^1[ERROR]^3 Only owners can add admins.");
                    await SendServerTell(c, StatusMessage);

                    Log.Write(string.Format("Non-owner {0} attempted to add an admin from {1} Ignoring.",
                        c.FromUser, ((c.FromIrc) ? "IRC." : "in-game.")), _logClassType, _logPrefix);

                    return false;
                }
            }
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var result = _users.AddUserToDb(Helpers.GetArgVal(c, 1),
                (UserLevel)Convert.ToInt32(Helpers.GetArgVal(c, 2)), c.FromUser,
                date);
            if (result == UserDbResult.Success)
            {
                StatusMessage = string.Format("^2[SUCCESS]^7 Added user^2 {0} ^7to the ^2[{1}] ^7group.",
                    Helpers.GetArgVal(c, 1), (UserLevel)Convert.ToInt32(Helpers.GetArgVal(c, 2)));
                await SendServerSay(c, StatusMessage);

                // UI: reflect changes
                _sst.UserInterface.RefreshCurrentSstUsersDataSource();

                return true;
            }

            StatusMessage = string.Format(
                "^1[ERROR]^3 Unable to add user ^1{0}^3 to the ^1[{1}] ^3group. Code:^1 {2}",
                Helpers.GetArgVal(c, 1), (UserLevel)Convert.ToInt32(Helpers.GetArgVal(c, 2)), result);
            await SendServerTell(c, StatusMessage);
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
                "^1[ERROR]^3 Usage: {0}{1} name access# - name is without clantag. access #s are: 1(user), 2(superuser), 3(admin)",
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
        ///     Determines whether the command was sent from the owner of
        ///     the bot via IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was sent from IRC and from
        ///     an the IRC owner.
        /// </returns>
        private bool IsIrcOwner(CmdArgs c)
        {
            if (!c.FromIrc) return false;
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            return
                (c.FromUser.Equals(cfgHandler.Config.IrcOptions.ircAdminNickname,
                    StringComparison.InvariantCultureIgnoreCase));
        }
    }
}