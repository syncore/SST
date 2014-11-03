using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Core.Commands.Admin;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Auto voter. Automatically pass or reject votes based on specified criteria.
    /// </summary>
    public class AutoVoter : IModule
    {
        public const string NameModule = "autovote";
        private readonly List<AutoVote> _autoVotes;
        private readonly SynServerBot _ssb;
        private readonly List<string> _validCallVotes;
        private int _minModuleArgs = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AutoVoter" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AutoVoter(SynServerBot ssb)
        {
            _ssb = ssb;
            _autoVotes = new List<AutoVote>();
            _validCallVotes = new List<string>
            {
                "clientkick",
                //"cointoss",
                "fraglimit",
                "g_gametype",
                "kick",
                "map",
                "map_restart",
                "nextmap",
                "ruleset",
                "shuffle",
                "teamsize",
                "timelimit"
            };
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the auto voter module is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the auto voter module is active; otherwise, <c>false</c>.
        /// </value>
        public static bool IsModuleActive { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Used to query activity status for a list of modules. Be sure to set
        /// a public static bool property IsModuleActive for outside access in other parts of app.
        /// </remarks>
        public bool Active { get { return IsModuleActive; } }

        /// <summary>
        ///     Gets the automatic votes.
        /// </summary>
        /// <value>
        ///     The automatic votes.
        /// </value>
        public List<AutoVote> AutoVotes
        {
            get { return _autoVotes; }
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinModuleArgs
        {
            get { return _minModuleArgs; }
        }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        /// <value>
        /// The name of the module.
        /// </value>
        public string ModuleName { get { return NameModule; } }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <no vote|yes vote|del #|list|clear>",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AutoVoteArg));
        }

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args.Length == 3)
            {
                if (c.Args[2].Equals("off"))
                {
                    await DisableAutoVoter();
                }
                else if (c.Args[2].Equals("clear"))
                {
                    _autoVotes.Clear();
                    await _ssb.QlCommands.QlCmdSay("^2[SUCCESS]^7 Cleared list of votes to automatically pass or reject.");
                    IsModuleActive = false;
                }
                else if (c.Args[2].Equals("list"))
                {
                    await ListAutoVotes(c);
                }
                else
                {
                    await DisplayArgLengthError(c);
                    return;
                }
            }
            if (c.Args.Length >= 4)
            {
                if (c.Args[2].Equals("del"))
                {
                    await HandleAutoVoteDeletion(c);
                }
                else if (c.Args[2].Equals("yes") || c.Args[2].Equals("no"))
                {
                    await HandleAutoVoteAddition(c);
                }
                else
                {
                    await DisplayArgLengthError(c);
                }
            }
        }

        /// <summary>
        /// Adds the automatic vote with arguments
        /// </summary>
        /// <param name="c">The c.</param>
        /// <remarks>
        /// This method is for auto-votes where a second parameter is specified in addition to
        /// the generic type of vote. Example: 'map campgrounds'
        /// </remarks>
        private async Task AddAutoVoteWithArgs(CmdArgs c)
        {
            string fullVote = string.Format("{0} {1}", c.Args[3], c.Args[4]);
            foreach (var av in _autoVotes.Where(av => av.VoteText.Equals(fullVote,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format("^1[ERROR]^3 AUTO {0} vote for^7 {1}^3 was already added by {2}",
                            (av.IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"), fullVote, av.AddedBy));
                return;
            }
            IsModuleActive = true;
            _autoVotes.Add(new AutoVote(fullVote, true, (c.Args[2].Equals("yes") ? IntendedVoteResult.Yes : IntendedVoteResult.No), c.FromUser));
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Any vote matching: ^3{0}^7 will automatically {1}.",
                    fullVote, (c.Args[2].Equals("yes") ? "^2pass" : "^1fail")));
        }

        /// <summary>
        /// Adds the no argument automatic vote.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <remarks>
        /// This method is for auto-votes where a second parameter is not specified
        ///  in addition to the generic type of vote. Example: 'map'
        /// </remarks>
        private async Task AddNoArgAutoVote(CmdArgs c)
        {
            foreach (var av in _autoVotes.Where(av => av.VoteText.Equals(c.Args[3], StringComparison.InvariantCultureIgnoreCase)))
            {
                await
                        _ssb.QlCommands.QlCmdSay(
                            string.Format("^1[ERROR]^3 AUTO {0} vote for^7 {1}^3 was already added by {2}",
                            (av.IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"), c.Args[3], av.AddedBy));
                return;
            }
            IsModuleActive = true;
            _autoVotes.Add(new AutoVote(c.Args[3], false, (c.Args[2].Equals("yes") ? IntendedVoteResult.Yes : IntendedVoteResult.No), c.FromUser));
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Any vote matching: ^3{0}^7 will automatically {1}.",
                    c.Args[3], (c.Args[2].Equals("yes") ? "^2pass" : "^1fail")));
        }

        /// <summary>
        ///     Disables the automatic voter.
        /// </summary>
        private async Task DisableAutoVoter()
        {
            IsModuleActive = false;
            await _ssb.QlCommands.QlCmdSay(
                "^2[SUCCESS]^7 Auto-voter is ^1OFF^7. Votes will not be passed/rejected automatically.");
        }

        /// <summary>
        ///     Displays an error indicating that the auto-vote id is not a valid numeric value.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task DisplayNotNumError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Auto-vote to remove must be a number. To see #s: {0}{1} {2} list",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AutoVoteArg));
        }

        /// <summary>
        ///     Displays an error indicating that the vote doesnt exist error.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task DisplayVoteDoesntExistError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 No auto-vote exists with that #. To see #s: {0}{1} {2} list",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AutoVoteArg));
        }

        /// <summary>
        ///     Handles the automatic vote addition.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task HandleAutoVoteAddition(CmdArgs c)
        {
            if (!_validCallVotes.Contains(c.Args[3]))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ERROR]^3 {0} isn't valid or applicable. In console: /callvote to see valid types.",
                            c.Args[3]));
                return;
            }
            if (c.Args[2].Equals("yes") || c.Args[2].Equals("no"))
            {
                if (c.Args.Length == 4)
                {
                    // Generic vote type w/o additional parameter
                    await AddNoArgAutoVote(c);
                }
                else if (c.Args.Length == 5)
                {
                    // Generic vote type with additional parameter
                    await AddAutoVoteWithArgs(c);
                }
            }
        }

        /// <summary>
        ///     Handles the automatic vote deletion.
        /// </summary>
        /// <param name="c">The c.</param>
        private async Task HandleAutoVoteDeletion(CmdArgs c)
        {
            int voteNum;
            if (!int.TryParse(c.Args[3], out voteNum))
            {
                await DisplayNotNumError(c);
                return;
            }
            if (_autoVotes.ElementAtOrDefault(voteNum) == null)
            {
                await DisplayVoteDoesntExistError(c);
                return;
            }
            await RemoveAutoVote(voteNum);
            // Disable if there is no rules specified
            if (_autoVotes.Count == 0)
            {
                IsModuleActive = false;
            }
        }

        /// <summary>
        ///     Lists the automatic votes.
        /// </summary>
        private async Task ListAutoVotes(CmdArgs c)
        {
            if (_autoVotes.Count == 0)
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^7No automatic pass/reject votes are set. Use ^2{0}{1} {2} yes vote ^7OR^1 no vote^7 to add.",
                    CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.AutoVoteArg));
                return;
            }
            var yes = new StringBuilder();
            var no = new StringBuilder();
            if (_autoVotes.Any(av => av.IntendedResult == IntendedVoteResult.Yes))
            {
                foreach (AutoVote a in _autoVotes.Where(a => a.IntendedResult == IntendedVoteResult.Yes))
                {
                    yes.Append(string.Format("^7#{0}:^2 {1}^7 ({2}), ", _autoVotes.IndexOf(a), a.VoteText, a.AddedBy));
                }
                await _ssb.QlCommands.QlCmdSay("^2[AUTO YES]^7 " + yes.ToString().TrimEnd(',', ' '));
            }
            if (_autoVotes.Any(av => av.IntendedResult == IntendedVoteResult.No))
            {
                foreach (AutoVote a in _autoVotes.Where(a => a.IntendedResult == IntendedVoteResult.No))
                {
                    no.Append(string.Format("^7#{0}:^1 {1}^7 ({2}), ", _autoVotes.IndexOf(a), a.VoteText, a.AddedBy));
                }
                await _ssb.QlCommands.QlCmdSay("^1[AUTO NO]^7 " + no.ToString().TrimEnd(',', ' '));
            }
        }

        /// <summary>
        ///     Removes the automatic vote.
        /// </summary>
        /// <param name="voteNum">The vote number.</param>
        private async Task RemoveAutoVote(int voteNum)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format("^2[SUCCESS]^7 AUTO {0} vote (^3{1}^7) was removed.",
                (_autoVotes[voteNum].IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"),
                _autoVotes[voteNum].VoteText
                ));
            _autoVotes.RemoveAt(voteNum);
        }
    }
}