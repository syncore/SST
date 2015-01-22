using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: list the eligible players (or sub candidates)
    /// </summary>
    public class PickupWhoCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupWhoCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PickupWhoCmd(SynServerBot ssb)
        {
            _ssb = ssb;
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
        /// <remarks>
        ///     Not implemented because the cmd in this class requires no args.
        /// </remarks>
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
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

            string epStr;
            if (_ssb.Mod.Pickup.Manager.IsPickupPreGame)
            {
                if (_ssb.Mod.Pickup.Manager.EligiblePlayers.Count > 0)
                {
                    epStr = string.Format("Available players are: ^2{0}",
                        string.Join(",", _ssb.Mod.Pickup.Manager.EligiblePlayers));
                }
                else
                {
                    epStr = string.Format("^1NO available players.^3 {0}{1}^7 to sign up!",
                        CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd);
                }

                await _ssb.QlCommands.QlCmdSay(string.Format("^5[PICKUP]^7 {0}", epStr));
            }
            else if (_ssb.Mod.Pickup.Manager.IsPickupInProgress)
            {
                if (_ssb.Mod.Pickup.Manager.InProgressSubCandidates.Count > 0)
                {
                    epStr = string.Format("Available substitutes are: ^2{0}",
                        string.Join(",", _ssb.Mod.Pickup.Manager.InProgressSubCandidates));
                }
                else
                {
                    epStr = string.Format("^1NO available substitutes.^3 {0}{1}^7 to sign up!",
                        CommandProcessor.BotCommandPrefix, CommandProcessor.CmdPickupAdd);
                }
                await _ssb.QlCommands.QlCmdSay(string.Format("^5[PICKUP]^7 {0}", epStr));
            }
        }
    }
}