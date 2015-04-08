﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using SSB.Enums;
using SSB.Interfaces;
using SSB.Model;
using SSB.Util;

namespace SSB.Core.Commands.None
{
    /// <summary>
    ///     Command: vote yes to a team balance suggestion.
    /// </summary>
    public class AcceptTeamSuggestCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = false;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:ACCEPT]";
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.None;
        private int _qlMinArgs = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AcceptTeamSuggestCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AcceptTeamSuggestCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        /// The minimum arguments for the IRC command.
        /// </value>
        public int IrcMinArgs { get { return _qlMinArgs + 1; } }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed { get { return _isIrcAccessAllowed; } }

        /// <summary>
        ///     Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the QL command.
        /// </value>
        public int QlMinArgs
        {
            get { return _qlMinArgs; }
        }

        /// <summary>
        ///     Gets the command's status message.
        /// </summary>
        /// <value>
        ///     The command's status message.
        /// </value>
        public string StatusMessage { get; set; }

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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        ///     <c>true</c> if the command was successfully executed, otherwise
        ///     <c>false</c>.
        /// </returns>
        public async Task<bool> ExecAsync(CmdArgs c)
        {
            if (!_ssb.VoteManager.IsTeamSuggestionVotePending)
            {
                StatusMessage = "^1[ERROR]^3 A team balance vote is not in progress.";
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to accept team suggestion, but team balance vote is not in progress. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                return false;
            }
            if (!Helpers.KeyExists(c.FromUser, _ssb.ServerInfo.CurrentPlayers))
            {
                Log.Write(string.Format(
                    "{0} attempted to accept team suggestion, but {0} is not in list of current players. Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                return false;
            }
            if (_ssb.ServerInfo.CurrentPlayers[c.FromUser].Team.Equals(Team.None) ||
                _ssb.ServerInfo.CurrentPlayers[c.FromUser].Team.Equals(Team.Spec))
            {
                StatusMessage = "^1[ERROR]^3 Only active players may vote.";
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to accept team suggestion, but {0} is not an active player (no team or spec). Ignoring.",
                    c.FromUser), _logClassType, _logPrefix);

                return false;
            }
            if (Helpers.KeyExists(c.FromUser, _ssb.VoteManager.TeamSuggestionVoters))
            {
                StatusMessage = string.Format("^1[ERROR]^3 {0} has already voted.", c.FromUser);
                await SendServerTell(c, StatusMessage);

                Log.Write(string.Format(
                    "{0} attempted to accept team suggestion, but has already voted. Ignoring.", c.FromUser),
                    _logClassType, _logPrefix);

                return false;
            }

            _ssb.VoteManager.TeamSuggestionYesVoteCount++;
            _ssb.VoteManager.TeamSuggestionVoters.Add(c.FromUser, TeamBalanceVote.Yes);
            StatusMessage = string.Format("^3[TEAMBALANCE] {0}^7 voted ^2YES", c.FromUser);
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Counted 'yes' team suggestion vote for {0}",
                c.FromUser), _logClassType, _logPrefix);

            return true;
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
            return string.Empty;
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
    }
}