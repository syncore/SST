using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    ///     Module: Auto voter. Automatically pass or reject votes based on specified criteria.
    /// </summary>
    public class AutoVoter : IModule
    {
        public const string NameModule = "autovote";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AutoVoter" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AutoVoter(SynServerBot ssb)
        {
            _ssb = ssb;
            _configHandler = new ConfigHandler();
            ValidCallVotes = new List<Vote>
            {
                new Vote { Name = "clientkick", Type = VoteType.Clientkick },
                new Vote { Name = "fraglimit", Type = VoteType.Fraglimit },
                new Vote { Name = "g_gametype", Type = VoteType.GGametype },
                new Vote { Name = "kick", Type = VoteType.Kick },
                new Vote { Name = "map", Type = VoteType.Map },
                new Vote { Name = "map_restart", Type = VoteType.MapRestart },
                new Vote { Name = "nextmap", Type = VoteType.Nextmap },
                new Vote { Name = "ruleset", Type = VoteType.Ruleset },
                new Vote { Name = "shuffle", Type = VoteType.Shuffle },
                new Vote { Name = "teamsize", Type = VoteType.Teamsize },
                new Vote { Name = "timelimit", Type = VoteType.Timelimit}
            };

            LoadConfig();
        }

        /// <summary>
        ///     Gets the automatic votes.
        /// </summary>
        /// <value>
        ///     The automatic votes.
        /// </value>
        public List<AutoVote> AutoVotes { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     Gets the minimum module arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum module arguments for the IRC command.
        /// </value>
        public int IrcMinModuleArgs
        {
            get { return _qlMinModuleArgs + 1; }
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
        }

        /// <summary>
        ///     Gets the name of the module.
        /// </summary>
        /// <value>
        ///     The name of the module.
        /// </value>
        public string ModuleName
        {
            get { return NameModule; }
        }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinModuleArgs
        {
            get { return _qlMinModuleArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the valid callvotes.
        /// </summary>
        /// <value>
        /// The valid callvotes.
        /// </value>
        public List<Vote> ValidCallVotes { get; set; } 

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c>if the command evaluation was successful,
        ///     otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < (c.FromIrc ? IrcMinModuleArgs : _qlMinModuleArgs))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            if (c.Args.Length == ((c.FromIrc) ? 4 : 3))
            {
                if (Helpers.GetArgVal(c, 2).Equals("off"))
                {
                    await DisableAutoVoter(c);
                    return true;
                }
                if (Helpers.GetArgVal(c, 2).Equals("clear"))
                {
                    AutoVotes.Clear();
                    StatusMessage = "^2[SUCCESS]^7 Cleared list of votes to automatically pass or reject.";
                    await SendServerSay(c, StatusMessage);
                    UpdateConfig(false);
                    return true;
                }
                if (Helpers.GetArgVal(c, 2).Equals("list"))
                {
                    await ListAutoVotes(c);
                    return true;
                }
                await DisplayArgLengthError(c);
                return false;
            }
            if (c.Args.Length >= ((c.FromIrc) ? 5 : 4))
            {
                if (Helpers.GetArgVal(c, 2).Equals("del"))
                {
                    return await HandleAutoVoteDeletion(c);
                }
                if (Helpers.GetArgVal(c, 2).Equals("yes") || Helpers.GetArgVal(c, 2).Equals("no"))
                {
                    return await HandleAutoVoteAddition(c);
                }

                await DisplayArgLengthError(c);
                return false;
            }
            return false;
        }

        /// <summary>
        ///     Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     The argument length error message, correctly color-formatted
        ///     depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(CmdArgs c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <no vote|yes vote|del #|list|clear>",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();
            Active = _configHandler.Config.AutoVoterOptions.autoVotes.Count != 0 &&
                     _configHandler.Config.AutoVoterOptions.isActive;
            AutoVotes = _configHandler.Config.AutoVoterOptions.autoVotes;
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _ssb.QlCommands.QlCmdTell(message, c.FromUser);
        }

        /// <summary>
        ///     Updates the configuration.
        /// </summary>
        /// <param name="active">
        ///     if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        ///     updating the configuration.
        /// </param>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            _configHandler.Config.AutoVoterOptions.isActive = active;
            _configHandler.Config.AutoVoterOptions.autoVotes = AutoVotes;

            _configHandler.WriteConfiguration();

            // Reflect changes in UI
            _ssb.UserInterface.PopulateModAutoVoterUi();
        }

        /// <summary>
        ///     Adds the automatic vote with arguments
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the argumented automatic vote was successfully added;
        ///     otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     This method is for auto-votes where a second parameter is specified in addition to
        ///     the generic type of vote. Example: 'map campgrounds'
        /// </remarks>
        private async Task<bool> AddAutoVoteWithArgs(CmdArgs c)
        {
            var fullVote = string.Format("{0} {1}", Helpers.GetArgVal(c, 3), Helpers.GetArgVal(c, 4));
            foreach (var av in AutoVotes.Where(av => av.VoteText.Equals(fullVote,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 AUTO {0} vote for ^1{1}^3 was already added by ^1{2}",
                        (av.IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"), fullVote, av.AddedBy);
                await SendServerTell(c, StatusMessage);
                return false;
            }

            AutoVotes.Add(new AutoVote(fullVote, true, (Helpers.GetArgVal(c, 2).Equals("yes")
                ? IntendedVoteResult.Yes
                : IntendedVoteResult.No), c.FromUser));
            UpdateConfig(true);
            StatusMessage = string.Format("^2[SUCCESS]^7 Any vote matching: ^2{0}^7 will automatically {1}",
                fullVote, (Helpers.GetArgVal(c, 2).Equals("yes") ? "^2pass." : "^1fail."));
            return true;
        }

        /// <summary>
        ///     Adds the no argument automatic vote.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the no-argument automatic vote was successfully added;
        ///     otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     This method is for auto-votes where a second parameter is not specified
        ///     in addition to the generic type of vote. Example: 'map'
        /// </remarks>
        private async Task<bool> AddNoArgAutoVote(CmdArgs c)
        {
            foreach (var av in AutoVotes.Where(av => av.VoteText.Equals(Helpers.GetArgVal(c, 3),
                StringComparison.InvariantCultureIgnoreCase)))
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 AUTO {0} vote for ^1{1}^3 was already added by ^1{2}",
                        (av.IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"),
                        Helpers.GetArgVal(c, 3), av.AddedBy);
                await SendServerTell(c, StatusMessage);
                return false;
            }

            AutoVotes.Add(new AutoVote(Helpers.GetArgVal(c, 3), false,
                (Helpers.GetArgVal(c, 2).Equals("yes")
                    ? IntendedVoteResult.Yes
                    : IntendedVoteResult.No), c.FromUser));
            UpdateConfig(true);
            StatusMessage = string.Format("^2[SUCCESS]^7 Any vote matching: ^2{0}^7 will automatically {1}",
                Helpers.GetArgVal(c, 3), (Helpers.GetArgVal(c, 2).Equals("yes") ? "^2pass." : "^1fail."));
            await SendServerSay(c, StatusMessage);
            return true;
        }

        /// <summary>
        ///     Disables the automatic voter.
        /// </summary>
        /// <param name="c">
        ///     The command argument information.
        /// </param>
        private async Task DisableAutoVoter(CmdArgs c)
        {
            UpdateConfig(false);
            StatusMessage =
                "^2[SUCCESS]^7 Auto-voter is ^1OFF^7. Votes will not be passed/rejected automatically.";
            await SendServerSay(c, StatusMessage);
        }

        /// <summary>
        ///     Displays an error indicating that the auto-vote id is not a valid numeric value.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisplayNotNumError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Auto-vote to remove must be a number. To see #s: ^1{0}{1} {2} list",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule)));
        }

        /// <summary>
        ///     Displays an error indicating that the vote doesnt exist error.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisplayVoteDoesntExistError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 No auto-vote exists with that #. To see #s: ^1{0}{1} {2} list",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule)));
        }

        /// <summary>
        ///     Handles the automatic vote addition.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the vote addition was attempted, otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> HandleAutoVoteAddition(CmdArgs c)
        {
            var isValidCallVote = (ValidCallVotes.Any(v => v.Name.Equals(Helpers.GetArgVal(c, 3),
                StringComparison.InvariantCultureIgnoreCase)));

            if (!isValidCallVote)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 {0} isn't valid or applicable. In console: /callvote to see valid types.",
                    Helpers.GetArgVal(c, 3));
                await SendServerTell(c, StatusMessage);
                return false;
            }
            if (Helpers.GetArgVal(c, 2).Equals("yes") || Helpers.GetArgVal(c, 2).Equals("no"))
            {
                if (c.Args.Length == ((c.FromIrc) ? 5 : 4))
                {
                    // Generic vote type w/o additional parameter
                    return await AddNoArgAutoVote(c);
                }
                if (c.Args.Length == ((c.FromIrc) ? 6 : 5))
                {
                    // Generic vote type with additional parameter
                    return await AddAutoVoteWithArgs(c);
                }
            }
            return false;
        }

        /// <summary>
        ///     Handles the automatic vote deletion.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the vote deletion was successful, otherwise
        ///     <c>false</c>.
        /// </returns>
        private async Task<bool> HandleAutoVoteDeletion(CmdArgs c)
        {
            int voteNum;
            if (!int.TryParse(Helpers.GetArgVal(c, 3), out voteNum))
            {
                await DisplayNotNumError(c);
                return false;
            }
            if (AutoVotes.ElementAtOrDefault(voteNum) == null)
            {
                await DisplayVoteDoesntExistError(c);
                return false;
            }
            await RemoveAutoVote(c, voteNum);
            // Disable if there are no rules specified
            var disable = AutoVotes.Count == 0;

            UpdateConfig(disable);
            return true;
        }

        /// <summary>
        ///     Lists the automatic votes.
        /// </summary>
        private async Task ListAutoVotes(CmdArgs c)
        {
            if (AutoVotes.Count == 0)
            {
                StatusMessage = string.Format("^7No automatic pass/reject votes are set. Use ^2{0}{1} {2}" +
                                              " yes vote ^7OR^1 no vote^7 to add.",
                    CommandList.GameCommandPrefix, c.CmdName,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}", c.Args[1],
                            NameModule))
                        : NameModule));
                await SendServerSay(c, StatusMessage);
                return;
            }
            var yes = new StringBuilder();
            var no = new StringBuilder();
            if (AutoVotes.Any(av => av.IntendedResult == IntendedVoteResult.Yes))
            {
                foreach (var a in AutoVotes.Where(a => a.IntendedResult == IntendedVoteResult.Yes))
                {
                    yes.Append(string.Format("^7#{0}:^2 {1}^7 ({2}), ", AutoVotes.IndexOf(a), a.VoteText,
                        a.AddedBy));
                }
                StatusMessage = string.Format("^2[AUTO YES]^7 " + yes.ToString().TrimEnd(',', ' '));
                await SendServerSay(c, StatusMessage);
            }
            if (AutoVotes.Any(av => av.IntendedResult == IntendedVoteResult.No))
            {
                foreach (var a in AutoVotes.Where(a => a.IntendedResult == IntendedVoteResult.No))
                {
                    no.Append(string.Format("^7#{0}:^1 {1}^7 ({2}), ", AutoVotes.IndexOf(a), a.VoteText,
                        a.AddedBy));
                }
                StatusMessage = string.Format("^1[AUTO NO]^7 " + no.ToString().TrimEnd(',', ' '));
                await SendServerSay(c, StatusMessage);
            }
        }

        /// <summary>
        ///     Removes the automatic vote.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="voteNum">The vote number.</param>
        /// <returns></returns>
        private async Task RemoveAutoVote(CmdArgs c, int voteNum)
        {
            StatusMessage = string.Format("^2[SUCCESS]^7 AUTO {0} vote (^3{1}^7) was removed.",
                (AutoVotes[voteNum].IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"),
                AutoVotes[voteNum].VoteText
                );
            AutoVotes.RemoveAt(voteNum);
            await SendServerSay(c, StatusMessage);
        }
    }
}