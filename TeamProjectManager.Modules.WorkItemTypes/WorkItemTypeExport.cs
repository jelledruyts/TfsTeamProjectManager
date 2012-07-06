using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeExport
    {
        public WorkItemTypeInfo WorkItemType { get; private set; }
        public string SaveAsFileName { get; private set; }
        public WorkItemTypeDefinition WorkItemTypeDefinition { get; set; }

        public WorkItemTypeExport(WorkItemTypeInfo workItemType)
            : this(workItemType, null)
        {
        }

        public WorkItemTypeExport(WorkItemTypeInfo workItemType, string saveAsFileName)
        {
            this.WorkItemType = workItemType;
            this.SaveAsFileName = saveAsFileName;
        }
    }
}