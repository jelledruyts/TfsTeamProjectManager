using System;

namespace TeamProjectManager.Common
{
    public class TeamProjectInfo
    {
        public string Name { get; private set; }
        public Uri Uri { get; private set; }
        public bool IsDeleted { get; private set; }

        public TeamProjectInfo(string name, Uri uri, bool isDeleted)
        {
            this.Name = name;
            this.Uri = uri;
            this.IsDeleted = isDeleted;
        }
    }
}