using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class ComparisonSource
    {
        public string Name { get; private set; }
        public ICollection<WorkItemTypeDefinition> WorkItemTypes { get; private set; }
        public string Description { get; private set; }

        public ComparisonSource(string name, ICollection<WorkItemTypeDefinition> workItemTypes)
        {
            this.Name = name;
            this.WorkItemTypes = workItemTypes ?? new WorkItemTypeDefinition[0];
            this.Description = this.Name;
            if (this.WorkItemTypes.Count > 0)
            {
                this.Description += string.Format(CultureInfo.CurrentCulture, " ({0})", string.Join(", ", this.WorkItemTypes.Select(w => w.Name)));
            }
        }
    }
}