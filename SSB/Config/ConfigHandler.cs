using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SSB.Config.Core;
using SSB.Config.Modules;
using SSB.Model;
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
            // Load these fail-safe defaults and save as the new configuration
            // Bot owners
            var owners = new HashSet<string> {"syncore"};
            // Time in minutes after which cached elo data will expire
            uint eloCacheExpiration = 300; // 5 hours
            // Bot name
            string botName = "syncore";
            
            var acctDateOptions = new AccountDateOptions
            {
                isActive = false,
                minimumDaysRequired = 0
            };
            var accuracyOptions = new AccuracyOptions
            {
                isActive = false,
            };
            var autoVoterOptions = new AutoVoterOptions
            {
                isActive = false,
                autoVotes = new List<AutoVote>()
            };
            var coreOptions = new CoreOptions
            {
                botName = botName,
                owners = owners,
                eloCacheExpiration = eloCacheExpiration
            };
            var earlyQuitOptions = new EarlyQuitOptions
            {
                isActive = false,
                banTime = 0,
                banTimeScale = string.Empty
            };
            var eloLimitOptions = new EloLimitOptions
            {
                isActive = false,
                maximumRequiredElo = 0,
                minimumRequiredElo = 0
            };
            var motdOptions = new MotdOptions
            {
                isActive = false,
                message = string.Empty,
                repeatInterval = 0
            };

            Config.AccountDateOptions = acctDateOptions;
            Config.AccuracyOptions = accuracyOptions;
            Config.AutoVoterOptions = autoVoterOptions;
            Config.CoreOptions = coreOptions;
            Config.EarlyQuitOptions = earlyQuitOptions;
            Config.EloLimitOptions = eloLimitOptions;
            Config.MotdOptions = motdOptions;

            string json = JsonConvert.SerializeObject(Config);
            using (FileStream fs = File.Create(Filepaths.ConfigurationFilePath))
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
            string json = JsonConvert.SerializeObject(Config);
            using (FileStream fs = File.Create(Filepaths.ConfigurationFilePath))
            using (TextWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine(json);
                Debug.WriteLine("> Wrote configuration to disk at: " + Filepaths.ConfigurationFilePath +
                                " **");
            }
        }
    }
}