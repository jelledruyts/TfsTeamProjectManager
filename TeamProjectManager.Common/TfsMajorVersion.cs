
namespace TeamProjectManager.Common
{
    /// <summary>
    /// Represents the major versions of Team Foundation Server.
    /// </summary>
    public enum TfsMajorVersion
    {
        /// <summary>
        /// Represents an unknown version of Team Foundation Server.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Represents Team Foundation Server 8 (TFS 2005).
        /// </summary>
        V8 = 8,

        /// <summary>
        /// Represents Team Foundation Server 9 (TFS 2008).
        /// </summary>
        V9 = 9,

        /// <summary>
        /// Represents Team Foundation Server 10 (TFS 2010).
        /// </summary>
        V10 = 10,

        /// <summary>
        /// Represents Team Foundation Server 11 (TFS 2012).
        /// </summary>
        V11 = 11
    }
}