using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SST.Core.Commands.Admin;
using SST.Core.Modules.Irc;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    ///     Command: retrieve players' accuracy information.
    /// </summary>
    public class AccCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:ACC]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccCmd" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public AccCmd(SynServerTool sst)
        {
            _sst = sst;
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
        /// <returns>
        ///     <c>true</c> if the command was successfully executed, otherwise
        ///     <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (!_sst.Mod.Accuracy.Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Accuracy module has not been loaded. An admin must first load it with:^7 {0}{1} {2}",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}",
                            IrcCommandList.IrcCmdQl, CommandList.CmdModule))
                        : CommandList.CmdModule),
                    ModuleCmd.AccuracyArg);

                await SendServerTell(c, StatusMessage);

                Log.Write(
                    string.Format(
                        "{0} attempted {1} command from {2}, but {3} module is not loaded. Ignoring.",
                        c.FromUser, c.CmdName, ((c.FromIrc) ? "from IRC" : "from in-game"),
                        ModuleCmd.AccuracyArg), _logClassType, _logPrefix);

                return false;
            }

            if (!Helpers.KeyExists(Helpers.GetArgVal(c, 1), _sst.ServerInfo.CurrentPlayers))
            {
                StatusMessage = string.Format("^1[ERROR]^3 {0} is not currently on the server!",
                    Helpers.GetArgVal(c, 1));
                await SendServerTell(c, StatusMessage);

                Log.Write(
                    string.Format(
                        "{0} attempted {1} command from {2}, but {3} module is not loaded. Ignoring.",
                        c.FromUser, c.CmdName, ((c.FromIrc) ? "from IRC" : "from in-game"),
                        ModuleCmd.AccuracyArg), _logClassType, _logPrefix);

                return false;
            }

            if (!Helpers.KeyExists(_sst.AccountName, _sst.ServerInfo.CurrentPlayers))
            {
                Log.Write(
                    string.Format("Bot does not exist in internal list of players. Ignoring {0} command.",
                        c.CmdName), _logClassType, _logPrefix);

                return false;
            }
            if (IsBotPlayer())
            {
                StatusMessage = "^1[ERROR]^3 Accuracies are unavailable because bot owner is" +
                                " currently playing on same account as bot.";
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "Accuracy display is unavailable because owner is playing on some account as bot. Ignoring {0} command.",
                    c.CmdName), _logClassType, _logPrefix);

                return false;
            }

            await ShowAccSinglePlayer(c);
            return true;
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
                "^1[ERROR]^3 Usage: {0}{1} <name> ^7- name is without the clan tag.",
                CommandList.GameCommandPrefix, ((c.FromIrc)
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
        ///     Ends the accuracy read by sending the -acc button command to Quake Live, rejoining the spectators,
        ///     and by clearing the player that is currently being tracked by the tool internally.
        /// </summary>
        private async Task EndAccuracyRead()
        {
            // Send negative state of acc button.
            // Must "re-join" spectators even though we're already there, so that the 1st player whose
            // accuracy is being scanned on the next go-around is correctly detected (QL issue)
            await _sst.QlCommands.SendToQlAsync("-acc;team s", true);
            // Reset internal tracking
            _sst.ServerInfo.PlayerCurrentlyFollowing = string.Empty;
            Log.Write("Ended accuracy scan.", _logClassType, _logPrefix);
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
            var playerAcc = _sst.ServerInfo.CurrentPlayers[player].Acc;
            if (playerAcc == null || !playerAcc.HasAcc())
            {
                Log.Write(string.Format("No accuracy or all zero values detected for {0}. Skipping.",
                    player), _logClassType, _logPrefix);
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
        ///     Determines whether the owner is currently playing on the same account as the bot, and
        ///     prevents accuracy scanning from taking place, in addition to silently disabling it as well.
        /// </summary>
        /// <returns><c>true</c> if the owner is currently playing on the bot account, otherwise <c>false</c>.</returns>
        private bool IsBotPlayer()
        {
            // We've joined the game. Disable scanning.
            var botIsPlayer = (_sst.ServerInfo.CurrentPlayers[_sst.AccountName].Team == Team.Red ||
                               _sst.ServerInfo.CurrentPlayers[_sst.AccountName].Team == Team.Blue);

            if (botIsPlayer)
            {
                // Silently disable, but don't update the config on disk so as to save the owner
                // the trouble of not having to re-enable the accuracy scanner the next time tool is launched
                _sst.Mod.Accuracy.Active = false;

                Log.Write("Bot owner has left spectator mode and is playing on bot account. Silently" +
                          " disabling accuracy display module.", _logClassType, _logPrefix);

                return true;
            }
            return false;
        }

        /// <summary>
        ///     Retrieves the accuracy.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task RetrieveAccuracy(CmdArgs c)
        {
            var player = Helpers.GetArgVal(c, 1);
            _sst.QlCommands.SendToQl("team s", false);
            var id = _sst.ServerEventProcessor.GetPlayerId(player);
            if (id != -1)
            {
                await _sst.QlCommands.SendToQlAsync(string.Format("follow {0}", id), true);
            }

            _sst.ServerInfo.PlayerCurrentlyFollowing = player;

            Log.Write(string.Format("Attempting to follow player {0} to determine accuracy",
                player), _logClassType, _logPrefix);

            await StartAccuracyRead();
            await EndAccuracyRead();
        }

        /// <summary>
        ///     Shows the accuracy of a single player.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task ShowAccSinglePlayer(CmdArgs c)
        {
            var player = Helpers.GetArgVal(c, 1);
            await RetrieveAccuracy(c);
            var accStr = FormatAccString(player);
            StatusMessage = string.Format("^3{0}'s^7 accuracy: {1}",
                player, (string.IsNullOrEmpty(accStr) ? "^1not available^7" : accStr));
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Displaying {0}'s accuracy, which is: {1}",
                player, (string.IsNullOrEmpty(accStr) ? "not available" : Helpers.RemoveQlColorChars(accStr))),
                _logClassType, _logPrefix);
        }

        /// <summary>
        ///     Starts the accuracy read by sending the +acc button command to Quake Live.
        /// </summary>
        private async Task StartAccuracyRead()
        {
            await _sst.QlCommands.SendToQlAsync("+acc", true);
            Log.Write("Started accuracy scan.", _logClassType, _logPrefix);
        }
    }
}