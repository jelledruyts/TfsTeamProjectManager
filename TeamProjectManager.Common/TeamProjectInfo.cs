using System;

namespace TeamProjectManager.Common
{
    public sealed class TeamProjectInfo
    {
        public TeamProjectCollectionInfo TeamProjectCollection { get; private set; }
        public string Name { get; private set; }
        public Uri Uri { get; private set; }

        public TeamProjectInfo(TeamProjectCollectionInfo teamProjectCollection, string name, Uri uri)
        {
            this.TeamProjectCollection = teamProjectCollection;
            this.Name = name;
            this.Uri = uri;
        }
    }
}