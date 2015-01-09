using System;
using System.Linq;
using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Call an admin-forced vote to change the map.
    /// </summary>
    public class MapCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Admin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public MapCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} map",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            if (_ssb.Mod.AutoVoter.Active)
            {
                bool containsAutoMapRejectVote = false;
                foreach (
                    var av in
                        _ssb.Mod.AutoVoter.AutoVotes.Where(
                            av => av.VoteText.StartsWith(
                                "map", StringComparison.InvariantCultureIgnoreCase)
                                && av.IntendedResult == IntendedVoteResult.No))
                {
                    containsAutoMapRejectVote = !av.VoteText.Equals("map_restart", StringComparison.InvariantCultureIgnoreCase);
                }

                if (containsAutoMapRejectVote)
                {
                    await
                    _ssb.QlCommands.QlCmdSay("^1[ERROR]^3 AutoVoter module is enabled and is set to auto-reject map votes.");
                    return;
                }
            }

            await
                    _ssb.QlCommands.QlCmdSay(string.Format("^2[SUCCESS]^7 Attempting to change map to: ^2{0}", c.Args[1]));
            await _ssb.QlCommands.SendToQlAsync(string.Format("cv map {0}", c.Args[1]), false);
        }
    }
}