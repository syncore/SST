﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SST.Database;
using SST.Enums;
using SST.Model;

namespace SST.Core
{
    /// <summary>
    /// Class responsible for automatically banning players if necessary and handling removal of
    /// module-specific bans.
    /// </summary>
    public class BanManager
    {
        private readonly DbBans _banDb;
        private readonly int _kickDelaySecs = 3;
        private readonly int _kickTellDelaySecs = 20;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanManager"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public BanManager(SynServerTool sst)
        {
            _sst = sst;
            _banDb = new DbBans();
        }

        /// <summary>
        /// Check to see whether a time-ban exists for the specified user and kickbans the player
        /// from the server if so; if ban has expired then remove ban.
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
                await
                    _sst.QlCommands.QlCmdSay(
                        string.Format(
                            "^3[=> TIMEBAN IN {0} SECS] ^7Player: ^3{1}^7 was banned on ^1{2}^7. Ban expires: ^2{3}^7. For: ^3{4}",
                            _kickDelaySecs, player,
                            banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo),
                            banInfo.BanExpirationDate.ToString("G",
                                DateTimeFormatInfo.InvariantInfo), reason), false);

                // Wait prior to kicking, so recipient's screen doesn't freeze on 'awaiting snapshot'
                await _sst.QlCommands.QlCmdDelayedTell(string.Format(
                    "^3You will be banned shortly. You were time-banned on {0} for: {1}, ban expires: {2}",
                    banInfo.BanAddedDate.ToString("G", DateTimeFormatInfo.InvariantInfo), reason,
                    banInfo.BanExpirationDate.ToString("G",
                        DateTimeFormatInfo.InvariantInfo)), player, _kickTellDelaySecs);
                await _sst.QlCommands.CustCmdDelayedKickban(player, (_kickDelaySecs + 2));
            }
            else
            {
                await RemoveBan(banInfo);
            }
        }

        /// <summary>
        /// Check to see whether a time-ban exists for the specified group of users and kickbans any
        /// player in the group from the server if so; if ban has expired then remove ban.
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
        /// Removes a user's ban and removes/resets any other extraneous ban-related database
        /// properties for the user.
        /// </summary>
        /// <param name="banInfo">The ban information.</param>
        /// <param name="updateUi">
        /// if set to <c>true</c> then update relevant datasources in the user interface.
        /// </param>
        /// <returns><c>true</c> if the ban was deleted, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method is typically used when access to the user interface is needed and when the
        /// unban command needs to be directly sent to the game. The underlying SST database classes
        /// are not given access to the main SST class.
        /// </remarks>
        public async Task<bool> RemoveBan(BanInfo banInfo, bool updateUi = true)
        {
            if (banInfo == null) return false;
            // If the user was banned for quitting early, then also remove the user from the early
            // quit database when we clear the expired ban
            if (banInfo.BanType == BanType.AddedByEarlyQuit)
            {
                var eQuitDb = new DbQuits();
                eQuitDb.DeleteUserFromDb(banInfo.PlayerName);

                // UI: reflect changes
                if (updateUi)
                {
                    _sst.UserInterface.RefreshCurrentQuittersDataSource();
                }
            }
            // If the user was banned for using too many substitutes in pickup games, reset the
            // sub-used count
            if (banInfo.BanType == BanType.AddedByPickupSubs)
            {
                var pickupDb = new DbPickups();
                pickupDb.ResetSubsUsedCount(banInfo.PlayerName);
            }
            // If the user was banned for too many no-shows in pickup games, reset the user's
            // no-show count
            if (banInfo.BanType == BanType.AddedByPickupNoShows)
            {
                var pickupDb = new DbPickups();
                pickupDb.ResetNoShowCount(banInfo.PlayerName);
            }
            // Remove the ban from the database. This "on-demand" method of removing the ban is
            // preferred instead of using some mechanism such as a timer that would check every X
            // time period; In other words, leave the user banned until he tries to reconnect then
            // silently remove the ban.
            // Note: expired bans are also removed at various points during the bot's existence, for example,
            // they are also removed when admins try to add, list, or check bans with the timeban
            // command or can be removed using the UI.
            _banDb.DeleteUserFromDb(banInfo.PlayerName);

            // remove from QL's external temp kickban system as well
            if (_sst.IsMonitoringServer)
            {
                await _sst.QlCommands.CmdUnban(banInfo.PlayerName);
            }

            // UI: reflect changes
            if (updateUi)
            {
                _sst.UserInterface.RefreshCurrentBansDataSource();
            }

            return true;
        }
    }
}
