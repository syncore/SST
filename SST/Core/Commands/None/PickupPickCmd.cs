using System;
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
    ///     Command: captain: pick a player for a team.
    /// </summary>
    public class PickupPickCmd : IBotCommand
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CMD:PICKUPPICK]";
        private readonly int _qlMinArgs = 2;
        private readonly SynServerTool _sst;
        private readonly UserLevel _userLevel = UserLevel.None;
        private bool _isIrcAccessAllowed = false;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PickupCapCmd" /> class.
        /// </summary>
        /// <param name="sst">The main class.</param>
        public PickupPickCmd(SynServerTool sst)
        {
            _sst = sst;
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
        public bool IsIrcAccessAllowed
        {
            get
            {
                return _isIrcAccessAllowed;
            }
        }

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
        public async Task<bool> ExecAsync(CmdArgs c)
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

            return await _sst.Mod.Pickup.Manager.Captains.ProcessPlayerPick(c);
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
                "^1[ERROR]^3 Usage: {0}{1} <name> ^7- name is without the clan tag.",
                CommandList.GameCommandPrefix,
                ((c.FromIrc) ? (string.Format("{0} {1}",
                c.CmdName, c.Args[1])) : c.CmdName));
        }

        /// <summary>
        ///     Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerSay(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdSay(message);
        }

        /// <summary>
        ///     Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        public async Task SendServerTell(CmdArgs c, string message)
        {
            if (!c.FromIrc)
                await _sst.QlCommands.QlCmdTell(message, c.FromUser);
        }
    }
}