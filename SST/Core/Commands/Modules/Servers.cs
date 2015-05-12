using System;
using System.Reflection;
using System.Threading.Tasks;
using SST.Config;
using SST.Interfaces;
using SST.Model;
using SST.Util;

namespace SST.Core.Commands.Modules
{
    /// <summary>
    ///     Module: enable or disable the ability to list the active QL servers for a given gametype and region.
    /// </summary>
    public class Servers : IModule
    {
        public const string NameModule = "servers";
        private readonly ConfigHandler _configHandler;
        private readonly bool _isIrcAccessAllowed = true;
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[MOD:SERVERLIST]";
        private readonly int _qlMinModuleArgs = 3;
        private readonly SynServerTool _sst;

        public Servers(SynServerTool sst)
        {
            _sst = sst;
            _configHandler = new ConfigHandler();
            LoadConfig();
        }

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
        ///     Gets or sets the date and time that the query command was last used.
        /// </summary>
        /// <value>
        ///     The date and time that the query command was last used.
        /// </value>
        public DateTime LastQueryTime { get; set; }

        /// <summary>
        ///     Gets or sets the maximum servers to display.
        /// </summary>
        /// <value>
        ///     The maximum servers to display.
        /// </value>
        public int MaxServersToDisplay { get; set; }

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
        ///     Gets or sets the time between queries.
        /// </summary>
        /// <value>
        ///     The time between queries.
        /// </value>
        public double TimeBetweenQueries { get; set; }

        /// <summary>
        ///     Disables the active servers module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        public async Task DisableServers(CmdArgs c)
        {
            UpdateConfig(false);
            StatusMessage = "^2[SUCCESS]^7 Active server list display is ^1disabled^7.";
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Received {0} request from {1} to disable server list . Disabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
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
        ///     Enables the active servers module.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="maxServers">The maximum servers to display.</param>
        /// <param name="timeBetween">
        ///     The time in seconds that must elapse between users issuing the
        ///     query command.
        /// </param>
        public async Task EnableServers(CmdArgs c, int maxServers, double timeBetween)
        {
            MaxServersToDisplay = maxServers;
            TimeBetweenQueries = timeBetween;
            UpdateConfig(true);
            StatusMessage = string.Format(
                "^3[ACTIVESERVERS]^7 Active server listing is now ^2ON^7. Players can" +
                " see up to^5 {0}^7 active servers every^5 {1}^7 seconds.",
                maxServers, timeBetween);
            await SendServerSay(c, StatusMessage);

            Log.Write(string.Format("Received {0} request from {1} to enable server list module. Enabling.",
                (c.FromIrc ? "IRC" : "in-game"), c.FromUser), _logClassType, _logPrefix);
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
            if (Helpers.GetArgVal(c, 2).Equals("off"))
            {
                await DisableServers(c);
                return true;
            }
            if (c.Args.Length != (c.FromIrc ? 5 : 4))
            {
                await DisplayArgLengthError(c);
                return false;
            }
            int maxNum;
            if (!int.TryParse(Helpers.GetArgVal(c, 2), out maxNum) || maxNum <= 0)
            {
                StatusMessage =
                    "^1[ERROR]^3 The maximum servers to display must be a number greater than zero!";
                await SendServerTell(c, StatusMessage);
                return false;
            }
            double timebtNum;
            if (!double.TryParse(Helpers.GetArgVal(c, 3), out timebtNum) || timebtNum < 0)
            {
                StatusMessage = string.Format(
                    "^1[ERROR]^3 The time limit to impose between the {0} cmd must be a number >=0.",
                    CommandList.CmdServers);
                await SendServerTell(c, StatusMessage);
                return false;
            }

            await EnableServers(c, maxNum, timebtNum);
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
            return string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <maxservers> <timebetween> -^7 max servers to" +
                " show, limit in secs between queries",
                CommandList.GameCommandPrefix, c.CmdName,
                ((c.FromIrc)
                    ? (string.Format("{0} {1}", c.Args[1],
                        NameModule))
                    : NameModule));
        }

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            var cfg = _configHandler.ReadConfiguration();

            // Valid values?
            if (cfg.ServersOptions.maxServers == 0 ||
                cfg.ServersOptions.timeBetweenQueries < 0)
            {
                Log.Write(
                    "Invalid max servers or time between queries value detected during initial load" +
                    "of server list module configuration. Will not active. Will set defaults.",
                    _logClassType, _logPrefix);

                Active = false;
                cfg.ServersOptions.SetDefaults();
                _configHandler.WriteConfiguration(cfg);
                return;
            }

            Active = cfg.ServersOptions.isActive;
            MaxServersToDisplay = cfg.ServersOptions.maxServers;
            TimeBetweenQueries = cfg.ServersOptions.timeBetweenQueries;

            Log.Write(string.Format(
                "Active: {0}, max servers to display: {1}, time between queries: {2} seconds",
                (Active ? "YES" : "NO"), MaxServersToDisplay, TimeBetweenQueries), _logClassType,
                _logPrefix);
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

        /// <summary>
        ///     Updates the configuration.
        /// </summary>
        /// if set to
        /// <c>true</c>
        /// then the module is to remain active;
        /// otherwise it is to be disabled when updating the configuration.
        public void UpdateConfig(bool active)
        {
            // Go into effect now
            Active = active;

            var cfg = _configHandler.ReadConfiguration();
            cfg.ServersOptions.isActive = active;
            cfg.ServersOptions.maxServers = MaxServersToDisplay;
            cfg.ServersOptions.timeBetweenQueries = TimeBetweenQueries;

            _configHandler.WriteConfiguration(cfg);

            // Reflect changes in UI
            _sst.UserInterface.PopulateModServerListUi();
        }
    }
}