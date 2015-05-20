using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SST.Enums;
using SST.Model;
using SST.Util;

namespace SST.Core
{
    /// <summary> Quick and dirty team balancer according to Elo ranking Algorithm idea by 'Nari'
    /// <see cref="http://forums.faforever.com/forums//viewtopic.php?f=42&t=3659&start=10" /> </summary>
    public class TeamBalancer
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[TEAMBALANCE]";

        /// <summary>
        /// Main method for performing the team balance operation.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <param name="gametype">The gametype.</param>
        /// <param name="desiredTeam">The desired team to return.</param>
        /// <returns>A list of players for the desired team.</returns>
        public List<PlayerInfo> DoBalance(IList<PlayerInfo> players, QlGameTypes gametype, Team desiredTeam)
        {
            // Ascending array sort
            var playersSortedByElo =
                players.OrderBy(player => player.EloData.GetEloFromGameType(gametype)).ToList();
            var numPlayers = playersSortedByElo.Count;
            var redTeam = new List<PlayerInfo>();
            var blueTeam = new List<PlayerInfo>();
            // The first four players are predetermined: Highest ranked player to red
            redTeam.Add(playersSortedByElo[numPlayers - 1]);
            // Second highest ranked player to blue
            blueTeam.Add(playersSortedByElo[numPlayers - 2]);
            // Third highest ranked player to blue
            blueTeam.Add(playersSortedByElo[numPlayers - 3]);
            // Fourth highest ranked player to red
            redTeam.Add(playersSortedByElo[numPlayers - 4]);
            // Determine the ordering for the remaining players.
            for (var i = 5; i < numPlayers; i++)
            {
                // We're only interested in the even iterations
                if (i % 2 == 0) i++;

                var lowVsHigh =
                    CompareBothPlayers(
                        playersSortedByElo[numPlayers - i].EloData.GetEloFromGameType(gametype),
                        playersSortedByElo[numPlayers - (i + 1)].EloData.GetEloFromGameType(gametype),
                        redTeam, blueTeam, gametype);
                // better ranked player: playerElos[numPlayers - i] worse ranked player:
                // playerElos[numPlayers - (i+1)]
                if (lowVsHigh == 0)
                {
                    redTeam.Add(playersSortedByElo[numPlayers - (i + 1)]);
                    blueTeam.Add(playersSortedByElo[numPlayers - i]);
                }
                else
                {
                    redTeam.Add(playersSortedByElo[numPlayers - i]);
                    blueTeam.Add(playersSortedByElo[numPlayers - (i + 1)]);
                }
            }

            if (desiredTeam == Team.Blue)
            {
                // Only show once
                DisplayStats(redTeam, blueTeam, gametype);
            }
            return (desiredTeam == Team.Red ? redTeam : blueTeam);
        }

        /// <summary>
        /// Adds a player's Elo to the existing Elo of a team to determine what the average Elo
        /// would be if that player were on that team.
        /// </summary>
        /// <param name="team">The team.</param>
        /// <param name="playerEloToAdd">The player's Elo to add.</param>
        /// <param name="gametype">The gametype.</param>
        /// <returns>
        /// The average once the player's Elo has been added to the existing team's Elo.
        /// </returns>
        private long AddToTeamGetAvg(IList<PlayerInfo> team, long playerEloToAdd, QlGameTypes gametype)
        {
            var teamExistingTotal = team.Sum(player => player.EloData.GetEloFromGameType(gametype));
            return ((teamExistingTotal + playerEloToAdd) / (team.Count() + 1));
        }

