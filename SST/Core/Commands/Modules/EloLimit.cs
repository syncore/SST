using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Database;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Elo limiter. Kick player if player does not meet elo requirements.
    /// </summary>
    public class EloLimit : IModule
    {
        public const string NameModule = "elo";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:ELOLIMIT]";
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;
        private readonly DbUsers _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EloLimit" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public EloLimit(SynServerTool sst)
        {
            _sst = sst;
            _configHandler = new ConfigHandler();
            _users = new DbUsers();
            LoadConfig();
        }

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
            get { return _sst.ServerInfo.CurrentServerGameType; }
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
        ///     Gets or sets the maximum required Elo.
        /// </summary>
        /// <value>
        ///     The maximum required Elo.
        /// </value>
        public int MaximumRequiredElo { get; set; }

        /// <summary>
        ///     Gets or sets the minimum required Elo.
        /// </summary>
        /// <value>
        ///     The minimum required Elo.
        /// </value>
        public int MinimumRequiredElo { get; set; }

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
            var playersToUpdate = (from player in _sst.ServerInfo.CurrentPlayers
                                   where qlrHelper.PlayerHasInvalidEloData(player.Value)
                                   select player.Key).ToList();
            if (playersToUpdate.Any())
            {
                var qlr =
                    await
                        qlrHelper.RetrieveEloDataFromApiAsync(_sst.ServerInfo.CurrentPlayers, playersToUpdate);
                if (qlr == null)
                {
                    await _sst.QlCommands.QlCmdSay(
                        "^1[ERROR]^7 Unable to retrieve QLRanks data. Elo limit might not be enforced.");

                    Log.Write("QLRanks Elo data could not be retrieved. Elo limit might not be enforced.",
                        _logClassType, _logPrefix);
                }
            }
            // Kick the players from the server...
            foreach (var player in _sst.ServerInfo.CurrentPlayers.ToList())
            {
                // Still have invalid elo data...skip.
                if (qlrHelper.PlayerHasInvalidEloData(player.Value))
                {
                    Log.Write(
                        string.Format(
                            "Still have invalid Elo data for player {0}; player won't be evaluated for kicking.",
                            player.Key), _logClassType, _logPrefix);
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
                Log.Write(string.Format(
                    "Could not determine server's gametype when trying to enable Elo limiter module from {0}. Will re-request game info.",
                    (c.FromIrc ? "IRC" : "in-game")), _logClassType, _logPrefix);
                await _sst.QlCommands.QlCmdServerInfo();
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
                Log.Write("Minimum required Elo was greater than maximum Elo on initial load of Elo limiter" +
                          " module configuration. Will not enable & will set defaults.", _logClassType,
                    _logPrefix);
                Active = false;
                _configHandler.Config.EloLimitOptions.SetDefaults();
                return;
            }
            // Set
            MaximumRequiredElo = _configHandler.Config.EloLimitOptions.maximumRequiredElo;
            MinimumRequiredElo = _configHandler.Config.EloLimitOptions.minimumRequiredElo;

            Log.Write(string.Format(
                "Active: {0}, minimum Elo required: {1}, maxmium Elo: {2}",
                (Active ? "YES" : "NO"), ((MinimumRequiredElo == 0) ? "none" : MinimumRequiredElo.ToString()),
                ((MaximumRequiredElo == 0) ? "none" : MaximumRequiredElo.ToString())),
                _logClassType, _logPrefix);
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
        ///     Updates the configuration.
        /// </summary>
        /// <param name="active">
        ///     if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        ///     updating the configuration.
        /// </param>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            _configHandler.Config.EloLimitOptions.isActive = active;
            _configHandler.Config.EloLimitOptions.maximumRequiredElo = MaximumRequiredElo;
            _configHandler.Config.EloLimitOptions.minimumRequiredElo = MinimumRequiredElo;

            _configHandler.WriteConfiguration();

            // Reflect changes in UI
            _sst.UserInterface.PopulateModEloLimiterUi();
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

            Log.Write("Received in-game request to disable Elo limiter module. Disabling.",
                _logClassType, _logPrefix);
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

            Log.Write(string.Format(
                "Received {0} request from {1} to enable Elo limiter module with {2}-{3} min-max Elo range. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser, min, max), _logClassType, _logPrefix);

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

            Log.Write(string.Format(
                "Received {0} request from {1} to enable Elo limiter module with {2} minimum Elo. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser, min), _logClassType, _logPrefix);
            return true;
        }

        /// <summary>
        ///     Gets the elo type to compare based on the server's current gametype.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The elo value to use based on the server's current gametype.</returns>
        private long GetEloTypeToCompare(string player)
        {
            if (!Helpers.KeyExists(player, _sst.ServerInfo.CurrentPlayers)) return 0;
            long elo = 0;
            switch (_sst.ServerInfo.CurrentServerGameType)
            {
                case QlGameTypes.Ca:
                    elo = _sst.ServerInfo.CurrentPlayers[player].EloData.CaElo;
                    break;

                case QlGameTypes.Ctf:
                    elo = _sst.ServerInfo.CurrentPlayers[player].EloData.CtfElo;
                    break;

                case QlGameTypes.Duel:
                    elo = _sst.ServerInfo.CurrentPlayers[player].EloData.DuelElo;
                    break;

                case QlGameTypes.Ffa:
                    elo = _sst.ServerInfo.CurrentPlayers[player].EloData.FfaElo;
                    break;

                case QlGameTypes.Tdm:
                    elo = _sst.ServerInfo.CurrentPlayers[player].EloData.CaElo;
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
            if (player.Equals(_sst.AccountName, StringComparison.InvariantCultureIgnoreCase)) return;
            // Get Elo for the current gametype
            var playerElo = GetEloTypeToCompare(player);
            // Player Elo is either invalid or we're not in a QLRanks-supported gametype. Abort.
            if (playerElo == 0) return;
            // Handle minimum
            if (playerElo < MinimumRequiredElo)
            {
                Log.Write(
                    string.Format("{0}'s {1} Elo is less than minimum required ({2})...Kicking player.",
                        player,
                        GameType.ToString().ToUpper(), MinimumRequiredElo), _logClassType, _logPrefix);

                await _sst.QlCommands.CustCmdKickban(player);
                await _sst.QlCommands.QlCmdSay(
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

            Log.Write(
                string.Format("{0}'s {1} Elo is greater than maximum allowed ({2})...Kicking player.", player,
                    GameType.ToString().ToUpper(), MaximumRequiredElo), _logClassType, _logPrefix);

            await _sst.QlCommands.CustCmdKickban(player);
            await _sst.QlCommands.QlCmdSay(
                string.Format(
                    "^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} ^7Max:^1 {4}",
                    player, GameType.ToString().ToUpper(), playerElo,
                    MinimumRequiredElo, MaximumRequiredElo));
        }
    }
}