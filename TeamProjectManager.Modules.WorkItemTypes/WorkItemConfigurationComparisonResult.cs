using System.Collections.Generic;
using System.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemConfigurationComparisonResult
    {
        public WorkItemConfiguration Source { get; private set; }
        public WorkItemConfiguration Target { get; private set; }
        public ICollection<WorkItemConfigurationItemComparisonResult> ItemResults { get; private set; }
        public double PercentMatch { get; private set; }

        public WorkItemConfigurationComparisonResult(WorkItemConfiguration source, WorkItemConfiguration target, ICollection<WorkItemConfigurationItemComparisonResult> itemResults)
        {
            this.Source = source;
            this.Target = target;
            this.ItemResults = itemResults ?? new WorkItemConfigurationItemComparisonResult[0];
            this.PercentMatch = this.ItemResults.Count == 0 ? 1.0 : this.ItemResults.Average(r => r.PercentMatch);
        }
    }
}