using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;
using SSB.Model.QlRanks;
using SSB.Util;

namespace SSB.Core.Commands.Limits
{
    public class EloLimit : ILimit
    {
        private readonly SynServerBot _ssb;
        private readonly Users _users;
        private int _minLimitArgs = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EloLimit" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public EloLimit(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new Users();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the elo limit is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the elo limit is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsLimitActive { get; set; }

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
        /// Gets the minimum arguments.
        /// </summary>
        /// <value>
        /// The minimum arguments.
        /// </value>
        public int MinLimitArgs { get { return _minLimitArgs; } }

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
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^7 Unable to retrieve QlRanks data. Elo limit might not be enforced.");
                }
            }
            // Kick the players from the server...
            foreach (var player in _ssb.ServerInfo.CurrentPlayers)
            {
                // Still have invalid elo data...skip.
                if (qlrHelper.PlayerHasInvalidEloData(player.Value))
                {
                    Debug.WriteLine(string.Format("Still have invalid elo data for {0}...skipping.",
                        player.Key));
                    break;
                }
                KickPlayerIfEloNotMet(player.Key);
            }
        }

        /// <summary>
        ///     Checks to see if the player meets the elo requirement on connect.
        /// </summary>
        /// <param name="player">The player.</param>
        public void CheckPlayerEloRequirement(string player)
        {
            long playerElo = GetEloTypeToCompare(player);
            // Likely invalid, skip.
            if (playerElo == 0) return;
            KickPlayerIfEloNotMet(player);
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public void DisplayArgLengthError(CmdArgs c)
        {
            _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <minimumelo> [maximumelo] ^7 - minimumelo must be >0 & maximumelo must be >600",
                CommandProcessor.BotCommandPrefix, c.CmdName, LimitCmd.EloLimitArg));
        }

        /// <summary>
        ///     Evaluates the elo limit command.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task EvalLimitCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minLimitArgs)
            {
                DisplayArgLengthError(c);
                return;
            }

            if (!CouldGetServerType())
            {
                _ssb.QlCommands.QlCmdSay("^1[ERROR]^7 Unable to process gametype information. Try again.");
                return;
            }
            // Disable elo limiter
            if (c.Args[2].Equals("off"))
            {
                DisableEloLimiter();
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
        ///     Determines whether the current type of server could be obtained.
        /// </summary>
        /// <returns><c>true</c>if the server type could be obtained, otherwise <c>false</c>.</returns>
        private bool CouldGetServerType()
        {
            _ssb.QlCommands.QlCvarG_gametype();
            return _ssb.ServerInfo.CurrentGameType != QlGameTypes.Unspecified;
        }

        /// <summary>
        ///     Disables the elo limiter.
        /// </summary>
        private void DisableEloLimiter()
        {
            IsLimitActive = false;
            _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]^7 {0} Elo limit ^1disabled^7. Players with any {0} Elo can now play on this server.",
                    _ssb.ServerInfo.CurrentGameType.ToString().ToUpper()));
        }

        /// <summary>
        ///     Gets the elo type to compare based on the server's current gametype.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The elo value to use based on the server's current gametype.</returns>
        private long GetEloTypeToCompare(string player)
        {
            PlayerInfo pi;
            if (!_ssb.ServerInfo.CurrentPlayers.TryGetValue(player, out pi)) return 0;
            long elo = 0;
            switch (_ssb.ServerInfo.CurrentGameType)
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
            bool maxAcceptable = ((int.TryParse(c.Args[3], out max) && max > 600));
            if ((!minAcceptable || !maxAcceptable))
            {
                DisplayArgLengthError(c);
                return;
            }
            IsLimitActive = true;
            MinimumRequiredElo = min;
            MaximumRequiredElo = max;
            _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]: ^2{0}^7 Elo limit ^2ON.^7 Players must have between^2 {1} ^7and^2 {2}^7 {0} Elo to play on this server.",
                    _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), min, max
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
                DisplayArgLengthError(c);
                return;
            }
            IsLimitActive = true;
            MinimumRequiredElo = min;
            _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^2[SUCCESS]: ^2{0}^7 Elo limit ^2ON.^7 Players must have at least^2 {1} ^7{0} Elo to play on this server.",
                    _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), min));
            await BatchRemoveEloPlayers();
        }

        /// <summary>
        ///     Kicks the player if player does not meet the server's elo requirements.
        /// </summary>
        /// <param name="player">The player.</param>
        private void KickPlayerIfEloNotMet(string player)
        {
            bool hasMaxElo = MaximumRequiredElo != 0;
            // Elo limits don't apply to SuperUsers or higher
            if (_users.GetUserLevel(player) >= UserLevel.SuperUser) return;
            // Can't kick ourselves, though QL doesn't allow it anyway, don't show kick msg.
            if (player.Equals(_ssb.BotName, StringComparison.InvariantCultureIgnoreCase)) return;

            long playerElo = GetEloTypeToCompare(player);

            if (playerElo == 0) return;
            if (playerElo < MinimumRequiredElo)
            {
                Debug.WriteLine("{0}'s {1} Elo is less than min ({2})...Kicking.", player,
                    _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), MinimumRequiredElo);
                _ssb.QlCommands.QlCmdKickban(player);
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} {4}",
                        player, _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), playerElo,
                        MinimumRequiredElo, hasMaxElo ? string.Format("^7Max:^1 {0}", MaximumRequiredElo) : ""));
                return;
            }
            if (!hasMaxElo) return;
            if (playerElo <= MaximumRequiredElo) return;
            Debug.WriteLine("{0}'s {1} Elo is greater than max ({2})...Kicking.", player,
                _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), MaximumRequiredElo);
            _ssb.QlCommands.QlCmdKickban(player);
            _ssb.QlCommands.QlCmdSay(
                string.Format(
                    "^3[=> KICK]: ^1{0}^7 ({1} Elo:^1 {2}^7) does not meet this server's Elo requirements. Min:^2 {3} Max:^1 {4}",
                    player, _ssb.ServerInfo.CurrentGameType.ToString().ToUpper(), playerElo,
                    MinimumRequiredElo, MaximumRequiredElo));
        }
    }
}