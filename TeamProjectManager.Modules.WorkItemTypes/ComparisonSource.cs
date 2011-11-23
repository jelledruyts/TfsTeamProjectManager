using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class ComparisonSource
    {
        public string Name { get; private set; }
        public ICollection<WorkItemTypeDefinition> WorkItemTypeFiles { get; private set; }
        public string Description { get; private set; }

        public ComparisonSource(string name, ICollection<WorkItemTypeDefinition> workItemTypeFiles)
        {
            this.Name = name;
            this.WorkItemTypeFiles = workItemTypeFiles ?? new WorkItemTypeDefinition[0];
            this.Description = this.Name;
            if (this.WorkItemTypeFiles.Count > 0)
            {
                this.Description += string.Format(CultureInfo.CurrentCulture, " ({0})", string.Join(", ", this.WorkItemTypeFiles.Select(w => w.Name)));
            }
        }
    }
}