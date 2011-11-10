using System;

namespace TeamProjectManager.Common
{
    public class TeamProjectInfo
    {
        public string Name { get; private set; }
        public Uri Uri { get; private set; }

        public TeamProjectInfo(string name, Uri uri)
        {
            this.Name = name;
            this.Uri = uri;
        }
    }
}