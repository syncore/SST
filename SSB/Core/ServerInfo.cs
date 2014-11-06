using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using SSB.Enum;
using SSB.Model;
using Timer = System.Timers.Timer;

namespace SSB.Core
{
    /// <summary>
    ///     Class that contains important information about the server on which the bot is loaded.
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerInfo"/> class.
        /// </summary>
        public ServerInfo()
        {
            CurrentPlayers = new Dictionary<string, PlayerInfo>();
            VoteTimer = new Timer();
        }

        /// <summary>
        ///     Gets the current players.
        /// </summary>
        /// <value>
        ///     The current players.
        /// </value>
        public Dictionary<string, PlayerInfo> CurrentPlayers { get; private set; }

        /// <summary>
        /// Gets or sets the type of game for the current server.
        /// </summary>
        /// <value>
        /// The type of game for the current server.
        /// </value>
        public QlGameTypes CurrentServerGameType { get; set; }

        /// <summary>
        /// Gets or sets the current server identifier.
        /// </summary>
        /// <value>
        /// The current server identifier.
        /// </value>
        public string CurrentServerId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a vote iis n progress.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a vote is in progress; otherwise, <c>false</c>.
        /// </value>
        public bool VoteInProgress { get; set; }

        /// <summary>
        /// Gets or sets the vote timer.
        /// </summary>
        /// <value>
        /// The vote timer.
        /// </value>
        public Timer VoteTimer { get; set; }

        /// <summary>
        /// Gets the team.
        /// </summary>
        /// <param name="t">The Team enum.</param>
        /// <returns>A list of <see cref="PlayerInfo"/>objects for a given Team enum.</returns>
        public List<PlayerInfo> GetTeam(Team t)
        {
            return CurrentPlayers.Where(player => player.Value.Team.Equals(t)).Select(player => player.Value).ToList();
        }

        /// <summary>
        /// Starts the vote timer and sets the <see cref="VoteInProgress"/> boolean to true.
        /// </summary>
        public void StartVoteTimer()
        {
            VoteTimer.Interval = 30000;
            VoteTimer.AutoReset = true;
            VoteTimer.Elapsed += VoteTimerElapsed;
            VoteTimer.Enabled = true;
            VoteInProgress = true;
            Debug.WriteLine("Vote timer started. Vote is in progress.");
        }

        /// <summary>
        /// Stops the vote timer and sets the <see cref="VoteInProgress"/> boolean to false.
        /// </summary>
        public void StopVoteTimer()
        {
            VoteTimer.Enabled = false;
            VoteInProgress = false;
            Debug.WriteLine("Vote timer stopped. Vote is no longer active.");
        }

        /// <summary>
        /// Method that is executed when the votes timer has elapsed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private void VoteTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            VoteInProgress = false;
        }
    }
}