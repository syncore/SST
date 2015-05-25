using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Admin
{
    /// <summary>
    /// Command: Call an admin-forced vote to change the map.
    /// </summary>
    public class MapCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:MAP]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public MapCmd(SynServerTool sst)
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
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

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
        /// <param name="c">The command argument information.</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        public async Task<bool> ExecAsync(Cmd c)
        {
            if (_sst.Mod.AutoVoter.Active)
            {
                var containsAutoMapRejectVote = false;
                foreach (
                    var av in
                        _sst.Mod.AutoVoter.AutoVotes.Where(
                            av => av.VoteText.StartsWith(
                                "map", StringComparison.InvariantCultureIgnoreCase)
                                  && av.IntendedResult == IntendedVoteResult.No))
                {
                    containsAutoMapRejectVote = !av.VoteText.Equals("map_restart",
                        StringComparison.InvariantCultureIgnoreCase);
                }

                if (containsAutoMapRejectVote)
                {
                    StatusMessage =
                        "^1[ERROR]^3 AutoVoter module is enabled and is set to auto-reject map votes.";

                    await SendServerTell(c, StatusMessage);

                    Log.Write(string.Format(
                        "{0} attemped to change map from {1}, but auto-voter is enabled and set to reject map votes. Ignoring.",
                        c.FromUser, ((c.FromIrc) ? "IRC" : "in-game")), _logClassType, _logPrefix);

                    return false;
                }
            }

            StatusMessage = string.Format("^2[SUCCESS]^7 Attempting to change map to: ^2{0}",
                Helpers.GetArgVal(c, 1));
            await SendServerSay(c, StatusMessage);
            await _sst.QlCommands.SendToQlAsync(string.Format("cv map {0}", Helpers.GetArgVal(c, 1)), false);

            Log.Write(string.Format("Attempting to send map change command to QL, to change to {0}",
                Helpers.GetArgVal(c, 1)), _logClassType, _logPrefix);

            return true;
        }

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} map",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}", c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}
