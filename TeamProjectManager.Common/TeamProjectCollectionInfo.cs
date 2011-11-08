using System;

namespace TeamProjectManager.Common
{
    public class TeamProjectCollectionInfo
    {
        public string Name { get; private set; }
        public Uri Uri { get; private set; }

        public TeamProjectCollectionInfo(string name, Uri uri)
        {
            this.Name = name;
            this.Uri = uri;
        }
    }
}