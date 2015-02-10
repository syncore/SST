using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SSB.Config.Core;
using SSB.Config.Modules;
using SSB.Util;

namespace SSB.Config
{
    /// <summary>
    ///     Class responsible for handling SSB's configuration.
    /// </summary>
    public class ConfigHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConfigHandler" /> class.
        /// </summary>
        public ConfigHandler()
        {
            Config = new Configuration();
        }

        public Configuration Config { get; set; }

        /// <summary>
        ///     Reads the configuration.
        /// </summary>
        public void ReadConfiguration()
        {
            try
            {
                // Might need fail-safe configuration
                ValidateConfiguration();

                using (var sr = new StreamReader(Filepaths.ConfigurationFilePath))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var serializer = new JsonSerializer();
                    var cfg = serializer.Deserialize<Configuration>(jsonTextReader);

                    Config = cfg;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading configuration " + ex.Message);
                RestoreDefaultConfiguration();
            }
        }

        /// <summary>
        ///     Restores the default configuration.
        /// </summary>
        public void RestoreDefaultConfiguration()
        {
            // TODO: re-examine some reasonable defaults for these options, including isActive before release
            // Load these fail-safe defaults and save as the new configuration

            var acctDateOptions = new AccountDateOptions();
            acctDateOptions.SetDefaults();
            var accuracyOptions = new AccuracyOptions();
            accuracyOptions.SetDefaults();
            var autoVoterOptions = new AutoVoterOptions();
            autoVoterOptions.SetDefaults();
            var coreOptions = new CoreOptions();
            coreOptions.SetDefaults();
            var earlyQuitOptions = new EarlyQuitOptions();
            earlyQuitOptions.SetDefaults();
            var eloLimitOptions = new EloLimitOptions();
            eloLimitOptions.SetDefaults();
            var ircOptions = new IrcOptions();
            ircOptions.SetDefaults();
            var motdOptions = new MotdOptions();
            motdOptions.SetDefaults();
            var pickupOptions = new PickupOptions();
            pickupOptions.SetDefaults();
            var serversOptions = new ServersOptions();
            serversOptions.SetDefaults();

            Config.AccountDateOptions = acctDateOptions;
            Config.AccuracyOptions = accuracyOptions;
            Config.AutoVoterOptions = autoVoterOptions;
            Config.CoreOptions = coreOptions;
            Config.EarlyQuitOptions = earlyQuitOptions;
            Config.EloLimitOptions = eloLimitOptions;
            Config.IrcOptions = ircOptions;
            Config.MotdOptions = motdOptions;
            Config.PickupOptions = pickupOptions;
            Config.ServersOptions = serversOptions;

            var json = JsonConvert.SerializeObject(Config);
            using (var fs = File.Create(Filepaths.ConfigurationFilePath))
            using (TextWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine(json);
                Debug.WriteLine("> Loaded default configuration from: " + Filepaths.ConfigurationFilePath);
            }
        }

        /// <summary>
        ///     Validates the configuration.
        /// </summary>
        public void ValidateConfiguration()
        {
            // Nothing here; now validation is handled from the class that calls the cfg handler via LoadConfig() on as-needed basis.
        }

        /// <summary>
        ///     Writes the configuration to the disk.
        /// </summary>
        public void WriteConfiguration()
        {
            var json = JsonConvert.SerializeObject(Config);
            using (var fs = File.Create(Filepaths.ConfigurationFilePath))
            using (TextWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine(json);
                Debug.WriteLine("> Wrote configuration to disk at: " + Filepaths.ConfigurationFilePath +
                                " **");
            }
        }
    }
}