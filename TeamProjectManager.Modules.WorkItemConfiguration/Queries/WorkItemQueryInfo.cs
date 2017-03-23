using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Queries
{
    public class WorkItemQueryInfo
    {
        public string TeamProject { get; private set; }
        public Guid Id { get; private set; }
        public string Path { get; private set; }
        public string Name { get; private set; }
        public string Text { get; set; }
        public QueryType Type { get; private set; }

        public WorkItemQueryInfo(string path, string name, string text)
            : this(null, Guid.Empty, path, name, text, QueryType.Invalid)
        {
        }

        public WorkItemQueryInfo(string teamProject, Guid id, string path, string name, string text, QueryType type)
        {
            this.TeamProject = teamProject;
            this.Id = id;
            this.Path = path;
            this.Name = name;
            this.Type = type;
            this.Text = text;
        }

        public WorkItemQueryInfo Clone()
        {
            return new WorkItemQueryInfo(this.TeamProject, this.Id, this.Path, this.Name, this.Text, this.Type);
        }
    }
}