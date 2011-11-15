
namespace TeamProjectManager.Common
{
    public sealed class TeamFoundationServerInfo
    {
        public TfsMajorVersion MajorVersion { get; private set; }
        public string DisplayVersion { get; private set; }
        public string ShortDisplayVersion { get; private set; }

        public TeamFoundationServerInfo(TfsMajorVersion majorVersion)
        {
            this.MajorVersion = majorVersion;
            switch (this.MajorVersion)
            {
                case TfsMajorVersion.Tfs2005:
                    this.DisplayVersion = "Team Foundation Server 2005";
                    this.ShortDisplayVersion = "TFS 2005";
                    break;
                case TfsMajorVersion.Tfs2008:
                    this.DisplayVersion = "Team Foundation Server 2008";
                    this.ShortDisplayVersion = "TFS 2008";
                    break;
                case TfsMajorVersion.Tfs2010:
                    this.DisplayVersion = "Team Foundation Server 2010";
                    this.ShortDisplayVersion = "TFS 2010";
                    break;
                default:
                    this.DisplayVersion = "Unknown version of Team Foundation Server";
                    this.ShortDisplayVersion = "Unknown TFS Version";
                    break;
            }
        }
    }
}