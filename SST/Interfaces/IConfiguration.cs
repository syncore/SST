namespace SST.Interfaces
{
    /// <summary>
    /// Configuration interface
    /// </summary>
    internal interface IConfiguration
    {
        /// <summary>
        ///     Checks whether the configuration already exists
        /// </summary>
        /// <returns><c>true</c> if configuration exists, otherwise <c>false</c></returns>
        bool CfgExists();

        /// <summary>
        ///     Loads the configuration.
        /// </summary>
        void LoadCfg();

        /// <summary>
        ///     Loads the default configuration.
        /// </summary>
        void LoadDefaultCfg();

        /// <summary>
        ///     Saves the configuration.
        /// </summary>
        void SaveCfg();
    }
}