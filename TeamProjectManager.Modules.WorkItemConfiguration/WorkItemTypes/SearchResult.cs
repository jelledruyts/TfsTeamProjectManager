
namespace TeamProjectManager.Modules.WorkItemConfiguration.WorkItemTypes
{
    public class SearchResult
    {
        public string TeamProject { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
        public string Reason { get; private set; }

        public SearchResult(string teamProject, string type, string name, string reason)
        {
            this.TeamProject = teamProject;
            this.Type = type;
            this.Name = name;
            this.Reason = reason;
        }
    }
}