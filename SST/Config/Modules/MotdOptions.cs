﻿namespace SST.Config.Modules
{
    /// <summary>
    /// Model class that represents the message of the day module options for the configuration file.
    /// </summary>
    public class MotdOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether this module is active.
        /// </summary>
        /// <value><c>true</c> if this module is active; otherwise, <c>false</c>.</value>
        // ReSharper disable once InconsistentNaming
        public bool isActive { get; set; }

        /// <summary>
        /// Gets or sets the message to repeat.
        /// </summary>
        /// <value>The message to repeat.</value>
        // ReSharper disable once InconsistentNaming
        public string message { get; set; }

        /// <summary>
        /// Gets or sets the repeat time in minutes.
        /// </summary>
        /// <value>The repeat time in minutes.</value>
        // ReSharper disable once InconsistentNaming
        public int repeatInterval { get; set; }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        public void SetDefaults()
        {
            isActive = false;
            message = "Welcome to the server. This is the message of the day.";
            repeatInterval = 2;
        }
    }
}
