using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model.QlRanks;
using SSB.Modules;
using SSB.Util;

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
        private const string LimitEloCmd = "!elolimiter";
        private const string LoadModuleCmd = "!loadmod";

        private const string NoPermission =
            "^3* ^1[ERROR]^7 You do not have permission to use that command. ^3*";

        private const string UnloadModuleCmd = "!unloadmod";
        private readonly Dictionary<string, UserLevel> _cmdRequiredLevel;

        private readonly ModuleManager _modManager;
        private readonly List<string> _multipleArgsRequiredCmds;

        private readonly SynServerBot _ssb;
        private readonly Users _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BotCommands" /> class.
        /// </summary>
        public BotCommands(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
            _modManager = new ModuleManager(_ssb);
            _cmdRequiredLevel = new Dictionary<string, UserLevel>();
            SetupRequiredCmdLevels();
            AllBotCommands = new List<string> { HelpCmd, AccessCmd, AddUserCmd, DelUserCmd, LimitEloCmd, LoadModuleCmd, UnloadModuleCmd };
            _multipleArgsRequiredCmds = new List<string> { AddUserCmd, DelUserCmd, LimitEloCmd, LoadModuleCmd, UnloadModuleCmd };
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
        /// <param name="command">The full command text.</param>
        public void ProcessBotCommand(string fromUser, string command)
        {
            char[] sep = { ' ' };
            string[] args = command.Split(sep, 5);
            if (!UserHasReqLevel(fromUser, _cmdRequiredLevel[args[0]]))
            {
                return;
            }
            if ((_multipleArgsRequiredCmds.Contains(args[0]) && (args.Length == 1)))
            {
                _ssb.QlCommands.QlCmdSay(DisplayCmdArgError(args[0]));
                return;
            }
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
        /// Displays the command argument error.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A string containing the required command arguments.</returns>
        private string DisplayCmdArgError(string command)
        {
            switch (command)
            {
                case AddUserCmd:
                    return string.Format(
                        "^3* ^1[ERROR]^3 Usage: {0} name accesslevel#^7 - access levels #s are: 1(user), 2(superuser), 3(admin) ^3*",
                        AddUserCmd);

                case DelUserCmd:
                    return string.Format("^3* ^1[ERROR]^3 Usage: {0} user ^3*", DelUserCmd);

                case LimitEloCmd:
                    return string.Format("^3* ^1[ERROR]^3 Usage: {0} on/off minimumelo maximumelo ^7 - minimumelo must be >0 & maximumelo must be >600 ^3*", LimitEloCmd);

                case LoadModuleCmd:
                    return string.Format("^3* ^1[ERROR]^3 Usage: {0} modulename ^7 - Valid modules are:^2 {1} ^3*", LoadModuleCmd, string.Join(",", _modManager.ValidModules));

                case UnloadModuleCmd:
                    return string.Format("^3* ^1[ERROR]^3 Usage: {0} modulename ^7 - Valid modules are:^2 {1} ^3*", UnloadModuleCmd, string.Join(",", _modManager.ValidModules));
            }
            return
                "^3* ^1[ERROR]^3 Invalid number of arguments specified. ^3*";
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
            // TODO: define this in terms of the enum instead of hardcoding values
            if (!args[2].Equals("1") && !args[2].Equals("2") && !args[2].Equals("3"))
            {
                _ssb.QlCommands.QlCmdSay(DisplayCmdArgError(AddUserCmd));
                return;
            }
            // TODO: define this in terms of the enum instead of hardcoding values
            if ((args[2].Equals("3")) && ((GetUserLevel(fromUser) != UserLevel.Owner)))
            {
                _ssb.QlCommands.QlCmdSay(string.Format("^3* ^1[ERROR]^7 Only owners can add admins. ^3*"));
                return;
            }
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            DbResult result = _users.AddUserToDb(args[1], (UserLevel)Convert.ToInt32(args[2]), fromUser, date);
            if (result == DbResult.Success)
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^3* ^2[SUCCESS]:^7 Added user^2 {0} ^7to the^2 [{1}] ^7group. ^3*", args[1],
                        (UserLevel)Convert.ToInt32(args[2])));
            }
            else
            {
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3* ^1[ERROR]^7 Unable to add user^1 {0}^7 to the^1 [{1}] ^7group. Code:^1 {2} ^3*",
                        args[1], (UserLevel)Convert.ToInt32(args[2]), result));
            }
        }

        /// <summary>
        ///     Executes the user deletion command.
        /// </summary>
        /// <param name="fromUser">The user who sent the user deletion command.</param>
        /// <param name="userToDelete">The user to delete.</param>
        /// <remarks>
        /// Required permission level: Admin.
        /// </remarks>
        private void ExecDelUserCmd(string fromUser, string userToDelete)
        {
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
        /// Executes the limit elo command.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        /// args[1]: on/off, args[2]: minimum elo, args[3]: maximum elo
        /// Required permission level: Admin
        /// Module required: elolimiter
        /// </remarks>
        private async Task ExecLimitEloCmd(string[] args)
        {
            var modname = _modManager.GetModuleName(LimitEloCmd.Replace("!", string.Empty));
            if (!_modManager.IsModuleActive(modname))
            {
                _ssb.QlCommands.QlCmdSay(string.Format(
                "^3* ^1[ERROR]^7 Module ^3'{0}'^7 is not loaded. First load with:^2 {1} {0} ^3*", modname, LoadModuleCmd));
                return;
            }

            if (!args[1].Equals("on") && !args[1].Equals("off"))
            {
                _ssb.QlCommands.QlCmdSay(DisplayCmdArgError(LimitEloCmd));
                return;
            }
            if (args[1].Equals("off"))
            {
                _ssb.QlCommands.QlCmdSay("^3* ^2[SUCCESS]:^7 Elo limit ^1disabled.^7 Players with any Elo can play on this server. ^3*");
                return;
            }
            // Only the mininmum elo is specified...
            if (args[1].Equals("on") && args.Length == 3)
            {
                int min;
                bool minAcceptable = ((int.TryParse(args[2], out min) && min > 0));
                if ((!minAcceptable))
                {
                    _ssb.QlCommands.QlCmdSay(DisplayCmdArgError(LimitEloCmd));
                    return;
                }
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3* ^2[SUCCESS]:^7 Elo limit ^2enabled.^7 Players must have at least^2 {0} ^7elo to play on this server. ^3*",
                        min));
                // Perform the removal of the current players on the server...
                await _modManager.ModEloLimiter.RemoveEloPlayers(min, 0);
            }
            // An elo range (min to max) has been specified...
            else if (args[1].Equals("on") && args.Length == 4)
            {
                int min;
                int max;
                bool minAcceptable = ((int.TryParse(args[2], out min) && min > 0));
                bool maxAcceptable = ((int.TryParse(args[3], out max) && max > 600));
                if ((!minAcceptable || !maxAcceptable))
                {
                    _ssb.QlCommands.QlCmdSay(DisplayCmdArgError(LimitEloCmd));
                    return;
                }
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3* ^2[SUCCESS]:^7 Elo limit ^2enabled.^7 Players must have between^2 {0} ^7and^2 {1}^7 elo to play on this server. ^3*",
                        min, max
                        ));
                // Perform the removal of the current players on the server...
                await _modManager.ModEloLimiter.RemoveEloPlayers(min, max);
            }
            else
            {
                _ssb.QlCommands.QlCmdSay(DisplayCmdArgError(LimitEloCmd));
            }
        }

        /// <summary>
        /// Executes the load module command.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        ///     args[1]: The module name
        /// Required permission level: Admin
        /// </remarks>
        private void ExecLoadModuleCmd(string[] args)
        {
            if (!_modManager.ValidModules.Contains(args[1]))
            {
                _ssb.QlCommands.QlCmdSay(string.Format(
                        "^3* ^1[ERROR]^7 Invalid module ^1'{0}'^7 Available modules are:^2 {1} ^3*",
                        args[1], string.Join(",", _modManager.ValidModules)));
                return;
            }
            var modname = _modManager.GetModuleName(args[1]);
            if (_modManager.IsModuleActive(modname))
            {
                _ssb.QlCommands.QlCmdSay(string.Format(
                        "^3* ^1[ERROR]^7 Module ^1'{0}'^7 is already ^2active! ^3*",
                        modname));
                return;
            }
            var modtype = _modManager.GetModuleType(modname);
            _modManager.Load(modtype);
            _ssb.QlCommands.QlCmdSay(
                string.Format("^3* ^2[SUCCESS]:^7 Successfully loaded ^2'{0}'^7 module. ^3*", modname));
        }

        /// <summary>
        /// Executes the unload module command.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        /// args[1]: The module name
        /// Required permission level: Admin
        /// </remarks>
        private void ExecUnloadModuleCmd(string[] args)
        {
            if (!_modManager.ValidModules.Contains(args[1]))
            {
                _ssb.QlCommands.QlCmdSay(string.Format(
                        "^3* ^1[ERROR]^7 Invalid module ^1'{0}'^7 Available modules are:^2 {1} ^3*",
                        args[1], string.Join(",", _modManager.ValidModules)));
                return;
            }
            var modname = _modManager.GetModuleName(args[1]);
            if (!_modManager.IsModuleActive(modname))
            {
                _ssb.QlCommands.QlCmdSay(string.Format(
                        "^3* ^1[ERROR]^7 Module ^1'{0}'^7 is not ^2active! ^3*",
                        modname));
                return;
            }
            var modtype = _modManager.GetModuleType(modname);
            _modManager.Unload(modtype);
            _ssb.QlCommands.QlCmdSay(
                string.Format("^3* ^2[SUCCESS]:^7 Successfully unloaded ^2'{0}'^7 module. ^3*", modname));
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
                // !limitelo <on/off> <min> <max>
                case LimitEloCmd:
                    ExecLimitEloCmd(args);
                    break;
                // !loadmod <module>
                case LoadModuleCmd:
                    ExecLoadModuleCmd(args);
                    break;
                // !unloadmod <module>
                case UnloadModuleCmd:
                    ExecUnloadModuleCmd(args);
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
            // !limitelo
            _cmdRequiredLevel[LimitEloCmd] = UserLevel.Admin;
            // !loadmod
            _cmdRequiredLevel[LoadModuleCmd] = UserLevel.Admin;
            // !unloadmod
            _cmdRequiredLevel[UnloadModuleCmd] = UserLevel.Admin;
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