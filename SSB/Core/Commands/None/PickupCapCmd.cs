using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: sign up to be a captain for a pickup game.
    /// </summary>
    public class PickupCapCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupCapCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PickupCapCmd(SynServerBot ssb)
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

            await _ssb.Mod.Pickup.Manager.Captains.ProcessAddCaptain(c.FromUser);
        }
    }
}