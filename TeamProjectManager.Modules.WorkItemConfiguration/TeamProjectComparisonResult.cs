using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class TeamProjectComparisonResult
    {
        public string TeamProject { get; private set; }
        public ICollection<WorkItemConfigurationComparisonResult> WorkItemConfigurationResults { get; private set; }
        public WorkItemConfigurationComparisonResult BestMatch { get; private set; }
        public string Summary { get; private set; }

        public TeamProjectComparisonResult(string teamProject, ICollection<WorkItemConfigurationComparisonResult> workItemConfigurationResults)
        {
            this.TeamProject = teamProject;
            this.WorkItemConfigurationResults = workItemConfigurationResults ?? new WorkItemConfigurationComparisonResult[0];
            this.BestMatch = this.WorkItemConfigurationResults.First(s => s.PercentMatch == this.WorkItemConfigurationResults.Max(r => r.PercentMatch));
            this.Summary = string.Join("; ", workItemConfigurationResults.Select(w => string.Format(CultureInfo.CurrentCulture, "{0} ({1:0%})", w.Source.Name, w.PercentMatch)));
        }
    }
}