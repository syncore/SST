using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Database;
using SST.Enums;
using SST.Util;

namespace SST.Core.Modules
{
    /// <summary>
    ///     Class responsible for evaluating and handling players who quit early,
    ///     as specified by the EarlyQuit module.
    /// </summary>
    public class EarlyQuitHandler
    {
        private readonly DbBans _bansDb;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:EARLYQUIT]";
        private readonly DbQuits _quitsDb;
        private readonly SynServerTool _sst;
        private readonly DbUsers _usersDb;
        private double _banTime;
        private string _banTimeScale;
        private uint _maxQuitsAllowed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EarlyQuitHandler" /> class.
        /// </summary>
        public EarlyQuitHandler(SynServerTool sst)
        {
            _sst = sst;
            _quitsDb = new DbQuits();
            _usersDb = new DbUsers();
            _bansDb = new DbBans();
            GetConfigData();
        }

        /// <summary>
        ///     Evaluates early quitters who leave during countdown and punishes them
        ///     with double the usual punishment if the teams would be made uneven by their quitting.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task EvalCountdownQuitter(string player)
        {
            if (!_sst.Mod.EarlyQuit.Active) return;
            if (_sst.ServerInfo.CurrentServerGameState != QlGameStates.Countdown) return;
            // Only punish if early quitter actually made the teams uneven by quitting
            if (!_sst.ServerInfo.HasEvenTeams())
            {
                // Double penalty for countdown quit
                await ProcessEarlyQuit(player, true);
            }
        }

        /// <summary>
        ///     Evaluates early quitters who leave during games that are in progress.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task EvalInProgressQuitter(string player)
        {
            if (!_sst.Mod.EarlyQuit.Active) return;
            if (_sst.ServerInfo.CurrentServerGameState != QlGameStates.InProgress) return;
            await ProcessEarlyQuit(player, false);
        }

        /// <summary>
        ///     Bans the early quitter.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task BanEarlyQuitter(string player)
        {
            // User already banned by admin (or by another module); do nothing
            if (_bansDb.UserAlreadyBanned(player))
            {
                Log.Write(string.Format(
                    "{0} already existed in ban database; skipping early quit ban.",
                    player), _logClassType, _logPrefix);
                return;
            }
            // Do the ban
            var expirationDate = ExpirationDateGenerator.GenerateExpirationDate(_banTime, _banTimeScale);
            _bansDb.AddUserToDb(player, "earlyQuitMod", DateTime.Now, expirationDate, BanType.AddedByEarlyQuit);

            await
                _sst.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[EARLYQUIT]^7 ^3{0}^7 has quit too many games early and is now banned until:^1 {1}",
                        player, expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));

            // The player might have not actually disconnected but spectated instead, so kickban(QL) immediately
            await _sst.QlCommands.CustCmdKickban(player);

            // UI: reflect changes
            _sst.UserInterface.RefreshCurrentBansDataSource();
        }

        /// <summary>
        ///     Evaluates the user quit count and bans the user if count exceeds a certain value.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task<long> EvaluateUserQuitCount(string player)
        {
            var quitCount = _quitsDb.GetUserQuitCount(player);
            if (quitCount >= _maxQuitsAllowed)
            {
                await BanEarlyQuitter(player);

                Log.Write(string.Format(
                    "Will attempt to add ban for {0} because of too many early quits ({1}, " +
                    "max allowed: {2}) if admin ban doesn't already exist.",
                    player, quitCount, _maxQuitsAllowed), _logClassType, _logPrefix);
            }
            return quitCount;
        }

        /// <summary>
        ///     Gets the configuration data.
        /// </summary>
        private void GetConfigData()
        {
            var cfgHandler = new ConfigHandler();
            cfgHandler.ReadConfiguration();
            _banTime = cfgHandler.Config.EarlyQuitOptions.banTime;
            _banTimeScale = cfgHandler.Config.EarlyQuitOptions.banTimeScale;
            _maxQuitsAllowed = cfgHandler.Config.EarlyQuitOptions.maxQuitsAllowed;
        }

        /// <summary>
        ///     Processes the early quit.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="doublePenalty">
        ///     if set to <c>true</c> double the penalty
        ///     for particularly egregious early quits (i.e. during countdown).
        /// </param>
        private async Task ProcessEarlyQuit(string player, bool doublePenalty)
        {
            if (_usersDb.GetUserLevel(player) >= UserLevel.SuperUser)
            {
                Log.Write(string.Format(
                    "Player {0} quit early, but will not be evaluated for early quitting due to excluded userlevel",
                    player), _logClassType, _logPrefix);
                return;
            }
            if (_quitsDb.UserExistsInDb(player))
            {
                _quitsDb.IncrementUserQuitCount(player, doublePenalty);

                // UI: reflect changes
                _sst.UserInterface.RefreshCurrentQuittersDataSource();
            }
            else
            {
                _quitsDb.AddUserToDb(player, doublePenalty);

                // UI: reflect changes
                _sst.UserInterface.RefreshCurrentQuittersDataSource();
            }

            var qCount = await EvaluateUserQuitCount(player);

            if (doublePenalty)
            {
                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format(
                            "^3{0}'s^7 penalty was doubled for unbalancing teams during match start!",
                            player));

                Log.Write(string.Format("Active player {0} left during count-down. Penalty will be doubled.",
                    player), _logClassType, _logPrefix);
            }
            // Only show the log msg if we're not banning the user this time (ban msg is shown in that case)
            if (qCount < _maxQuitsAllowed)
            {
                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format("^5[EARLYQUIT]^7 Early quit detected and logged for player ^3{0}",
                            player));
            }
        }
    }
}