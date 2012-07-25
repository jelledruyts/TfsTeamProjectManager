using System;

namespace TeamProjectManager.Shell.Infrastructure
{
    public class ApplicationVersion
    {
        public Version VersionNumber { get; private set; }
        public Uri DownloadUrl { get; private set; }

        public ApplicationVersion(Version versionNumber, Uri downloadUrl)
        {
            this.VersionNumber = versionNumber;
            this.DownloadUrl = downloadUrl;
        }
    }
}