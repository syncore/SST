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
        private int _minArgs = 2;
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
                "^1[ERROR]^3 Usage: {0}{1} <name> ^7- name is without the clan tag.",
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

            if (!Helpers.KeyExists(c.Args[1], _ssb.ServerInfo.CurrentPlayers))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^3 {0} is not currently on the server!",
                        c.Args[1]));
                return;
            }
            
            if (!Helpers.KeyExists(_ssb.BotName, _ssb.ServerInfo.CurrentPlayers))
            {
                Debug.WriteLine("Bot does not exist in internal list of players. Ignoring.");
                return;
            }
            if (IsBotPlayer())
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 Accuracies are unavailable because bot owner is currently playing on same account as bot.");
                return;
            }

            await ShowAccSinglePlayer(c);
        }

        /// <summary>
        ///     Ends the accuracy read by sending the -acc button command to Quake Live, rejoining the spectators,
        ///     and by clearing the player that is currently being tracked by the bot internally.
        /// </summary>
        private async Task EndAccuracyRead()
        {
            // Send negative state of acc button.
            // Must "re-join" spectators even though we're already there, so that the 1st player whose
            // accuracy is being scanned on the next go-around is correctly detected (QL issue)
            await _ssb.QlCommands.SendToQlAsync("-acc;team s", true);
            // Reset internal tracking
            _ssb.ServerInfo.PlayerCurrentlyFollowing = string.Empty;
            Debug.WriteLine("Ended accuracy read.");
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
            if (playerAcc == null || !playerAcc.HasAcc())
            {
                Debug.WriteLine(
                    string.Format(
                        "Accuracy object is null or no accuracies other than defaults detected for {0}. Skipping acc string formation.",
                        player));
                return string.Empty;
            }

            if (playerAcc.MachineGun != 0)
            {
                aBuilder.Append(string.Format("^3MG^7 {0} ", playerAcc.MachineGun));
            }
            if (playerAcc.ShotGun != 0)
            {
                aBuilder.Append(string.Format("^3SG^7 {0} ", playerAcc.ShotGun));
            }
            if (playerAcc.GrenadeLauncher != 0)
            {
                aBuilder.Append(string.Format("^2GL^7 {0} ", playerAcc.GrenadeLauncher));
            }
            if (playerAcc.RocketLauncher != 0)
            {
                aBuilder.Append(string.Format("^1RL^7 {0} ", playerAcc.RocketLauncher));
            }
            if (playerAcc.LightningGun != 0)
            {
                aBuilder.Append(string.Format("^7LG {0} ", playerAcc.LightningGun));
            }
            if (playerAcc.RailGun != 0)
            {
                aBuilder.Append(string.Format("^5RG^7 {0} ", playerAcc.RailGun));
            }
            if (playerAcc.PlasmaGun != 0)
            {
                aBuilder.Append(string.Format("^6PG^7 {0} ", playerAcc.PlasmaGun));
            }
            if (playerAcc.Bfg != 0)
            {
                aBuilder.Append(string.Format("^4BFG^7 {0} ", playerAcc.Bfg));
            }
            if (playerAcc.GrapplingHook != 0)
            {
                aBuilder.Append(string.Format("^4GH^7 {0} ", playerAcc.GrapplingHook));
            }
            if (playerAcc.NailGun != 0)
            {
                aBuilder.Append(string.Format("^4NG^7 {0} ", playerAcc.NailGun));
            }
            if (playerAcc.ProximityMineLauncher != 0)
            {
                aBuilder.Append(string.Format("^4PRX^7 {0} ", playerAcc.ProximityMineLauncher));
            }
            if (playerAcc.ChainGun != 0)
            {
                aBuilder.Append(string.Format("^4CG^7 {0} ", playerAcc.ChainGun));
            }
            if (playerAcc.HeavyMachineGun != 0)
            {
                aBuilder.Append(string.Format("^4MG^7 {0} ", playerAcc.HeavyMachineGun));
            }

            return aBuilder.ToString();
        }

        /// <summary>
        /// Determines whether the owner is currently playing on the same account as the bot, and
        /// prevents accuracy scanning from taking place, in addition to silently disabling it as well.
        /// </summary>
        /// <returns><c>true</c> if the owner is currently playing on the bot account, otherwise <c>false</c>.</returns>
        private bool IsBotPlayer()
        {
            // We've joined the game. Disable scanning.
            bool botIsPlayer = (_ssb.ServerInfo.CurrentPlayers[_ssb.BotName].Team == Team.Red ||
                                _ssb.ServerInfo.CurrentPlayers[_ssb.BotName].Team == Team.Blue);

            if (botIsPlayer)
            {
                // Silently disable, but don't update the config on disk so as to save the owner
                // the trouble of not having to re-enable the accuracy scanner the next time bot is launched
                _ssb.Mod.Accuracy.Active = false;
                Debug.WriteLine(
                    "Owner has left spectator mode and is playing on bot account. Silently disabling accuracy scanning.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves the accuracy.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task RetrieveAccuracy(CmdArgs c)
        {
            var player = c.Args[1];
            _ssb.QlCommands.SendToQl("team s", false);
            int id = _ssb.ServerEventProcessor.GetPlayerId(player);
            if (id != -1)
            {
                await _ssb.QlCommands.SendToQlAsync(string.Format("follow {0}", id), true);
            }
            
            _ssb.ServerInfo.PlayerCurrentlyFollowing = player;
            Debug.WriteLine("Attempting to follow player: " + player);
            await StartAccuracyRead();
            await EndAccuracyRead();
        }

        /// <summary>
        /// Shows the accuracy of a single player.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task ShowAccSinglePlayer(CmdArgs c)
        {
            string player = c.Args[1];
            await RetrieveAccuracy(c);
            string accStr = FormatAccString(player);
            await _ssb.QlCommands.QlCmdSay(string.Format("^3{0}'s^7 accuracy: {1}",
                player, (string.IsNullOrEmpty(accStr) ? "^1not available^7" : accStr)));
        }

        /// <summary>
        ///     Starts the accuracy read by sending the +acc button command to Quake Live.
        /// </summary>
        private async Task StartAccuracyRead()
        {
            //_ssb.QlCommands.SendToQl("+acc", true);
            await _ssb.QlCommands.SendToQlAsync("+acc", true);
            Debug.WriteLine("Starting accuracy read.");
        }
    }
}