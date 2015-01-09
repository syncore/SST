using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Database;
using SSB.Enum;
using SSB.Util;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for evaluating and handling players who quit early, as specified by the EarlyQuit module.
    /// </summary>
    public class EarlyQuitHandler
    {
        private readonly DbBans _bansDb;
        private readonly DbQuits _quitsDb;
        private readonly SynServerBot _ssb;
        private readonly DbUsers _usersDb;
        private double _banTime;
        private string _banTimeScale;
        private uint _maxQuitsAllowed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EarlyQuitHandler" /> class.
        /// </summary>
        public EarlyQuitHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            _quitsDb = new DbQuits();
            _usersDb = new DbUsers();
            _bansDb = new DbBans();
            GetConfigData();
        }

        /// <summary>
        /// Processes the early quit.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="doublePenalty">if set to <c>true</c> double the penalty
        /// for particularly egregious early quits (i.e. during countdown).</param>
        public async Task ProcessEarlyQuit(string player, bool doublePenalty)
        {
            if (_usersDb.GetUserLevel(player) >= UserLevel.SuperUser)
            {
                Debug.WriteLine(
                    string.Format(
                        "Player {0} quit early, but will not be evaluated for early quitting due to excluded userlevel",
                        player));
                return;
            }
            if (_quitsDb.UserExistsInDb(player))
            {
                _quitsDb.IncrementUserQuitCount(player, doublePenalty);
            }
            else
            {
                _quitsDb.AddUserToDb(player, doublePenalty);
            }
            
            long qCount = await EvaluateUserQuitCount(player);
            
            if (doublePenalty)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^3{0}'s^7 penalty was doubled for unbalancing teams during match start!",
                    player));
                Debug.WriteLine(string.Format("+++++ Active player {0} left during count-down. Penalty will be doubled.",
                    player));
            }
            // Only show the log msg if we're not banning the user this time (ban msg is shown in that case)
            if (qCount < _maxQuitsAllowed)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format("^5[EARLYQUIT]^7 Early quit detected and logged for player ^3{0}",
                            player));
            }
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
                Debug.WriteLine(string.Format(
                    "{0} already existed in ban database; skipping early quit ban.", player));
                return;
            }
            // Do the ban
            DateTime expirationDate = ExpirationDateGenerator.GenerateExpirationDate(_banTime, _banTimeScale);
            _bansDb.AddUserToDb(player, "botInternal", DateTime.Now, expirationDate, BanType.AddedByEarlyQuit);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^5[EARLYQUIT]^7 ^3{0}^7 has quit too many games early and is now banned until:^1 {1}",
                        player, expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));
            
            // The player might have not actually disconnected but spectated instead, so kickban(QL) immediately
            await _ssb.QlCommands.CustCmdKickban(player);
        }

        /// <summary>
        ///     Evaluates the user quit count and bans the user if count exceeds a certain value.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task<long> EvaluateUserQuitCount(string player)
        {
            long quitCount = _quitsDb.GetUserQuitCount(player);
            if (quitCount >= _maxQuitsAllowed)
            {
                await BanEarlyQuitter(player);
                Debug.WriteLine(
                    "Adding ban for {0} because of too many early quits ({1}, max allowed: {2}) if admin ban doesn't already exist.",
                    player, quitCount, _maxQuitsAllowed);
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
    }
}