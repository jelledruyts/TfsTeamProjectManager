
namespace TeamProjectManager.Common
{
    /// <summary>
    /// Provides information about a Team Foundation Server.
    /// </summary>
    public sealed class TeamFoundationServerInfo
    {
        /// <summary>
        /// Gets the major version of the Team Foundation Server.
        /// </summary>
        public TfsMajorVersion MajorVersion { get; private set; }

        /// <summary>
        /// Gets the display version of the Team Foundation Server.
        /// </summary>
        public string DisplayVersion { get; private set; }

        /// <summary>
        /// Gets the short display version of the Team Foundation Server.
        /// </summary>
        public string ShortDisplayVersion { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamFoundationServerInfo"/> class.
        /// </summary>
        /// <param name="majorVersion">The major version of the Team Foundation Server.</param>
        /// <param name="displayVersion">The display version of the Team Foundation Server.</param>
        /// <param name="shortDisplayVersion">The short display version of the Team Foundation Server.</param>
        public TeamFoundationServerInfo(TfsMajorVersion majorVersion, string displayVersion, string shortDisplayVersion)
        {
            this.MajorVersion = majorVersion;
            this.DisplayVersion = displayVersion;
            this.ShortDisplayVersion = shortDisplayVersion;
        }
    }
}