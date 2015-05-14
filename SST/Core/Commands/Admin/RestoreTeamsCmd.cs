using System;
using System.Linq;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;

namespace SST.Core.Commands.Admin
{
    /// <summary>
    /// Command: Restore the previous game's teams in the warm-up period of a new game.
    /// </summary>
    public class RestoreTeamsCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Admin;
        private int _qlMinArgs = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestoreTeamsCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public RestoreTeamsCmd(SynServerTool sst)
        {
            _sst = sst;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinArgs { get { return _qlMinArgs; } }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        /// <remarks>Not implemented because the cmd in this class requires no args.</remarks>
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (!_sst.ServerInfo.IsATeamGame())
            {
                StatusMessage = "^1[ERROR]^3 Teams can only be restored in team-based game modes.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (_sst.ServerInfo.CurrentServerGameState != QlGameStates.Warmup)
            {
                StatusMessage = "^1[ERROR]^3 Teams can only be restored during warm-up.";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (_sst.ServerInfo.EndOfGameBlueTeam.Count == 0 || _sst.ServerInfo.EndOfGameRedTeam.Count == 0)
            {
                StatusMessage = "^1[ERROR]^3 Unable to restore teams as this time.";
                await SendServerTell(c, StatusMessage);
                return false;
            }

            StatusMessage = ("^2[SUCCESS]^7 Attempting to restore the teams from last game.");
            await SendServerSay(c, StatusMessage);
            await RestoreTeams();
            return true;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        /// <remarks>Not implemented because the cmd in this class requires no args.</remarks>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Empty;
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        /// Restores the teams from the last game.
        /// </summary>
        private async Task RestoreTeams()
        {
            // Lock
            await _sst.QlCommands.SendToQlAsync("lock", false);
            // Move all to spec
            foreach (var player in _sst.ServerInfo.CurrentPlayers.ToList().Where(player =>
                !player.Value.ShortName.Equals(_sst.AccountName,
                    StringComparison.InvariantCultureIgnoreCase)))
            {
                await _sst.QlCommands.CustCmdPutPlayer(player.Value.ShortName, Team.Spec);
            }
            // Restore
            foreach (var player in _sst.ServerInfo.EndOfGameRedTeam)
            {
                await _sst.QlCommands.CustCmdPutPlayer(player, Team.Red);
            }
            foreach (var player in _sst.ServerInfo.EndOfGameBlueTeam)
            {
                await _sst.QlCommands.CustCmdPutPlayer(player, Team.Blue);
            }
            // Unlock
            await _sst.QlCommands.SendToQlDelayedAsync("unlock", false, 5);
        }
    }
}
