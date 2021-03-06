﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Core.Commands.Admin;
using SST.Core.Modules.Irc;
using SST.Enums;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.None
{
    /// <summary>
    /// Command: list the eligible players (or sub candidates)
    /// </summary>
    public class PickupWhoCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:PICKUPWHO]";
        private readonly int _qlMinArgs = 0;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="PickupWhoCmd"/> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public PickupWhoCmd(SynServerTool sst)
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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(Cmd c)
        {
            StatusMessage = GetArgLengthErrorMessage(c);
            await SendServerTell(c, StatusMessage);
        }

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        public async Task<bool> ExecAsync(Cmd c)
        {
            if (!_sst.Mod.Pickup.Active)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 Pickup module is not active. An admin must first load it with:^7 {0}{1} {2}",
                    CommandList.GameCommandPrefix,
                    ((c.FromIrc)
                        ? (string.Format("{0} {1}",
                            IrcCommandList.IrcCmdQl, CommandList.CmdModule))
                        : CommandList.CmdModule),
                    ModuleCmd.PickupArg);
                await SendServerTell(c, StatusMessage);

                Log.Write(
                    string.Format(
                        "{0} attempted {1} command from {2}, but {3} module is not loaded. Ignoring.",
                        c.FromUser, c.CmdName, ((c.FromIrc) ? "from IRC" : "from in-game"),
                        ModuleCmd.PickupArg), _logClassType, _logPrefix);

                return false;
            }

            string epStr;
            if (_sst.Mod.Pickup.Manager.IsPickupPreGame)
            {
                if (_sst.Mod.Pickup.Manager.AvailablePlayers.Count > 0)
                {
                    epStr = string.Format("^2Available players (^7{0}^2): {1}",
                        _sst.Mod.Pickup.Manager.AvailablePlayers.Count,
                        string.Join(",", _sst.Mod.Pickup.Manager.AvailablePlayers));
                }
                else
                {
                    epStr = string.Format("^1NO available players.^3 {0}{1}^1 to sign up!",
                        CommandList.GameCommandPrefix, CommandList.CmdPickupAdd);
                }

                StatusMessage = string.Format("^5[PICKUP] {0}", epStr);
                await SendServerSay(c, StatusMessage);
                return true;
            }
            if (_sst.Mod.Pickup.Manager.IsPickupInProgress)
            {
                if (_sst.Mod.Pickup.Manager.SubCandidates.Count > 0)
                {
                    epStr = string.Format("^6Available substitutes (^7{0}^6): {1}",
                        _sst.Mod.Pickup.Manager.SubCandidates.Count,
                        string.Join(",", _sst.Mod.Pickup.Manager.SubCandidates));
                }
                else
                {
                    epStr = string.Format("^1NO available substitutes.^3 {0}{1}^1 to sign up!",
                        CommandList.GameCommandPrefix, CommandList.CmdPickupAdd);
                }
                StatusMessage = string.Format("^5[PICKUP] {0} ^7- Game size: ^5{1}v{1}",
                    epStr, _sst.Mod.Pickup.Teamsize);
                await SendServerSay(c, StatusMessage);
                return true;
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
            return string.Empty;
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
    }
}
