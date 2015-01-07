using System;
using System.Threading.Tasks;
using SSB.Config;
using SSB.Core.Commands.Admin;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Modules
{
    /// <summary>
    /// Module: enable or disable the ability to list the active QL servers for a given gametype and region.
    /// </summary>
    public class Servers : IModule
    {
        public const string NameModule = "servers";
        private readonly ConfigHandler _configHandler;
        private readonly SynServerBot _ssb;
        private int _minModuleArgs = 3;

        public Servers(SynServerBot ssb)
        {
            _ssb = ssb;
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
        /// Gets or sets the date and time that the query command was last used.
        /// </summary>
        /// <value>
        /// The date and time that the query command was last used.
        /// </value>
        public DateTime LastQueryTime { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum servers to display.
        /// </summary>
        /// <value>
        /// The maximum servers to display.
        /// </value>
        public uint MaxServersToDisplay { get; set; }

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
        /// Gets or sets the time between queries.
        /// </summary>
        /// <value>
        /// The time between queries.
        /// </value>
        public double TimeBetweenQueries { get; set; }

        /// <summary>
        /// Disables the active servers module.
        /// </summary>
        public async Task DisableServers()
        {
            UpdateConfig(false);
            await
                _ssb.QlCommands.QlCmdSay(
                    "^2[SUCCESS]^7 Active server list display is ^1disabled^7.");
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} {2} [off] <maxservers> <timebetween> -^7 max servers to show, time between queries",
                CommandProcessor.BotCommandPrefix, c.CmdName, ModuleCmd.ServersArg));
        }

        /// <summary>
        /// Enables the active servers module.
        /// </summary>
        /// <param name="maxServers">The maximum servers to display.</param>
        /// <param name="timeBetween">The time in seconds that must elapse between users issuing the
        ///  query command.</param>
        public async Task EnableServers(uint maxServers, double timeBetween)
        {
            MaxServersToDisplay = maxServers;
            TimeBetweenQueries = timeBetween;
            UpdateConfig(true);
            await
                _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^3[ACTIVESERVERS]^7 Active server listing is now ^2ON^7. Players can see up to ^5{0}^7 active servers every ^5{1}^7 seconds.",
                        maxServers, timeBetween));
        }

        /// <summary>
        /// Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        public async Task EvalModuleCmdAsync(CmdArgs c)
        {
            if (c.Args.Length < _minModuleArgs)
            {
                await DisplayArgLengthError(c);
                return;
            }
            if (c.Args[2].Equals("off"))
            {
                await DisableServers();
                return;
            }
            if (c.Args.Length != 4)
            {
                await DisplayArgLengthError(c);
                return;
            }
            uint maxNum;
            if (!uint.TryParse(c.Args[2], out maxNum))
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        "^1[ERROR]^3 The maximum servers to display must be a number greater than zero!");
                return;
            }
            double timebtNum;
            if (!double.TryParse(c.Args[3], out timebtNum))
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^1[ERROR]^3 The time limit to impose between the {0} cmd must be a number.",
                        CommandProcessor.CmdServers));
                return;
            }
            if (timebtNum < 0)
            {
                await
                    _ssb.QlCommands.QlCmdSay(string.Format(
                        "^1[ERROR]^3 The time limit to impose between the {0} cmd must be a number greater than zero.",
                        CommandProcessor.CmdServers));
                return;
            }
            await EnableServers(maxNum, timebtNum);
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        public void LoadConfig()
        {
            _configHandler.ReadConfiguration();

            // Valid values?
            if (_configHandler.Config.ServersOptions.maxServers == 0 ||
                _configHandler.Config.ServersOptions.timeBetweenQueries < 0)
            {
                Active = false;
                _configHandler.Config.ServersOptions.SetDefaults();
                return;
            }
            Active = _configHandler.Config.ServersOptions.isActive;
            MaxServersToDisplay = _configHandler.Config.ServersOptions.maxServers;
            TimeBetweenQueries = _configHandler.Config.ServersOptions.timeBetweenQueries;
        }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        ///     if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        ///     updating the configuration.
        public void UpdateConfig(bool active)
        {
            Active = active;
            if (active)
            {
                _configHandler.Config.ServersOptions.isActive = true;
                _configHandler.Config.ServersOptions.maxServers = MaxServersToDisplay;
                _configHandler.Config.ServersOptions.timeBetweenQueries = TimeBetweenQueries;
            }
            else
            {
                _configHandler.Config.ServersOptions.SetDefaults();
            }

            _configHandler.WriteConfiguration();
        }
    }
}