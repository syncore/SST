using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace SSB
{
    /// <summary>
    /// Class responsible for handling SSB's configuration.
    /// </summary>
    public class ConfigHandler
    {
        /// <summary>
        /// Core cfg item: Gets or sets SSB's initial admin users.
        /// </summary>
        /// <value>
        /// SSB's initial admin users.
        /// </value>
        public List<string> InitialAdminUsers { get; set; }

        /// <summary>
        /// Reads the configuration.
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

                    // Core SSB options
                    InitialAdminUsers = cfg.core.admins;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading configuration " + ex);
                RestoreDefaultConfiguration();
            }
        }

        /// <summary>
        /// Restores the default configuration.
        /// </summary>
        public void RestoreDefaultConfiguration()
        {
            // Load these fail-safe defaults and save as the new configuration
            var coreOptions = new CoreConfig
            {
                admins = new List<string>()
            };
            var config = new Configuration
            {
                core = coreOptions
            };
            string json = JsonConvert.SerializeObject(config);
            using (FileStream fs = File.Create(Filepaths.ConfigurationFilePath))
            using (TextWriter writer = new StreamWriter(fs))
            {
                writer.WriteLine(json);
                Debug.WriteLine("> Loaded default configuration from: " + Filepaths.ConfigurationFilePath);
            }
        }

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        public void ValidateConfiguration()
        {
            // Nothing here yet.
        }

        /// <summary>
        /// Writes the configuration to the disk.
        /// </summary>
        public void WriteConfiguration()
        {
            var coreOptions = new CoreConfig
            {
                admins = InitialAdminUsers
            };
            var config = new Configuration
            {
                core = coreOptions
            };
            string json = JsonConvert.SerializeObject(config);
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