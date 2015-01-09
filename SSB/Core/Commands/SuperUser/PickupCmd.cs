using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.SuperUser
{
    public class PickupCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.SuperUser;

        public PickupCmd(SynServerBot ssb)
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
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} <start/reset/unban>",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        public async Task ExecAsync(CmdArgs c)
        {
            if (!c.Args[1].Equals("reset") && c.Args[1].Equals("start") && !c.Args[1].Equals("unban"))
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[1].Equals("reset"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupReset();
            }
            else if (c.Args[1].Equals("start"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupStart();
            }
            else if (c.Args[1].Equals("unban"))
            {
                await _ssb.Mod.Pickup.Manager.EvalPickupUnban();
            }
        }
    }
}