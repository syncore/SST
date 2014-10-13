using System;
using System.Collections.Generic;
using SSB.Enum;

namespace SSB.Core
{
    /// <summary>
    ///     Class that handles the bot's commands.
    /// </summary>
    public class BotCommands
    {
        private const string AccessCmd = "!access";
        private const string AddUserCmd = "!adduser";
        private const string DelUserCmd = "!deluser";
        private const string HelpCmd = "!help";

        private const string NoPermission =
            "^3* ^1[ERROR]^7 You do not have permission to use that command. ^3*";

        private readonly Dictionary<string, UserLevel> _cmdRequiredLevel;

        private readonly SynServerBot _ssb;
        private readonly Users _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BotCommands" /> class.
        /// </summary>
        public BotCommands(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
            _cmdRequiredLevel = new Dictionary<string, UserLevel>();
            SetupRequiredCmdLevels();
            AllBotCommands = new List<string> {HelpCmd, AccessCmd, AddUserCmd, DelUserCmd};
        }

        /// <summary>
        ///     Gets all of the bot commands.
        /// </summary>
        /// <value>
        ///     All of the bot commands
        /// </value>
        public List<string> AllBotCommands { get; private set; }

        /// <summary>
        ///     Processes the bot command.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="command">The command.</param>
        public void ProcessBotCommand(string fromUser, string command)
        {
            char[] sep = {' '};
            string[] args = command.Split(sep, 5);
            if (args.Length > 1)
            {
                HandleCmdWithArgs(fromUser, args);
            }
            else
            {
                HandleNoArgCmd(fromUser, args);
            }
        }

        /// <summary>
        ///     Executes the !access command.
        /// </summary>
        /// <remarks>
        ///     Arguments (optional): the user to check.
        ///     Required permission level: None.
        /// </remarks>
        private void ExecAccessCmd(string fromUser, string[] args)
        {
            _ssb.QlCommands.QlCmdSay(args.Length > 1
                ? string.Format("^3* ^5{0}'s^7 user level is: ^5[{1}] ^3*", args[1], GetUserLevel(args[1]))
                : string.Format("^3* ^5{0}'s^7 user level is: ^5[{1}] ^3*", fromUser, GetUserLevel(fromUser)));
        }

