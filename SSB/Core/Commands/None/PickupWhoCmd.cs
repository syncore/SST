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
        private readonly bool _isIrcAccessAllowed = true;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;
        private int _minArgs = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupWhoCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PickupWhoCmd(SynServerBot ssb)
        {
            _ssb = ssb;
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

            string epStr;
            if (_ssb.Mod.Pickup.Manager.IsPickupPreGame)
            {
                if (_ssb.Mod.Pickup.Manager.AvailablePlayers.Count > 0)
                {
                    epStr = string.Format("^2Available players (^7{0}^2): {1}",
                        _ssb.Mod.Pickup.Manager.AvailablePlayers.Count,
                        string.Join(",", _ssb.Mod.Pickup.Manager.AvailablePlayers));
                }
                else
                {
                    epStr = string.Format("^1NO available players.^3 {0}{1}^1 to sign up!",
                        CommandList.GameCommandPrefix, CommandList.CmdPickupAdd);
                }

                StatusMessage = string.Format("^5[PICKUP] {0}", epStr);
                await SendServerSay(c, StatusMessage);
                return true;
            }
            if (_ssb.Mod.Pickup.Manager.IsPickupInProgress)
            {
                if (_ssb.Mod.Pickup.Manager.SubCandidates.Count > 0)
                {
                    epStr = string.Format("^6Available substitutes (^7{0}^6): {1}",
                        _ssb.Mod.Pickup.Manager.SubCandidates.Count,
                        string.Join(",", _ssb.Mod.Pickup.Manager.SubCandidates));
                }
                else
                {
                    epStr = string.Format("^1NO available substitutes.^3 {0}{1}^1 to sign up!",
                        CommandList.GameCommandPrefix, CommandList.CmdPickupAdd);
                }
                StatusMessage = string.Format("^5[PICKUP] {0} ^7- Game size: ^5{1}v{1}",
                    epStr, _ssb.Mod.Pickup.Teamsize);
                await SendServerSay(c, StatusMessage);
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
            return string.Empty;
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
    }
}