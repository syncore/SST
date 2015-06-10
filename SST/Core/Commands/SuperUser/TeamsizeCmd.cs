namespace SST.Core.Commands.SuperUser
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using SST.Enums;
    using SST.Interfaces;
    using SST.Model;
    using SST.Util;

    /// <summary>
    /// Command: Call an admin-forced vote to change the size of the teams.
    /// Note: if the bot is in spec then this basically requires that the server be spawned with
    ///       'allow spectator voting' otherwise an admin will have to manually join the bot to a
    ///       team in order to use this command.
    /// </summary>
    public class TeamsizeCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:TEAMSIZE]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.SuperUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsizeCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public TeamsizeCmd(SynServerTool sst)
        {
            _sst = sst;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        public int IrcMinArgs
        {
            get { return _qlMinArgs + 1; }
        }

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
            int n;
            if (!int.TryParse(Helpers.GetArgVal(c, 1), out n) || n <= 0 || n > 8)
            {
                StatusMessage = "^1[ERROR]^3 Teamsize must be a number greater than 0 and less than 9.";
                await SendServerTell(c, StatusMessage);
                return false;
            }

            if (_sst.Mod.AutoVoter.Active)
            {
                if (_sst.Mod.AutoVoter.AutoVotes.Any(v =>
                    v.VoteText.StartsWith("teamsize", StringComparison.InvariantCultureIgnoreCase)))
                {
                    StatusMessage =
                        "^1[ERROR]^3 AutoVoter module is enabled and is set to auto-reject teamsize votes.";

                    await SendServerTell(c, StatusMessage);

                    Log.Write(string.Format(
                        "{0} attemped to change map from {1}, but auto-voter is enabled and set to reject teamsize votes. Ignoring.",
                        c.FromUser, ((c.FromIrc) ? "IRC" : "in-game")), _logClassType, _logPrefix);

                    return false;
                }
            }

            StatusMessage = string.Format("^2[SUCCESS]^7 Attempting to change teamsize to: ^2{0}",
                Helpers.GetArgVal(c, 1));
            await SendServerTell(c, StatusMessage);
            await _sst.QlCommands.SendToQlAsync(string.Format("cv teamsize {0}", Helpers.GetArgVal(c, 1)), false);
            await _sst.QlCommands.SendToQlDelayedAsync("vote yes", false, 1);

            Log.Write(string.Format("Attempting to send teamsize change command to QL, to change to {0}",
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
                "^1[ERROR]^3 Usage: {0}{1} #",
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
            {
                await _sst.QlCommands.QlCmdSay(message, false);
            }
        }

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(Cmd c, string message)
        {
            if (!c.FromIrc)
            {
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
            }
        }
    }
}
