using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enums;
using SSB.Model;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for automatically banning players if necessary and for removing bans.
    /// </summary>
    public class PlayerAutoBanner
    {
        private readonly DbBans _banDb;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerAutoBanner" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public PlayerAutoBanner(SynServerBot ssb)
        {
            _ssb = ssb;
            _banDb = new DbBans();
        }

        /// <summary>
        ///     Check to see whether a time-ban exists for the specified user and
        ///     kickbans the player from the server if so; if ban has expired then remove ban.
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
                string reason;
                switch (banInfo.BanType)
                {
                    case BanType.AddedByAdmin:
                        reason = "admin ban";
                        break;

                    case BanType.AddedByEarlyQuit:
                        reason = "early quits";
                        break;

                    default:
                        reason = "unspecified";
                        break;
                }
                await _ssb.QlCommands.CustCmdKickban(player);
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^3[=> TIMEBAN] ^7Player: ^3{0}^7 was banned on ^1{1}^7. Ban expires: ^2{2}^7. For: ^3{3}",
                            player,
                            banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                            banInfo.BanExpirationDate.ToString("G",
                                DateTimeFormatInfo.InvariantInfo), reason));
            }
            else
            {
                await RemoveBan(player, banInfo);
            }
        }

        /// <summary>
        ///     Check to see whether a time-ban exists for the specified group of users and
        ///     kickbans any player in the group from the server if so; if ban has expired then remove ban.
        /// </summary>
        /// <param name="players">The players to check.</param>
        public async Task CheckForBans(Dictionary<string, PlayerInfo> players)
        {
            foreach (var player in players.ToList())
            {
                await CheckForBans(player.Key);
            }
        }

        /// <summary>
        ///     Removes a user's ban and removes/resets any other extraneous ban-related database properties for the user.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="banInfo">The ban information.</param>
        /// s
        public async Task RemoveBan(string player, BanInfo banInfo)
        {
            if (banInfo == null) return;
            // If the user was banned for quitting early, then also remove the user from the early quit database
            // when we clear the expired ban
            if (banInfo.BanType == BanType.AddedByEarlyQuit)
            {
                var eQuitDb = new DbQuits();
                eQuitDb.DeleteUserFromDb(player);
            }
            // If the user was banned for using too many substitutes in pickup games, reset the sub-used count
            if (banInfo.BanType == BanType.AddedByPickupSubs)
            {
                var pickupDb = new DbPickups();
                pickupDb.ResetSubsUsedCount(player);
            }
            // If the user was banned for too many no-shows in pickup games, reset the user's no-show count
            if (banInfo.BanType == BanType.AddedByPickupNoShows)
            {
                var pickupDb = new DbPickups();
                pickupDb.ResetNoShowCount(player);
            }
            // Remove the ban from the database. This "on-demand" method of removing the ban is
            // preferred instead of using some mechanism such as a timer that would check every X time period;
            // In other words, leave user banned until he tries to reconnect then silently remove the ban. Note:
            // expired bans are also removed when admins try to add, list, or check bans with the timeban command.
            _banDb.DeleteUserFromDb(player);
            // remove from QL's external temp kickban system as well
            await _ssb.QlCommands.SendToQlAsync(string.Format("unban {0}", player), false);
        }
    }
}