using System.Collections.Generic;
using System.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class ComparisonSourceComparisonResult
    {
        public ComparisonSource Source { get; private set; }
        public ICollection<WorkItemTypeComparisonResult> WorkItemTypeResults { get; private set; }
        public double PercentMatch { get; private set; }

        public ComparisonSourceComparisonResult(ComparisonSource source, ICollection<WorkItemTypeComparisonResult> workItemTypeResults)
        {
            this.Source = source;
            this.WorkItemTypeResults = workItemTypeResults;
            this.PercentMatch = workItemTypeResults.Sum(r => r.PercentMatch) / workItemTypeResults.Count;
        }
    }
}