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
        private readonly Bans _bansDb;
        private readonly Quits _quitsDb;
        private readonly SynServerBot _ssb;
        private readonly Users _usersDb;
        private double _banTime;
        private string _banTimeScale;
        private uint _maxQuitsAllowed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EarlyQuitHandler" /> class.
        /// </summary>
        public EarlyQuitHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            _quitsDb = new Quits();
            _usersDb = new Users();
            _bansDb = new Bans();
            GetConfigData();
        }

        /// <summary>
        ///     Processes the early quit.
        /// </summary>
        /// <param name="player">The player.</param>
        public async Task ProcessEarlyQuit(string player)
        {
            if (_usersDb.GetUserLevel(player) >= UserLevel.SuperUser)
            {
                Debug.WriteLine(
                    string.Format(
                        "Player {0} quit early, but will not be evaluated for early quitting due to excluded userlevel",
                        player));
                return;
            }
            long qCount = 0;
            if (_quitsDb.UserExistsInDb(player))
            {
                _quitsDb.IncrementUserQuitCount(player);
                qCount = await EvaluateUserQuitCount(player);
            }
            else
            {
                _quitsDb.AddUserToDb(player);
            }
            // Only show this msg if we're not going to ban the user and show the ban msg anyway
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
                        "^5[EARLYQUIT]^7 ^3{0}^7 has quit too many games early. ^3{0}^7 is now banned until:^1 {1}",
                        player, expirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));
        }

        /// <summary>
        ///     Evaluates the user quit count and bans the user if count exceeds a certain value.
        /// </summary>
        /// <param name="player">The player.</param>
        private async Task<long> EvaluateUserQuitCount(string player)
        {
            long quitCount = _quitsDb.GetUserQuitCount(player);
            if (quitCount > _maxQuitsAllowed)
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