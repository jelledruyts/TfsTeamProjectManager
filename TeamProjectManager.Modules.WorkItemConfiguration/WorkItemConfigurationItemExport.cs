using System.Xml;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfigurationItemExport
    {
        public TeamProjectInfo TeamProject { get; private set; }
        public WorkItemConfigurationItem Item { get; private set; }
        public string SaveAsFileName { get; private set; }

        public WorkItemConfigurationItemExport(TeamProjectInfo teamProject, WorkItemConfigurationItem item)
            : this(teamProject, item, null)
        {
        }

        public WorkItemConfigurationItemExport(TeamProjectInfo teamProject, WorkItemConfigurationItem item, string saveAsFileName)
        {
            this.TeamProject = teamProject;
            this.Item = item;
            this.SaveAsFileName = saveAsFileName;
        }
    }
}