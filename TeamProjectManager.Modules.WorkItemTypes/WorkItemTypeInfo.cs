
using System.Collections.Generic;
namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeInfo
    {
        public string TeamProject { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int WorkItemCount { get; private set; }
        public ICollection<string> WorkItemCategories { get; private set; }
        public string WorkItemCategoriesList { get; private set; }

        public WorkItemTypeInfo(string teamProject, string name, string description, int workItemCount, ICollection<string> workItemCategories)
        {
            this.TeamProject = teamProject;
            this.Name = name;
            this.Description = description;
            this.WorkItemCount = workItemCount;
            this.WorkItemCategories = workItemCategories;
            this.WorkItemCategoriesList = this.WorkItemCategories == null ? null : string.Join(", ", this.WorkItemCategories);
        }
    }
}