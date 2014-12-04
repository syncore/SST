using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Model;

namespace SSB.Core
{
    /// <summary>
    /// Class responsible for automatically banning players if necessary and for removing bans.
    /// </summary>
    public class PlayerAutoBanner
    {
        private readonly SynServerBot _ssb;
        private readonly Bans _banDb;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAutoBanner"/> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PlayerAutoBanner(SynServerBot ssb)
        {
            _ssb = ssb;
            _banDb = new Bans();
        }

        /// <summary>
        /// Check to see whether a time-ban exists for the specified user and
        /// kickbans the player from the server if so; if ban has expired then remove ban.
        /// </summary>
        /// <param name="player">The player to check.</param>
        public async Task CheckForBans(string player)
        {
            player = player.ToLowerInvariant();
            if (!_banDb.UserAlreadyBanned(player)) return;
            var banInfo = _banDb.GetBanInfo(player);
            if (banInfo == null) return;

            if (banInfo.BanExpirationDate == default(DateTime)) return;
            if (DateTime.Now <= banInfo.BanExpirationDate)
            {
                await _ssb.QlCommands.CustCmdKickban(player);
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^3[=> TIMEBAN] ^7Player: ^3{0}^7 was banned on ^1{1}^7. Ban will expire on: ^2{2}",
                            player, banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                            banInfo.BanExpirationDate.ToString("G", DateTimeFormatInfo.InvariantInfo)));
            }
            else
            {
                // Remove the ban from the database. This "on-demand" method of removing the ban is
                // preferred instead of using some mechanism such as a timer that would check every X time period;
                // In other words, leave user banned until he tries to reconnect then silently remove the ban. Note:
                // expired bans are also remoevd when admins try to add, list, or check bans with the timeban command.
                _banDb.DeleteUserFromDb(player);
            }
        }

        /// <summary>
        /// Check to see whether a time-ban exists for the specified group of users and
        /// kickbans any player in the group from the server if so; if ban has expired then remove ban.
        /// </summary>
        /// <param name="players">The players to check.</param>
        public async Task CheckForBans(Dictionary<string, PlayerInfo> players)
        {
            foreach (var player in players.ToList())
            {
                await CheckForBans(player.Key);
            }
        }
    }
}