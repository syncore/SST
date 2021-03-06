﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SST.Config;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    /// Module: Auto voter. Automatically pass or reject votes based on specified criteria.
    /// </summary>
    public class AutoVoter : IModule
    {
        public const string NameModule = "autovote";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:AUTOVOTE]";
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoVoter"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public AutoVoter(SynServerTool sst)
        {
            _sst = sst;
            _configHandler = new ConfigHandler();
            ValidCallVotes = new List<Vote>
            {
                new Vote {Name = "clientkick", Type = VoteType.Clientkick},
                new Vote {Name = "fraglimit", Type = VoteType.Fraglimit},
                new Vote {Name = "g_gametype", Type = VoteType.GGametype},
                new Vote {Name = "kick", Type = VoteType.Kick},
                new Vote {Name = "map", Type = VoteType.Map},
                new Vote {Name = "map_restart", Type = VoteType.MapRestart},
                new Vote {Name = "nextmap", Type = VoteType.Nextmap},
                new Vote {Name = "ruleset", Type = VoteType.Ruleset},
                new Vote {Name = "shuffle", Type = VoteType.Shuffle},
                new Vote {Name = "teamsize", Type = VoteType.Teamsize},
                new Vote {Name = "timelimit", Type = VoteType.Timelimit}
            };

            LoadConfig();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IModule"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; set; }

        /// <summary>
        /// Gets the automatic votes.
        /// </summary>
        /// <value>The automatic votes.</value>
        public List<AutoVote> AutoVotes { get; set; }

        /// <summary>
        /// Gets the minimum module arguments for the IRC command.
        /// </summary>
        /// <value>The minimum module arguments for the IRC command.</value>
        public int IrcMinModuleArgs
        {
            get { return _qlMinModuleArgs + 1; }
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
        /// Gets the name of the module.
        /// </summary>
        /// <value>The name of the module.</value>
        public string ModuleName
        {
            get { return NameModule; }
        }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        public int QlMinModuleArgs
        {
            get { return _qlMinModuleArgs; }
        }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the valid callvotes.
        /// </summary>
        /// <value>The valid callvotes.</value>
        public List<Vote> ValidCallVotes { get; set; }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command evaluation was successful, otherwise <c>false</c>.</returns>
        public async Task<bool> EvalModuleCmdAsync(Cmd c)
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
                    StatusMessage =
                        "^2[SUCCESS]^7 Cleared list of votes to automatically pass or reject. Disabling auto voter.";
                    await SendServerSay(c, StatusMessage);
                    UpdateConfig(false);

                    Log.Write(string.Format(
                        "{0} cleared the auto-voter list from {1}. Auto voter module will now be disabled..",
                        c.FromUser, (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);

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
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        public string GetArgLengthErrorMessage(Cmd c)
        {
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <no vote|yes vote|del #|list|clear>",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            var cfg = _configHandler.ReadConfiguration();
            Active = cfg.AutoVoterOptions.autoVotes.Count != 0 &&
                     cfg.AutoVoterOptions.isActive;
            AutoVotes = cfg.AutoVoterOptions.autoVotes;

            var sb = new StringBuilder();
            foreach (var a in AutoVotes)
            {
                sb.Append(string.Format("{0}, ", a.VoteFormatDisplay));
            }

            Log.Write(string.Format("Active: {0}, auto-votes: {1}",
                (Active ? "YES" : "NO"), ((AutoVotes.Count > 0) ? sb.ToString().TrimEnd(',', ' ') : "none")),
                _logClassType, _logPrefix);
        }

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(Cmd c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message, false);
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

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="active">
        /// if set to <c>true</c> then the module is to remain active; otherwise it is to be
        /// disabled when updating the configuration.
        /// </param>
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            var cfg = _configHandler.ReadConfiguration();
            cfg.AutoVoterOptions.isActive = active;
            cfg.AutoVoterOptions.autoVotes = AutoVotes;

            _configHandler.WriteConfiguration(cfg);

            // Reflect changes in UI
            _sst.UserInterface.PopulateModAutoVoterUi();
        }

        /// <summary>
        /// Adds the automatic vote with arguments
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// <c>true</c> if the argumented automatic vote was successfully added; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is for auto-votes where a second parameter is specified in addition to the
        /// generic type of vote. Example: 'map campgrounds'
        /// </remarks>
        private async Task<bool> AddAutoVoteWithArgs(Cmd c)
        {
            var fullVote = string.Format("{0} {1}", Helpers.GetArgVal(c, 3), Helpers.GetArgVal(c, 4));
            foreach (var av in AutoVotes.Where(av => av.VoteText.Equals(fullVote,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 AUTO {0} vote for ^1{1}^3 was already added by ^1{2}",
                        (av.IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"), fullVote, av.AddedBy);
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to add auto-{1} vote: {2} from {3} but auto-{1} vote was already added by {4}. Ignoring.",
                    c.FromUser, Helpers.GetArgVal(c, 2).ToUpper(),
                    fullVote, (c.FromIrc ? "IRC." : "in-game."), av.AddedBy), _logClassType, _logPrefix);

                return false;
            }

            AutoVotes.Add(new AutoVote(fullVote, true, (Helpers.GetArgVal(c, 2).Equals("yes")
                ? IntendedVoteResult.Yes
                : IntendedVoteResult.No), c.FromUser));

            UpdateConfig(true);

            StatusMessage = string.Format("^2[SUCCESS]^7 Any vote matching: ^2{0}^7 will automatically {1}",
                fullVote, (Helpers.GetArgVal(c, 2).Equals("yes") ? "^2pass." : "^1fail."));
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("{0} added auto-{1} vote: {2} from {3}",
                c.FromUser, Helpers.GetArgVal(c, 2).ToUpper(),
                fullVote, (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);
            return true;
        }

        /// <summary>
        /// Adds the no argument automatic vote.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// <c>true</c> if the no-argument automatic vote was successfully added; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is for auto-votes where a second parameter is not specified in addition to
        /// the generic type of vote. Example: 'map'
        /// </remarks>
        private async Task<bool> AddNoArgAutoVote(Cmd c)
        {
            foreach (var av in AutoVotes.Where(av => av.VoteText.Equals(Helpers.GetArgVal(c, 3),
                StringComparison.InvariantCultureIgnoreCase)))
            {
                StatusMessage =
                    string.Format("^1[ERROR]^3 AUTO {0} vote for ^1{1}^3 was already added by ^1{2}",
                        (av.IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"),
                        Helpers.GetArgVal(c, 3), av.AddedBy);
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to add auto-{1} vote: {2} from {3} but auto-{1} vote was already added by {4}. Ignoring.",
                    c.FromUser, Helpers.GetArgVal(c, 2).ToUpper(),
                    Helpers.GetArgVal(c, 3), (c.FromIrc ? "IRC." : "in-game."), av.AddedBy), _logClassType,
                    _logPrefix);

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

            Log.Write(string.Format("{0} added auto-{1} vote: {2} from {3}",
                c.FromUser, Helpers.GetArgVal(c, 2).ToUpper(),
                Helpers.GetArgVal(c, 3), (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);

            return true;
        }

        /// <summary>
        /// Disables the automatic voter.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisableAutoVoter(Cmd c)
        {
            UpdateConfig(false);
            StatusMessage =
                "^2[SUCCESS]^7 Auto-voter is ^1OFF^7. Votes will not be passed/rejected automatically.";
            await SendServerSay(c, StatusMessage);
            Log.Write(string.Format("Received {0} request from {1} to disable auto voter module. Disabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
        }

        /// <summary>
        /// Displays an error indicating that the auto-vote id is not a valid numeric value.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisplayNotNumError(Cmd c)
        {
            await _sst.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Auto-vote to remove must be a number. To see #s: ^1{0}{1} {2} list",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule)), false);
        }

        /// <summary>
        /// Displays an error indicating that the vote doesnt exist error.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        private async Task DisplayVoteDoesntExistError(Cmd c)
        {
            await _sst.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 No auto-vote exists with that #. To see #s: ^1{0}{1} {2} list",
                CommandList.GameCommandPrefix, c.CmdName, ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule)), false);
        }

        /// <summary>
        /// Handles the automatic vote addition.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the vote addition was attempted, otherwise <c>false</c>.</returns>
        private async Task<bool> HandleAutoVoteAddition(Cmd c)
        {
            var isValidCallVote = (ValidCallVotes.Any(v => v.Name.Equals(Helpers.GetArgVal(c, 3),
                StringComparison.InvariantCultureIgnoreCase)));

            if (!isValidCallVote)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 {0} isn't valid or applicable. In console: /callvote to see valid types.",
                    Helpers.GetArgVal(c, 3));
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to add a vote but specified an invalid vote type from {1}. Ignoring.",
                    c.FromUser, (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);

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
        /// Handles the automatic vote deletion.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the vote deletion was successful, otherwise <c>false</c>.</returns>
        private async Task<bool> HandleAutoVoteDeletion(Cmd c)
        {
            int voteNum;
            if (!int.TryParse(Helpers.GetArgVal(c, 3), out voteNum))
            {
                await DisplayNotNumError(c);

                Log.Write(string.Format(
                    "{0} attempted to delete a vote but specified a non-numeric vote value from {1}. Ignoring.",
                    c.FromUser, (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);

                return false;
            }
            if (AutoVotes.ElementAtOrDefault(voteNum) == null)
            {
                await DisplayVoteDoesntExistError(c);

                Log.Write(string.Format(
                    "{0} attempted to delete a vote that does not exist from {1}. Ignoring.",
                    c.FromUser, (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);

                return false;
            }
            await RemoveAutoVote(c, voteNum);
            // Disable if there are no rules specified
            var disable = AutoVotes.Count == 0;

            Log.Write("All remaining auto-votes removed. Disabling auto voter module.",
                _logClassType, _logPrefix);

            UpdateConfig(disable);
            return true;
        }

        /// <summary>
        /// Lists the automatic votes.
        /// </summary>
        private async Task ListAutoVotes(Cmd c)
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
        /// Removes the automatic vote.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="voteNum">The vote number.</param>
        /// <returns></returns>
        private async Task RemoveAutoVote(Cmd c, int voteNum)
        {
            Log.Write(string.Format("{0} removed auto-{1} vote: {2} from {3}",
                c.FromUser, (AutoVotes[voteNum].IntendedResult == IntendedVoteResult.Yes
                    ? "YES"
                    : "NO"), AutoVotes[voteNum].VoteText,
                (c.FromIrc ? "IRC." : "in-game.")), _logClassType, _logPrefix);

            StatusMessage = string.Format("^2[SUCCESS]^7 AUTO {0} vote (^3{1}^7) was removed.",
                (AutoVotes[voteNum].IntendedResult == IntendedVoteResult.Yes ? "YES" : "NO"),
                AutoVotes[voteNum].VoteText);

            AutoVotes.RemoveAt(voteNum);
            await SendServerSay(c, StatusMessage);
        }
    }
}