        /// <summary>
        /// Compares a higher ranked player's Elo versus a lower ranked player's Elo.
        /// </summary>
        /// <param name="betterRankedPlayerElo">The better ranked player's Elo.</param>
        /// <param name="worseRankedPlayerElo">The worse ranked player's Elo.</param>
        /// <param name="teamRed">The red team.</param>
        /// <param name="teamBlue">The blue team.</param>
        /// <param name="gametype">The gametype.</param>
        /// <returns>0 or 1 to indicate which of the players has a higher Elo value.</returns>
        /// <remarks>See comment for explanation.</remarks>
        private long CompareBothPlayers(long betterRankedPlayerElo, long worseRankedPlayerElo,
            IList<PlayerInfo> teamRed,
            IList<PlayerInfo> teamBlue, QlGameTypes gametype)
        {
            /* Example:
                * Elos (asc): 860,1000,1200,1300,1750,1920,2000,2200
                * RED          BLUE
                * 2200         2000
                * 1750         1920
                * (first 4 players pre-determined. now need to determine last 4):
             * -------------------------------------------------------------------
             *    5th-ranked (1300) vs. 6th-ranked (1200) players:
             *    RED          BLUE
             *    1300          1200        t1A: RED avg with 1300 added: 1750
             *                              t2A: BLUE avg with 1200 added: 1706
             *                              t1A t2A difference: abs(t1A-t2A): abs(1750-1706): 44
             *
             *    1200          1300        t1B: RED avg with 1200 added: 1716
             *                              t2B: BLUE avg 1300 added: 1740
             *                              t1B t2B difference: abs(t1B-t2B): abs(1716-1740): 24
             *
             *                              t1Bt2B difference (24) <= t1At2A difference (44),
             *                              so place 5th & 6th ranked players according to the scenario
             *                              with the lower difference (24). 5th ranked(1200) to RED,
             *                              6th ranked(1300) to BLUE.
             *
             *      Now:
             *      RED              BLUE
             *      2200             2000
             *      1750             1920
             *      1200             1300
             * (Now first 6 players are determined. Now need to determine the final 2 players):
             * --------------------------------------------------------------------------------
             *    7th-ranked (1000) vs. 8th-ranked (860) players:
             *    RED          BLUE
             *    1000         860          t1A: RED avg with 1000 added: 1537
             *                              t2A: BLUE avg with 860 added: 1520
             *                              t1A t2A difference: abs(t1A-t2A): abs(1537-1520): 17
             *
             *    860         1000          t1B: RED avg with 860 added: 1502
             *                              t2B: BLUE avg 1000 added: 1555
             *                              t1B t2B difference: abs(t1B-t2B): abs(1502-1555): 53
             *
             *                              t1Bt2B difference (53) > t1At2A difference (17),
             *                              so place 7th & 8th ranked players according to the scenario
             *                              with the lower difference (17). 7th ranked(1000) to RED,
             *                              8th ranked(860) to BLUE.
             *
             *      Final ranking:
             *      RED              BLUE
             *      2200             2000
             *      1750             1920
             *      1200             1300
             *      1000             860
             *      Red total elo: 6150
             *      Blue total elo: 6080
             *      Difference: 70
             *      Avg red elo: 1537
             *      Avg blue elo: 1520
             *
             * If more than 8 players, repeat until final players are sorted out.
             *
             */

            var t1A = AddToTeamGetAvg(teamRed, betterRankedPlayerElo, gametype);
            var t2A = AddToTeamGetAvg(teamBlue, worseRankedPlayerElo, gametype);
            var t1aT2ADiff = Math.Abs(t1A - t2A);

            var t1B = AddToTeamGetAvg(teamRed, worseRankedPlayerElo, gametype);
            var t2B = AddToTeamGetAvg(teamBlue, betterRankedPlayerElo, gametype);
            var t1bT2BDiff = Math.Abs(t1B - t2B);

            return (t1bT2BDiff <= t1aT2ADiff) ? 0 : 1;
        }

        /// <summary>
        /// Gets and displays the results (for debug purposes)
        /// </summary>
        /// <param name="teamRed">The red team.</param>
        /// <param name="teamBlue">The blue team.</param>
        /// <param name="gametype">The gametype.</param>
        private void DisplayStats(IList<PlayerInfo> teamRed, IList<PlayerInfo> teamBlue,
            QlGameTypes gametype)
        {
            var redTeamElo = teamRed.Sum(player => player.EloData.GetEloFromGameType(gametype));
            var redTeamAvgElo = (redTeamElo / teamRed.Count);
            var blueTeamElo = teamBlue.Sum(player => player.EloData.GetEloFromGameType(gametype));
            var blueTeamAvgElo = (blueTeamElo / teamBlue.Count);
            var red = new StringBuilder();
            var blue = new StringBuilder();

            foreach (var player in teamRed)
            {
                red.Append(string.Format("{0} [{1}], ", player.ShortName,
                    player.EloData.GetEloFromGameType(gametype)));
            }
            foreach (var player in teamBlue)
            {
                blue.Append(string.Format("{0} [{1}], ", player.ShortName,
                    player.EloData.GetEloFromGameType(gametype)));
            }

            Log.Write("Team balance results:", _logClassType, _logPrefix);

            Log.Write(string.Format("RED: {0} | Total Elo: {1} | Avg Elo Per Red Player: {2}",
                red.ToString().TrimEnd(',', ' '), redTeamElo, redTeamAvgElo), _logClassType, _logPrefix);

            Log.Write(string.Format("BLUE: {0} | Total Elo: {1} | Avg Elo Per Blue Player: {2}",
                blue.ToString().TrimEnd(',', ' '), blueTeamElo, blueTeamAvgElo), _logClassType, _logPrefix);

            Log.Write(string.Format("RED - BLUE Total Elo Difference: abs({0}-{1}): {2}",
                redTeamElo, blueTeamElo, Math.Abs(redTeamElo - blueTeamElo)), _logClassType, _logPrefix);

            Log.Write(string.Format("RED - BLUE Avg Elo Per Player Difference: {0}",
                Math.Abs(redTeamAvgElo) - (blueTeamAvgElo)), _logClassType, _logPrefix);
        }
    }
}
