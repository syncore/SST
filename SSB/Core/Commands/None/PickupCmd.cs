using System;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
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
        private readonly int _minArgs = 2;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _userDb;
        private readonly UserLevel _userLevel = UserLevel.None;

        public PickupCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _userDb = new DbUsers();
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
            if (!_ssb.Mod.Pickup.Active)
            {
                StatusMessage = string.Format(
                            "^1[ERROR]^3 Pickup module is not active. An admin must first load it with:^7 {0}{1} {2}",
                            CommandList.GameCommandPrefix, CommandList.CmdModule,
                            ModuleCmd.PickupArg);
                await SendServerTell(c, StatusMessage);
                return false;
            }

            if (!c.Args[1].Equals("reset") && !c.Args[1].Equals("start") && !c.Args[1].Equals("stop")
                && !c.Args[1].Equals("unban") && !c.Args[1].Equals("help"))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            // These arguments to the the pickup command require elevated privileges.
            if (c.Args[1].Equals("reset") || c.Args[1].Equals("start") ||
                c.Args[1].Equals("stop") || c.Args[1].Equals("unban"))
            {
                var userLevel = IsIrcOwner(c) ? UserLevel.Owner : _userDb.GetUserLevel(c.FromUser);
                if (userLevel < UserLevel.SuperUser)
                {
                    await DisplayInsufficientAccessError(c);
                    return false;
                }
            }
            if (c.Args[1].Equals("reset"))
            {
                return await _ssb.Mod.Pickup.Manager.EvalPickupReset(c);
            }
            if (c.Args[1].Equals("start"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupStart(c);
            }
            else if (c.Args[1].Equals("stop"))
            {
                return await _ssb.Mod.Pickup.Manager.EvalPickupStop(c);
            }
            else if (c.Args[1].Equals("unban"))
            {
                return await _ssb.Mod.Pickup.Manager.EvalPickupUnban(c);
            }
            else if (c.Args[1].Equals("help"))
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
                CommandList.GameCommandPrefix, c.CmdName);
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
            StatusMessage = string.Format(
                "^5[PICKUP] ^7Commands: ^3{0}{1}^7 sign yourself up, ^3{0}{2}^7 remove yourself, ^3{0}{3}^7 sign up as a captain",
                CommandList.GameCommandPrefix, CommandList.CmdPickupAdd,
                CommandList.CmdPickupRemove, CommandList.CmdPickupCap);

            await SendServerSay(c, StatusMessage);

            StatusMessage = string.Format(
                        "^3{0}{1}^7 captains: pick a player, ^3{0}{2}^7 request a substitute for yourself, ^3{0}{3}^7 see who's signed up",
                        CommandList.GameCommandPrefix, CommandList.CmdPickupPick,
                        CommandList.CmdPickupSub, CommandList.CmdPickupWho);

            await SendServerSay(c, StatusMessage);

            StatusMessage = string.Format(
                "^5Privileged cmds: ^3{0}{1} start^7 lockdown server & start, ^3{0}{1} reset^7 reset game," +
                " ^3{0}{1} stop^7 cancel and unlock server, ^3{0}{1} unban^7 unban no-shows",
                CommandList.GameCommandPrefix, CommandList.CmdPickup);

            await SendServerSay(c, StatusMessage);
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