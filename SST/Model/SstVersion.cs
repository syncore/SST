// ReSharper disable InconsistentNaming

namespace SST.Model
{
    /// <summary>
    /// Model class for SST version updates.
    /// </summary>
    public class SstVersion
    {
        /// <summary>
        /// Gets or sets the latest version.
        /// </summary>
        /// <value>The latest version.</value>
        public double latestVersion { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        /// <value>The release date.</value>
        public string releaseDate { get; set; }

        /// <summary>
        /// Gets or sets the abbreviated form of the release date.
        /// </summary>
        /// <value>The abbreviated form of the release date.</value>
        public string releaseDateShort { get; set; }
    }
}
