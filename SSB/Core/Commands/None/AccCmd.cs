using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: retrieve players' accuracy information.
    /// </summary>
    public class AccCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 0;
        private UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccCmd(SynServerBot ssb)
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
                "^1[ERROR]^3 Usage: {0}{1} [name] ^7- Leave name empty to check all players' accuracies.",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!_ssb.Mod.Accuracy.Active)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 Accuracy module has not been loaded. An admin must first load it with:^7 {0}{1} {2}",
                            CommandProcessor.BotCommandPrefix, CommandProcessor.CmdModule,
                            ModuleCmd.AccuracyArg));
                return;
            }
            if (!Tools.KeyExists(_ssb.BotName, _ssb.ServerInfo.CurrentPlayers))
            {
                Debug.WriteLine("Bot does not exist in internal list of players. Ignoring.");
                return;
            }
            if (_ssb.ServerInfo.CurrentPlayers[_ssb.BotName].Team != Team.Spec)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Accuracies are unavailable because bot owner is currently playing on same account as bot.");
                return;
            }
            switch (c.Args.Length)
            {
                case 1:
                    await GetAccAllPlayers();
                    break;

                case 2:
                    await GetAccSinglePlayer(c);
                    break;

                default:
                    await DisplayArgLengthError(c);
                    break;
            }
        }

        /// <summary>
        ///     Formats a colored accuracy string based on the existing accuracies, if any.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///     A colored string containing the accuracies if they exist, otherwise
        ///     a blank string if they do not.
        /// </returns>
        private string FormatAccString(string player)
        {
            var aBuilder = new StringBuilder();
            AccuracyInfo playerAcc = _ssb.ServerInfo.CurrentPlayers[player].Acc;
            if (!playerAcc.HasAcc())
            {
                Debug.WriteLine(
                    string.Format(
                        "No accuracies other than defaults detected for {0}. Skipping acc string formation.",
                        player));
                return string.Empty;
            }
            if (playerAcc.MachineGun != 0)
            {
                aBuilder.Append(string.Format("^3MG^7 {0}%", playerAcc.MachineGun));
            }
            if (playerAcc.ShotGun != 0)
            {
                aBuilder.Append(string.Format("^3SG^7 {0}%", playerAcc.ShotGun));
            }
            if (playerAcc.GrenadeLauncher != 0)
            {
                aBuilder.Append(string.Format("^2GL^7 {0}%", playerAcc.GrenadeLauncher));
            }
            if (playerAcc.RocketLauncher != 0)
            {
                aBuilder.Append(string.Format("^1RL^7 {0}%", playerAcc.RocketLauncher));
            }
            if (playerAcc.LightningGun != 0)
            {
                aBuilder.Append(string.Format("^7LG {0}%", playerAcc.LightningGun));
            }
            if (playerAcc.RailGun != 0)
            {
                aBuilder.Append(string.Format("^5RG^7 {0}%", playerAcc.RailGun));
            }
            if (playerAcc.PlasmaGun != 0)
            {
                aBuilder.Append(string.Format("^6PG^7 {0}%", playerAcc.PlasmaGun));
            }
            if (playerAcc.Bfg != 0)
            {
                aBuilder.Append(string.Format("^4BFG^7 {0}%", playerAcc.Bfg));
            }
            if (playerAcc.GrapplingHook != 0)
            {
                aBuilder.Append(string.Format("^4GH^7 {0}%", playerAcc.GrapplingHook));
            }
            if (playerAcc.NailGun != 0)
            {
                aBuilder.Append(string.Format("^4NG^7 {0}%", playerAcc.NailGun));
            }
            if (playerAcc.ProximityMineLauncher != 0)
            {
                aBuilder.Append(string.Format("^4PRX^7 {0}%", playerAcc.ProximityMineLauncher));
            }
            if (playerAcc.ChainGun != 0)
            {
                aBuilder.Append(string.Format("^4CG^7 {0}%", playerAcc.ChainGun));
            }
            if (playerAcc.HeavyMachineGun != 0)
            {
                aBuilder.Append(string.Format("^4MG^7 {0}%", playerAcc.HeavyMachineGun));
            }

            return aBuilder.ToString();
        }

        private async Task GetAccAllPlayers()
        {
            // TODO limit on this cmd, once every X seconds since it is spammy
            var bigStr = new StringBuilder();
            foreach (var player in _ssb.ServerInfo.CurrentPlayers)
            {
                if (player.Value.Acc.HasAcc())
                {
                    bigStr.Append(string.Format("^3{0}: {1}", player.Value.ShortName,
                        FormatAccString(player.Value.ShortName)));
                }
                else
                {
                    bigStr.Append(string.Format("^3{0}: ^1{1}^7", player.Value.ShortName, "none"));
                }
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("{0} - Scanned every ^3{1}^7 secs.", bigStr,
                        _ssb.Mod.Accuracy.IntervalBetweenScans));
            }
        }

        private async Task GetAccSinglePlayer(CmdArgs c)
        {
            string player = c.Args[1];
            if (!Tools.KeyExists(player, _ssb.ServerInfo.CurrentPlayers))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 {0} is not currently on the server!",
                        player));
                return;
            }
            string accStr = FormatAccString(player);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0}: {1} - Scanned every ^3{2}^7 secs.",
                player, (string.IsNullOrEmpty(accStr) ? "^1none^7" : accStr),
                _ssb.Mod.Accuracy.IntervalBetweenScans));
        }
    }
}