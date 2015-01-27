using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    /// Command: Start, stop, reset, unban users from pickup, show pickup help.
    /// </summary>
    /// <remarks>
    /// The overall access level for this command is <see cref="UserLevel"/>.None to allow for general command access,
    /// but certain arguments to this command will require an elevated access (i.e. starting/stopping a pickup game).
    /// </remarks>
    public class PickupCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private DbUsers _userDb;
        private UserLevel _userLevel = UserLevel.None;

        public PickupCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _userDb = new DbUsers();
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
                "^1[ERROR]^3 Usage: {0}{1} <start/stop/reset/unban/help>",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!_ssb.Mod.Pickup.Active)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 Pickup module is not active. An admin must first load it with:^7 {0}{1} {2}",
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdModule,
                            ModuleCmd.PickupArg));
                return;
            }
            
            if (!c.Args[1].Equals("reset") && !c.Args[1].Equals("start") && !c.Args[1].Equals("stop")
                && !c.Args[1].Equals("unban") && !c.Args[1].Equals("help"))
            {
                await DisplayArgLengthError(c);
                return;
            }
            // These arguments to the the pickup command require elevated privileges.
            if (c.Args[1].Equals("reset") || c.Args[1].Equals("start") ||
                c.Args[1].Equals("stop") || c.Args[1].Equals("unban"))
            {
                if (_userDb.GetUserLevel(c.FromUser) < UserLevel.SuperUser)
                {
                    await DisplayInsufficientAccessError();
                    return;
                }
            }
            if (c.Args[1].Equals("reset"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupReset();
            }
            else if (c.Args[1].Equals("start"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupStart();
            }
            else if (c.Args[1].Equals("stop"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupStop();
            }
            else if (c.Args[1].Equals("unban"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupUnban(c.FromUser, c.Args);
            }
            else if (c.Args[1].Equals("help"))
            {
                await DisplayPickupHelp();
            }
        }

        /// <summary>
        /// Displays the insufficient access level error.
        /// </summary>
        private async Task DisplayInsufficientAccessError()
        {
            await _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 You do not have permission to use that command.");
        }

        /// <summary>
        /// Displays the pickup help (the possible pickup commands).
        /// </summary>
        private async Task DisplayPickupHelp()
        {
            await _ssb.QlCommands.QlCmdSay(string.Format("^5[PICKUP] ^7Commands: ^3{0}{1}^7 sign yourself up, ^3{0}{2}^7 remove yourself, ^3{0}{3}^7 sign up as a captain",
                CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd, CommandProcessor.CmdPickupRemove, CommandProcessor.CmdPickupCap));
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0}{1}^7 captains: pick a player, ^3{0}{2}^7 request a substitute for yourself, ^3{0}{3}^7 see who's signed up",
                CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupPick, CommandProcessor.CmdPickupSub, CommandProcessor.CmdPickupWho));
            await _ssb.QlCommands.QlCmdSay(string.Format("^5Privileged cmds: ^3{0}{1} start^7 lockdown server & start, ^3{0}{1} reset^7 reset game," +
                                                         " ^3{0}{1} stop^7 cancel and unlock server, ^3{0}{1} unban^7 unban no-shows",
                                                         CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickup));
        }
    }
}