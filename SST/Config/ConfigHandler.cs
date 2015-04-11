using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using SST.Config.Core;
using SST.Config.Modules;
using SST.Util;

namespace SST.Config
{
    /// <summary>
    ///     Class responsible for handling SST's configuration.
    /// </summary>
    public class ConfigHandler
    {
        private readonly Type _logClassType = MethodBase.GetCurrentMethod().DeclaringType;
        private readonly string _logPrefix = "[CFG]";

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConfigHandler" /> class.
        /// </summary>
        public ConfigHandler()
        {
            Config = new Configuration();
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Configuration Config { get; set; }

        /// <summary>
        ///     Reads the configuration.
        /// </summary>
        public void ReadConfiguration()
        {
            VerifyConfigLocation();
            try
            {
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
                Log.WriteCritical("Error loading configuration " + ex.Message, _logClassType, _logPrefix);
                RestoreDefaultConfiguration();
            }
        }

        /// <summary>
        ///     Restores the default configuration.
        /// </summary>
        public void RestoreDefaultConfiguration()
        {
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

            Config = new Configuration
            {
                AccountDateOptions = acctDateOptions,
                AccuracyOptions = accuracyOptions,
                AutoVoterOptions = autoVoterOptions,
                CoreOptions = coreOptions,
                EarlyQuitOptions = earlyQuitOptions,
                EloLimitOptions = eloLimitOptions,
                IrcOptions = ircOptions,
                MotdOptions = motdOptions,
                PickupOptions = pickupOptions,
                ServersOptions = serversOptions
            };

            var json = JsonConvert.SerializeObject(Config);
            using (var fs = File.Create(Filepaths.ConfigurationFilePath))
            using (TextWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine(json);
                Log.WriteCritical("Restored fail-safe, default configuration to: " + Filepaths.ConfigurationFilePath,
                    _logClassType, _logPrefix);
            }
        }

        /// <summary>
        /// Verifies the configuration location and file.
        /// </summary>
        public void VerifyConfigLocation()
        {
            if (!Directory.Exists(Filepaths.DataDirectoryPath))
            {
                Log.WriteCritical("Data directory does not exist. Will attempt to create.",
                    _logClassType, _logPrefix);
                Filepaths.CreateDataDirectory();
            }
            if (!File.Exists(Filepaths.ConfigurationFilePath))
            {
                Log.WriteCritical("Config file does not exist. Restoring default config.",
                    _logClassType, _logPrefix);
                RestoreDefaultConfiguration();
            }
            var fileInfo = new FileInfo(Filepaths.ConfigurationFilePath);
            if (fileInfo.Length != 0) return;
            Log.WriteCritical("Config exists but is empty. Restoring default config.",
                _logClassType, _logPrefix);
            RestoreDefaultConfiguration();
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
                Log.Write(
                    "Wrote configuration to disk at: " + Filepaths.ConfigurationFilePath,
                    _logClassType, _logPrefix);
            }
        }
    }
}