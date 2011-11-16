
namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeInfo
    {
        public string TeamProject { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int WorkItemCount { get; private set; }

        public WorkItemTypeInfo(string teamProject, string name, string description, int workItemCount)
        {
            this.TeamProject = teamProject;
            this.Name = name;
            this.Description = description;
            this.WorkItemCount = workItemCount;
        }
    }
}