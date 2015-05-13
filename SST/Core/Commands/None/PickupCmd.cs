using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Core.Commands.Admin;
using SST.Core.Modules.Irc;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    ///     Command: Start, stop, reset, unban users from pickup, show pickup help.
    /// </summary>
    /// <remarks>
    ///     The overall access level for this command is <see cref="UserLevel" />.None to allow for general command access,
    ///     but certain arguments to this command will require elevated access (i.e. starting/stopping a pickup game).
    /// </remarks>
    public class PickupCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:PICKUP]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly DbUsers _userDb;
        private readonly UserLevel _userLevel = UserLevel.None;

        public PickupCmd(SynServerTool sst)
        {
            _sst = sst;
            _userDb = new DbUsers();
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
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (!_sst.Mod.Pickup.Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Pickup module is not active. An admin must first load it with:^7 {0}{1} {2}",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}",
                            IrcCommandList.IrcCmdQl, CommandList.CmdModule))
                        : CommandList.CmdModule),
                    ModuleCmd.PickupArg);
                await SendServerTell(c, StatusMessage);

                Log.Write(
                    string.Format(
                        "{0} attempted {1} command from {2}, but {3} module is not loaded. Ignoring.",
                        c.FromUser, c.CmdName, ((c.FromIrc) ? "from IRC" : "from in-game"),
                        ModuleCmd.PickupArg), _logClassType, _logPrefix);

                return false;
            }

            if (!Helpers.GetArgVal(c, 1).Equals("reset") && !Helpers.GetArgVal(c, 1).Equals("start") &&
                !Helpers.GetArgVal(c, 1).Equals("stop")
                && !Helpers.GetArgVal(c, 1).Equals("unban") && !Helpers.GetArgVal(c, 1).Equals("help"))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            // These arguments to the the pickup command require elevated privileges.
            if (Helpers.GetArgVal(c, 1).Equals("reset") || Helpers.GetArgVal(c, 1).Equals("start") ||
                Helpers.GetArgVal(c, 1).Equals("stop") || Helpers.GetArgVal(c, 1).Equals("unban"))
            {
                var userLevel = IsIrcOwner(c) ? UserLevel.Owner : _userDb.GetUserLevel(c.FromUser);
                if (userLevel < UserLevel.SuperUser)
                {
                    await DisplayInsufficientAccessError(c);

                    Log.Write(
                        string.Format(
                            "{0} attempted to use a {1} command with args that require a higher user level than {0} has. Ignoring.",
                            c.FromUser, c.CmdName), _logClassType, _logPrefix);

                    return false;
                }
            }
            if (Helpers.GetArgVal(c, 1).Equals("reset"))
            {
                return await _sst.Mod.Pickup.Manager.EvalPickupReset(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("start"))
            {
                return await _sst.Mod.Pickup.Manager.EvalPickupStart(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("stop"))
            {
                return await _sst.Mod.Pickup.Manager.EvalPickupStop(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("unban"))
            {
                return await _sst.Mod.Pickup.Manager.EvalPickupUnban(c);
            }
            if (Helpers.GetArgVal(c, 1).Equals("help"))
            {
                await DisplayPickupHelp(c);
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
                "^1[ERROR]^3 Usage: {0}{1} <start/stop/reset/unban/help>",
                CommandList.GameCommandPrefix,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}",
                        c.CmdName, c.Args[1]))
                    : c.CmdName));
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
        ///     Displays the insufficient access level error.
        /// </summary>
        private async Task DisplayInsufficientAccessError(CmdArgs c)
        {
            StatusMessage = "^1[ERROR]^3 You do not have permission to use that command.";
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Displays the pickup help (the possible pickup commands).
        /// </summary>
        private async Task DisplayPickupHelp(CmdArgs c)
        {

            string[] msgs = {
                string.Format(
                "^5[PICKUP] ^7Commands: ^3{0}{1}^7 sign up, ^3{0}{2}^7 remove yourself, ^3{0}{3}^7 sign up as captain",
                CommandList.GameCommandPrefix, CommandList.CmdPickupAdd,
                CommandList.CmdPickupRemove, CommandList.CmdPickupCap),
                
                string.Format(
                "^3{0}{1}^7 captains: pick a player, ^3{0}{2}^7 request a substitute for yourself, ^3{0}{3}^7 see who's signed up",
                CommandList.GameCommandPrefix, CommandList.CmdPickupPick,
                CommandList.CmdPickupSub, CommandList.CmdPickupWho),

                string.Format(
                "^5Privileged: ^3{0}{1} start^7 lock server & start, ^3{0}{1} reset^7 reset game," +
                " ^3{0}{1} stop^7 cancel & unlock, ^3{0}{1} unban^7 to unban no-shows",
                CommandList.GameCommandPrefix,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}",
                        IrcCommandList.IrcCmdQl, CommandList.CmdPickup))
                    : CommandList.CmdPickup))
                            };

            foreach (var msg in msgs)
            {
                StatusMessage = msg;
                await SendServerSay(c, StatusMessage);
            }
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
            var cfg = cfgHandler.ReadConfiguration();
            return
                (c.FromUser.Equals(cfg.IrcOptions.ircAdminNickname,
                    StringComparison.InvariantCultureIgnoreCase));
        }
    }
}