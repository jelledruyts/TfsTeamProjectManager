
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
        V8 = 80,

        /// <summary>
        /// Represents Team Foundation Server 9 (TFS 2008).
        /// </summary>
        V9 = 90,

        /// <summary>
        /// Represents Team Foundation Server 10 (TFS 2010).
        /// </summary>
        V10 = 100,

        /// <summary>
        /// Represents Team Foundation Server 10 SP1 (TFS 2010 SP1).
        /// </summary>
        V10SP1 = 101,

        /// <summary>
        /// Represents Team Foundation Server 11 (TFS 2012).
        /// </summary>
        V11 = 110,

        /// <summary>
        /// Represents Team Foundation Server 11 Update 1 (TFS 2012.1).
        /// </summary>
        V11Update1 = 111,

        /// <summary>
        /// Represents Team Foundation Server 11 Update 2 (TFS 2012.2).
        /// </summary>
        V11Update2 = 112,

        /// <summary>
        /// Represents Team Foundation Server 12 (TFS 2013).
        /// </summary>
        V12 = 120,

        /// <summary>
        /// Represents Team Foundation Server 14 (TFS 2015).
        /// </summary>
        V14 = 140,

        /// <summary>
        /// Represents the highest known version of Team Foundation Server.
        /// </summary>
        HighestKnownVersion = V14
    }
}