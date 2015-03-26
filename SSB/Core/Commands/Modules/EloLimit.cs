using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Elo limiter. Kick player if player does not meet elo requirements.
    /// </summary>
    public class EloLimit : IModule
    {
        public const string NameModule = "elo";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EloLimit" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public EloLimit(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            _users = new DbUsers();
            LoadConfig();
        }

        /// <summary>
        ///     Gets or sets the maximum required Elo.
        /// </summary>
        /// <value>
        ///     The maximum required Elo.
        /// </value>
        public static int MaximumRequiredElo { get; set; }

        /// <summary>
        ///     Gets or sets the minimum required Elo.
        /// </summary>
        /// <value>
        ///     The minimum required Elo.
        /// </value>
        public static int MinimumRequiredElo { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets or sets the type of the game for the server.
        /// </summary>
        /// <value>
        ///     The type of the game.
        /// </value>
        public QlGameTypes GameType
        {
            get { return _ssb.ServerInfo.CurrentServerGameType; }
        }

        /// <summary>
        ///     Gets the minimum module arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum module arguments for the IRC command.
        /// </value>
        public int IrcMinModuleArgs
        {
            get { return _qlMinModuleArgs + 1; }
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
        ///     Gets the name of the module.
        /// </summary>
        /// <value>
        ///     The name of the module.
        /// </value>
        public string ModuleName
        {
            get { return NameModule; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinModuleArgs
        {
            get { return _qlMinModuleArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        ///     Removes players from server who do not meet the specified Elo
        ///     requirements immediately after enabling the elo
        ///     limiter.
        /// </summary>
        public async Task BatchRemoveEloPlayers()
        {
            // disable

            var qlrHelper = new QlRanksHelper();
            // First make sure that the elo is correct and fetch if not
            var playersToUpdate = (from player in _ssb.ServerInfo.CurrentPlayers
                                   where qlrHelper.PlayerHasInvalidEloData(player.Value)
                                   select player.Key).ToList();
            if (playersToUpdate.Any())
            {
                var qlr =
                    await
                        qlrHelper.RetrieveEloDataFromApiAsync(_ssb.ServerInfo.CurrentPlayers, playersToUpdate);
                if (qlr == null)
                {
                    await _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^7 Unable to retrieve QlRanks data. Elo limit might not be enforced.");
                }
            }
            // Kick the players from the server...
            foreach (var player in _ssb.ServerInfo.CurrentPlayers.ToList())
            {
                // Still have invalid elo data...skip.
                if (qlrHelper.PlayerHasInvalidEloData(player.Value))
                {
                    Debug.WriteLine(string.Format("Still have invalid elo data for {0}...skipping.",
                        player.Key));
                    break;
                }
                await KickPlayerIfEloNotMet(player.Key);
            }
        }

        /// <summary>
        ///     Checks to see if the player meets the elo requirement on connect.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task CheckPlayerEloRequirement(string player)
        {
            var playerElo = GetEloTypeToCompare(player);
            // Likely invalid, skip.
            if (playerElo == 0) return;
            await KickPlayerIfEloNotMet(player);
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
        ///     Evaluates the elo limit command.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c>if the command evaluation was successful,
        ///     otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < (c.FromIrc ? IrcMinModuleArgs : _qlMinModuleArgs))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            // Disable elo limiter
            if (Helpers.GetArgVal(c, 2).Equals("off"))
            {
                await DisableEloLimiter(c);
                return true;
            }
            if (GameType == QlGameTypes.Unspecified)
            {
                StatusMessage = "^1[ERROR]^7 Unable to process gametype information. Try again.";
                await SendServerTell(c, StatusMessage);
                // Request it
                Debug.WriteLine("ELOLIMITER: Server's gametype is unspecified. Now trying to retrieve.");
                await _ssb.QlCommands.QlCmdServerInfo();
                return false;
            }
            if (c.Args.Length == ((c.FromIrc) ? 4 : 3))
            {
                // Only the minimum elo is specified...
                return await EvalMinEloSpecified(c);
            }
            if (c.Args.Length == ((c.FromIrc) ? 5 : 4))
            {
                // Minimum and maximum elo range is specified...
                return await EvalEloRangeSpecified(c);
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
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <minimumelo> [maximumelo] : minimumelo and" +
                " maximumelo must be >0. minimumelo must also be less than maximumelo.",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            // Check to see if it's the default values
            if (_configHandler.Config.EloLimitOptions.minimumRequiredElo == 0 &&
                _configHandler.Config.EloLimitOptions.maximumRequiredElo == 0)
            {
                Active = false;
            }
            // Only min set
            else if (_configHandler.Config.EloLimitOptions.minimumRequiredElo != 0 &&
                     _configHandler.Config.EloLimitOptions.maximumRequiredElo == 0)
            {
                Active = _configHandler.Config.EloLimitOptions.isActive;
            }
            // Range set
            else if (_configHandler.Config.EloLimitOptions.minimumRequiredElo != 0 &&
                     _configHandler.Config.EloLimitOptions.maximumRequiredElo != 0)
            {
                Active = _configHandler.Config.EloLimitOptions.isActive;
            }
            // Min can't be greater than max
            if ((_configHandler.Config.EloLimitOptions.maximumRequiredElo > 0) &&
                (_configHandler.Config.EloLimitOptions.minimumRequiredElo >
                 _configHandler.Config.EloLimitOptions.maximumRequiredElo))
            {
                Active = false;
                _configHandler.Config.EloLimitOptions.SetDefaults();
                return;
            }
            // Set
            MaximumRequiredElo = _configHandler.Config.EloLimitOptions.maximumRequiredElo;
            MinimumRequiredElo = _configHandler.Config.EloLimitOptions.minimumRequiredElo;
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
        ///     Updates the configuration.
        /// </summary>
        /// <param name="active">
        ///     if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        ///     updating the configuration.
        /// </param>
        public void UpdateConfig(bool active)
        {
            Active = active;
            if (active)
            {
                _configHandler.Config.EloLimitOptions.isActive = true;
                _configHandler.Config.EloLimitOptions.maximumRequiredElo = MaximumRequiredElo;
                _configHandler.Config.EloLimitOptions.minimumRequiredElo = MinimumRequiredElo;
            }
            else
            {
                _configHandler.Config.EloLimitOptions.SetDefaults();
            }

            _configHandler.WriteConfiguration();
        }

        /// <summary>
        ///     Disables the elo limiter.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisableEloLimiter(CmdArgs c)
        {
            UpdateConfig(false);
            StatusMessage = string.Format(
                "^2[SUCCESS]^7 {0} Elo limit ^1disabled^7. Players with any {0} Elo can now play on this server.",
                ((GameType != QlGameTypes.Unspecified) ? GameType.ToString().ToUpper() : string.Empty));
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Evaluates the case in which a minimum and maximum elo range have been specified.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the evaluation was successful, otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> EvalEloRangeSpecified(CmdArgs c)
        {
            int min;
            int max;
            var minAcceptable = ((int.TryParse(Helpers.GetArgVal(c, 2), out min) && min >= 0));
            var maxAcceptable = ((int.TryParse(Helpers.GetArgVal(c, 3), out max) && max > 0));
            if ((!minAcceptable || !maxAcceptable))
            {
                await DisplayArgLengthError(c);
                return false;
            }

            if (max < min)
            {
                await DisplayArgLengthError(c);
                return false;
            }

            MinimumRequiredElo = min;
            MaximumRequiredElo = max;
            UpdateConfig(true);
            StatusMessage = string.Format(
                "^2[SUCCESS]^7 {0} Elo limit ^2ON.^7 Players must have between^2 {1} ^7and^2 {2}^7 {0} Elo to play on this server.",
                GameType.ToString().ToUpper(), min, max);
            await SendServerSay(c, StatusMessage);
            await BatchRemoveEloPlayers();
            return true;
        }

        /// <summary>
        ///     Evaluates the case where only the minimum elo specified.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the evaluation was successful, otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> EvalMinEloSpecified(CmdArgs c)
        {
            int min;
            var minAcceptable = ((int.TryParse(Helpers.GetArgVal(c, 2), out min) && min >= 0));
            if ((!minAcceptable))
            {
                await DisplayArgLengthError(c);
                return false;
            }

            MinimumRequiredElo = min;
            MaximumRequiredElo = 0;
            UpdateConfig(true);
            StatusMessage = string.Format(
                "^2[SUCCESS]^7 {0} Elo limit ^2ON.^7 Players must have at least^2 {1} ^7{0} Elo to play on this server.",
                GameType.ToString().ToUpper(), min);

            await SendServerSay(c, StatusMessage);
            await BatchRemoveEloPlayers();
            return true;
        }

        /// <summary>
        ///     Gets the elo type to compare based on the server's current gametype.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The elo value to use based on the server's current gametype.</returns>
        private long GetEloTypeToCompare(string player)
        {
            if (!Helpers.KeyExists(player, _ssb.ServerInfo.CurrentPlayers)) return 0;
            long elo = 0;
            switch (_ssb.ServerInfo.CurrentServerGameType)
            {
                case QlGameTypes.Ca:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.CaElo;
                    break;

                case QlGameTypes.Ctf:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.CtfElo;
                    break;

                case QlGameTypes.Duel:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.DuelElo;
                    break;

                case QlGameTypes.Ffa:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.FfaElo;
                    break;

                case QlGameTypes.Tdm:
                    elo = _ssb.ServerInfo.CurrentPlayers[player].EloData.CaElo;
                    break;
            }
            return elo;
        }

        /// <summary>
        ///     Kicks the player if player does not meet the server's elo requirements.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task KickPlayerIfEloNotMet(string player)
        {
            // Module values have(n't) been set?
            var hasMaxEloSpecified = MaximumRequiredElo != 0;
            var hasMinEloSpecified = MinimumRequiredElo != 0;

            if (!hasMinEloSpecified) return;

            // Elo limits don't apply to SuperUsers or higher
            if (_users.GetUserLevel(player) >= UserLevel.SuperUser) return;
            // Can't kick ourselves, though QL doesn't allow it anyway, don't show kick msg.
            if (player.Equals(_ssb.AccountName, StringComparison.InvariantCultureIgnoreCase)) return;
            // Get Elo for the current gametype
            var playerElo = GetEloTypeToCompare(player);
            // Player Elo is either invalid or we're not in a QLRanks-supported gametype. Abort.
            if (playerElo == 0) return;
            // Handle minimum
            if (playerElo < MinimumRequiredElo)
            {
                Debug.WriteLine("{0}'s {1} Elo is less than min ({2})...Kicking.", player,
                    GameType.ToString().ToUpper(), MinimumRequiredElo);
                await _ssb.QlCommands.CustCmdKickban(player);
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} {4}",
                        player, GameType.ToString().ToUpper(), playerElo,
                        MinimumRequiredElo,
                        hasMaxEloSpecified ? string.Format("^7Max:^1 {0}", MaximumRequiredElo) : ""));
                return;
                // Handle range
            }
            if (!hasMaxEloSpecified) return;
            if (playerElo <= MaximumRequiredElo) return;
            Debug.WriteLine("{0}'s {1} Elo is greater than max ({2})...Kicking.", player,
                GameType.ToString().ToUpper(), MaximumRequiredElo);
            await _ssb.QlCommands.CustCmdKickban(player);
            await _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} ^7Max:^1 {4}",
                    player, GameType.ToString().ToUpper(), playerElo,
                    MinimumRequiredElo, MaximumRequiredElo));
        }
    }
}