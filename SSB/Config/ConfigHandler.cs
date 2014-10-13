using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SSB.Util;

namespace SSB.Config
{
    /// <summary>
    ///     Class responsible for handling SSB's configuration.
    /// </summary>
    public class ConfigHandler
    {
        /// <summary>
        ///     Core cfg item: Gets or sets SSB's owner(s).
        /// </summary>
        /// <value>
        ///     SSB's owner(s).
        /// </value>
        public HashSet<string> Owners { get; set; }

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

                    // Core SSB options
                    Owners = cfg.core.owners;
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
            var owners = new HashSet<string> { "syncore" };
            var coreOptions = new CoreConfig
            {
                owners = owners
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
        ///     Validates the configuration.
        /// </summary>
        public void ValidateConfiguration()
        {
            // Nothing here yet.
        }

        /// <summary>
        ///     Writes the configuration to the disk.
        /// </summary>
        public void WriteConfiguration()
        {
            var coreOptions = new CoreConfig
            {
                owners = Owners
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