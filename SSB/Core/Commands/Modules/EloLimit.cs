using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Model.QlRanks;
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
        private readonly SynServerBot _ssb;
        private readonly DbUsers _users;
        private int _minModuleArgs = 3;

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
        public QlGameTypes GameType { get; set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinModuleArgs
        {
            get { return _minModuleArgs; }
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
        ///     Removes players from server who do not meet the specified Elo requirements immediately after enabling the elo
        ///     limiter.
        /// </summary>
        public async Task BatchRemoveEloPlayers()
        {
            var qlrHelper = new QlRanksHelper();
            // First make sure that the elo is correct and fetch if not
            List<string> playersToUpdate = (from player in _ssb.ServerInfo.CurrentPlayers
                                            where qlrHelper.PlayerHasInvalidEloData(player.Value)
                                            select player.Key).ToList();
            if (playersToUpdate.Any())
            {
                QlRanks qlr =
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
            long playerElo = GetEloTypeToCompare(player);
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
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <minimumelo> [maximumelo] ^7 - minimumelo and maximumelo must be >0. minimumelo must also be less than maximumelo.",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.EloLimitArg));
        }

        /// <summary>
        ///     Evaluates the elo limit command.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }

            if (!CouldGetGameType().Result)
            {
                await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^7 Unable to process gametype information. Try again.");
                return;
            }
            // Disable elo limiter
            if (c.Args[2].Equals("off"))
            {
                await DisableEloLimiter();
                return;
            }
            switch (c.Args.Length)
            {
                // Only the minimum elo is specified...
                case 3:
                    await HandleMinEloSpecified(c);
                    break;
                // Minimum and maximum elo range is specified...
                case 4:
                    await HandleEloRangeSpecified(c);
                    break;
            }
        }

        /// <summary>
        /// Loads the configuration.
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
                (_configHandler.Config.EloLimitOptions.minimumRequiredElo > _configHandler.Config.EloLimitOptions.maximumRequiredElo))
            {
                Active = false;
                _configHandler.Config.EloLimitOptions.SetDefaults();
                return;
            }
            // Set
            MaximumRequiredElo = _configHandler.Config.EloLimitOptions.maximumRequiredElo;
            MinimumRequiredElo = _configHandler.Config.EloLimitOptions.minimumRequiredElo;
            // ...On-startup
            if (Active)
            {
                // ReSharper disable once UnusedVariable
                // Synchronous; initialization
                Task i = Init();
            }
        }

        /// <summary>
        /// Automatically starts the module if an active flag is detected in the configuration.
        /// </summary>
        private async Task Init()
        {
            if (!CouldGetGameType().Result)
            {
                Debug.WriteLine("Unable to detect gametype info...");
                return;
            }
            // Minimum Elo only... No max set
            if ((MinimumRequiredElo > 0) && (MaximumRequiredElo == 0))
            {
                Debug.WriteLine("Auto-detected minimum elo in config file on init. Attempting to scan players...");
                await BatchRemoveEloPlayers();
            }
            // Elo range specified in config
            else if (MaximumRequiredElo > 0 && MinimumRequiredElo > 0)
            {
                Debug.WriteLine("Auto-detected min and max elo range in config file on init. Attempting to scan players...");
                await BatchRemoveEloPlayers();
            }
        }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="active">if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        /// updating the configuration.</param>
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
        ///     Determines whether the current type of server could be obtained.
        /// </summary>
        /// <returns><c>true</c>if the server type could be obtained, otherwise <c>false</c>.</returns>
        private async Task<bool> CouldGetGameType()
        {
            QlGameTypes gameType = _ssb.ServerInfo.CurrentServerGameType;
            if (gameType == QlGameTypes.Unspecified)
            {
                Debug.WriteLine("ELOLIMITER: Server's gametype is unspecified. Now trying to retrieve.");
                await _ssb.QlCommands.QlCmdServerInfo();
                return false;
            }
            GameType = gameType;
            return gameType != QlGameTypes.Unspecified;
        }

        /// <summary>
        ///     Disables the elo limiter.
        /// </summary>
        private async Task DisableEloLimiter()
        {
            UpdateConfig(false);
            await _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]^7 {0} Elo limit ^1disabled^7. Players with any {0} Elo can now play on this server.",
                    ((GameType != QlGameTypes.Unspecified) ? GameType.ToString().ToUpper() : "")));
        }

        /// <summary>
        ///     Gets the elo type to compare based on the server's current gametype.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The elo value to use based on the server's current gametype.</returns>
        private long GetEloTypeToCompare(string player)
        {
            if (!Tools.KeyExists(player, _ssb.ServerInfo.CurrentPlayers)) return 0;
            long elo = 0;
            switch (GameType)
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
        ///     Handles the case in which a minimum and maximum elo range have been specified.
        /// </summary>
        /// <param name="c">The command args.</param>
        private async Task HandleEloRangeSpecified(CmdArgs c)
        {
            int min;
            int max;
            bool minAcceptable = ((int.TryParse(c.Args[2], out min) && min > 0));
            bool maxAcceptable = ((int.TryParse(c.Args[3], out max) && max > 0));
            if ((!minAcceptable || !maxAcceptable))
            {
                await DisplayArgLengthError(c);
                return;
            }

            if (max < min)
            {
                await DisplayArgLengthError(c);
                return;
            }

            MinimumRequiredElo = min;
            MaximumRequiredElo = max;
            UpdateConfig(true);

            await _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]: ^2{0}^7 Elo limit ^2ON.^7 Players must have between^2 {1} ^7and^2 {2}^7 {0} Elo to play on this server.",
                    GameType.ToString().ToUpper(), min, max
                    ));
            await BatchRemoveEloPlayers();
        }

        /// <summary>
        ///     Handles the case where only the minimum elo specified.
        /// </summary>
        /// <param name="c">The command args.</param>
        private async Task HandleMinEloSpecified(CmdArgs c)
        {
            int min;
            bool minAcceptable = ((int.TryParse(c.Args[2], out min) && min > 0));
            if ((!minAcceptable))
            {
                await DisplayArgLengthError(c);
                return;
            }

            MinimumRequiredElo = min;
            MaximumRequiredElo = 0;
            UpdateConfig(true);

            await _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]: ^2{0}^7 Elo limit ^2ON.^7 Players must have at least^2 {1} ^7{0} Elo to play on this server.",
                    GameType.ToString().ToUpper(), min));
            await BatchRemoveEloPlayers();
        }

        /// <summary>
        ///     Kicks the player if player does not meet the server's elo requirements.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task KickPlayerIfEloNotMet(string player)
        {
            bool hasMaxElo = MaximumRequiredElo != 0;
            bool hasMinElo = MinimumRequiredElo != 0;

            if (!hasMinElo) return;

            // Elo limits don't apply to SuperUsers or higher
            if (_users.GetUserLevel(player) >= UserLevel.SuperUser) return;
            // Can't kick ourselves, though QL doesn't allow it anyway, don't show kick msg.
            if (player.Equals(_ssb.BotName, StringComparison.InvariantCultureIgnoreCase)) return;

            long playerElo = GetEloTypeToCompare(player);

            if (playerElo == 0) return;
            if (playerElo < MinimumRequiredElo)
            {
                Debug.WriteLine("{0}'s {1} Elo is less than min ({2})...Kicking.", player,
                    GameType.ToString().ToUpper(), MinimumRequiredElo);
                await _ssb.QlCommands.CustCmdKickban(player);
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} {4}",
                        player, GameType.ToString().ToUpper(), playerElo,
                        MinimumRequiredElo, hasMaxElo ? string.Format("^7Max:^1 {0}", MaximumRequiredElo) : ""));
                return;
            }
            if (!hasMaxElo) return;
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