        /// <summary>
        ///     Executes the add user command.
        /// </summary>
        /// <param name="fromUser">The user who sent the add user command.</param>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        ///     args[1]: userToAdd, args[2]: accessLevel
        ///     Required permission level: Admin.
        /// </remarks>
        private void ExecAddUserCmd(string fromUser, string[] args)
        {
            UserLevel reqLevel = _cmdRequiredLevel[AddUserCmd];
            if (!UserHasReqLevel(fromUser, reqLevel))
            {
                return;
            }
            // TODO: define this in terms of the enum instead of hardcoding values
            if (!args[2].Equals("1") && !args[2].Equals("2") && !args[2].Equals("3"))
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3* ^1[ERROR]^3 {0} name accesslevel#^7 - access levels #s are: 1(user), 2(superuser), 3(admin) ^3*",
                        AddUserCmd));
                return;
            }
            // TODO: define this in terms of the enum instead of hardcoding values
            if ((args[2].Equals("3")) && ((GetUserLevel(fromUser) != UserLevel.Owner)))
            {
                _ssb.QlCommands.QlCmdSay(string.Format("^3* ^1[ERROR]^7 Only owners can add admins. ^3*"));
                return;
            }
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            DbResult result = _users.AddUserToDb(args[1], (UserLevel) Convert.ToInt32(args[2]), fromUser, date);
            if (result == DbResult.Success)
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^3* ^2[SUCCESS]:^7 Added user^2 {0} ^7to the^2 [{1}] ^7group. ^3*", args[1],
                        (UserLevel) Convert.ToInt32(args[2])));
            }
            else
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3* ^1[ERROR]^7 Unable to add user^1 {0}^7 to the^1 [{1}] ^7group. Code:^1 {2} ^3*",
                        args[1], (UserLevel) Convert.ToInt32(args[2]), result));
            }
        }

        /// <summary>
        ///     Executes the user deletion command.
        /// </summary>
        /// <param name="fromUser">The user who sent the user deletion command.</param>
        /// <param name="userToDelete">The user to delete.</param>
        /// <remarks>
        ///     /// Required permission level: Admin.
        /// </remarks>
        private void ExecDelUserCmd(string fromUser, string userToDelete)
        {
            UserLevel reqLevel = _cmdRequiredLevel[DelUserCmd];
            if (!UserHasReqLevel(fromUser, reqLevel))
            {
                return;
            }
            UserLevel todelUserLevel = GetUserLevel(userToDelete);
            DbResult result = _users.DeleteUserFromDb(userToDelete, fromUser, GetUserLevel(fromUser));
            if (result == DbResult.Success)
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^3* ^2[SUCCESS]:^7 Removed user^2 {0} ^7from the^2 [{1}] ^7group. ^3*",
                        userToDelete, todelUserLevel));
                //Refresh
                _users.RetrieveAllUsers();
            }
            else
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3* ^1[ERROR]^7 Unable to remove user^1 {0}^7 from the^1 [{1}] ^7group. Code:^1 {2} ^3*",
                        userToDelete, todelUserLevel, result));
            }
        }

        /// <summary>
        ///     Executes the !help command.
        /// </summary>
        /// <remarks>
        ///     Arguments: None.
        ///     Required permission level: None.
        /// </remarks>
        private void ExecHelpCmd()
        {
            _ssb.QlCommands.QlCmdSay("^7The ^2!help ^7command will go here.");
        }

        /// <summary>
        ///     Gets the requested user;s level.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The user's level.</returns>
        private UserLevel GetUserLevel(string user)
        {
            _users.RetrieveAllUsers();
            UserLevel level;
            return _users.AllUsers.TryGetValue(user, out level) ? level : level;
        }

        /// <summary>
        ///     Handles commands with arguments.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="args">The arguments.</param>
        private void HandleCmdWithArgs(string fromUser, string[] args)
        {
            string command = args[0];
            switch (command)
            {
                    // !addauser <name> <level>
                case AddUserCmd:
                    ExecAddUserCmd(fromUser, args);
                    break;
                    // !deluser <name>
                case DelUserCmd:
                    ExecDelUserCmd(fromUser, args[1]);
                    break;
                    // !access <name>
                case AccessCmd:
                    ExecAccessCmd(fromUser, args);
                    break;
            }
        }

        /// <summary>
        ///     Handles commands that do not have arguments.
        /// </summary>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="args">The arguments.</param>
        private void HandleNoArgCmd(string fromUser, string[] args)
        {
            string command = args[0];
            switch (command)
            {
                    // !help
                case HelpCmd:
                    ExecHelpCmd();
                    break;
                    // !access
                case AccessCmd:
                    ExecAccessCmd(fromUser, args);
                    break;
            }
        }

        /// <summary>
        ///     Sets up the required command levels for the bot commands.
        /// </summary>
        /// <returns>A dictionary with key: command, value: <see cref="UserLevel" />.</returns>
        /// <remarks>Note: commands that require no privelegs (ex: !help) are not defined here.</remarks>
        private void SetupRequiredCmdLevels()
        {
            // !adduser
            _cmdRequiredLevel[AddUserCmd] = UserLevel.Admin;
            // !deluser
            _cmdRequiredLevel[DelUserCmd] = UserLevel.Admin;
        }

        /// <summary>
        ///     Checks whether the user has the required access level to access a command.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="requiredLevel">The required access level.</param>
        /// <returns><c>true</c> if the user has the required access level, otherwise <c>false</c>.</returns>
        private bool UserHasReqLevel(string user, UserLevel requiredLevel)
        {
            _users.RetrieveAllUsers();
            if (GetUserLevel(user) >= requiredLevel)
            {
                return true;
            }
            _ssb.QlCommands.QlCmdSay(NoPermission);
            return false;
        }
    }
}