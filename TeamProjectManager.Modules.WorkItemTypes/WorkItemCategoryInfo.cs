using System.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemCategoryInfo
    {
        public string TeamProject { get; private set; }
        public WorkItemCategoryList WorkItemCategoryList { get; private set; }
        public WorkItemCategory WorkItemCategory { get; private set; }
        public string WorkItemTypeNames { get; private set; }

        public WorkItemCategoryInfo(string teamProject, WorkItemCategoryList workItemCategoryList, WorkItemCategory workItemCategory)
        {
            this.TeamProject = teamProject;
            this.WorkItemCategoryList = workItemCategoryList;
            this.WorkItemCategory = workItemCategory;
            this.WorkItemTypeNames = (this.WorkItemCategory == null || this.WorkItemCategory.WorkItemTypes == null) ? null : string.Join(", ", this.WorkItemCategory.WorkItemTypes.Select(s => s.Name));
        }
    }
}