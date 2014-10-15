namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for various bot modules.
    /// </summary>
    public interface ISsbModule
    {
        /// <summary>
        /// Determines whether this module is active.
        /// </summary>
        /// <returns></returns>
        bool IsEnabled { get; set; }
    }
}