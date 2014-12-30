using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using SSB.Enum;
using SSB.Model;

namespace SSB.Core
{
    /// <summary>
    ///     Class responsible for handling the scanning of accuracies set by the accuracy module.
    /// </summary>
    public class AccuracyHandler
    {
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccuracyHandler" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AccuracyHandler(SynServerBot ssb)
        {
            _ssb = ssb;
            AccTimer = new Timer();
        }

        /// <summary>
        ///     Gets the accuracy scanner timer.
        /// </summary>
        /// <value>
        ///     The accuracy scanner timer.
        /// </value>
        public Timer AccTimer { get; private set; }

        /// <summary>
        ///     Gets or sets the interval between scans in seconds.
        /// </summary>
        /// <value>
        ///     The interval between scans in seconds.
        /// </value>
        public uint IntervalBetweenScans { get; set; }

        /// <summary>
        ///     Starts the accuracy scanner timer.
        /// </summary>
        public void StartAccTimer()
        {
            AccTimer.Interval = (IntervalBetweenScans*1000);
            AccTimer.AutoReset = true;
            AccTimer.Elapsed += AccTimerElapsed;
            AccTimer.Enabled = true;
            Debug.WriteLine(
                "Accuracy scanner timer started. Accuracy scanning is enabled, will scan every {0} seconds.",
                IntervalBetweenScans);
        }

        /// <summary>
        ///     Stops the accuracy scanner timer.
        /// </summary>
        public void StopAccTimer()
        {
            AccTimer.Enabled = false;
            Debug.WriteLine("Accuracy scanner timer stopped. Accuracy scanning is disabled.");
        }

        /// <summary>
        ///     Method that is executed when the accuracy scanner timer elapses.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void AccTimerElapsed(object sender, ElapsedEventArgs e)
        {
            DoAccuracyScan();
        }

        /// <summary>
        ///     Performs the accuracy scan of all active players.
        /// </summary>
        private void DoAccuracyScan()
        {
            // We've joined the game. Disable scanning.
            bool botIsPlayer = (_ssb.ServerInfo.CurrentPlayers[_ssb.BotName].Team == Team.Red ||
                                _ssb.ServerInfo.CurrentPlayers[_ssb.BotName].Team == Team.Blue);

            if (botIsPlayer)
            {
                // No more scans
                StopAccTimer();
                // Silently disable, but don't update the config on disk so as to save the owner
                // the trouble of not having to re-enable the accuracy scanner the next time bot is launched
                _ssb.Mod.Accuracy.Active = false;
                Debug.WriteLine(
                    "Owner has left spectator mode since last scan. Silently disabling accuracy scanning.");
                return;
            }
            List<PlayerInfo> activePlayers = _ssb.ServerInfo.GetTeam(Team.Red);
            List<PlayerInfo> blueTeam = _ssb.ServerInfo.GetTeam(Team.Blue);
            activePlayers.AddRange(blueTeam);

            // TODO: FIXME - it currently goes through all players in like .5 sec. Needs to wait after following each
            StartAccuracyRead();
            foreach (PlayerInfo player in activePlayers)
            {
                _ssb.QlCommands.SendCvarReq(string.Format("follow {0}", player.ShortName), true);
                Debug.WriteLine("Attempted to request follow of player: " + player.ShortName);
            }
            EndAccuracyRead();
        }

        /// <summary>
        ///     Ends the accuracy read by sending the -acc button command to Quake Live, rejoining the spectators,
        ///     and by clearing the player that is currently being tracked by the bot internally.
        /// </summary>
        private void EndAccuracyRead()
        {
            // Send negative state of acc button.
            // Must "re-join" spectators even though we're already there, so that the 1st player whose
            // accuracy is being scanned on the next go-around is correctly detected (QL issue)
            _ssb.QlCommands.SendCvarReq("-acc;team s", false);
            // Reset internal tracking
            _ssb.ServerInfo.PlayerCurrentlyFollowing = string.Empty;
            Debug.WriteLine("Ended accuracy read.");
        }

        /// <summary>
        ///     Starts the accuracy read by sending the +acc button command to Quake Live.
        /// </summary>
        private void StartAccuracyRead()
        {
            _ssb.QlCommands.SendCvarReq("+acc", false);
            Debug.WriteLine("Starting accuracy read.");
        }
    }
}