using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class TeamProjectComparisonResult
    {
        public string TeamProject { get; private set; }
        public ICollection<ComparisonSourceComparisonResult> SourceResults { get; private set; }
        public ComparisonSourceComparisonResult BestMatch { get; private set; }
        public string Summary { get; private set; }

        public TeamProjectComparisonResult(string teamProject, ICollection<ComparisonSourceComparisonResult> sourceResults)
        {
            this.TeamProject = teamProject;
            this.SourceResults = sourceResults ?? new ComparisonSourceComparisonResult[0];
            this.BestMatch = this.SourceResults.First(s => s.PercentMatch == this.SourceResults.Max(r => r.PercentMatch));
            this.Summary = string.Join("; ", sourceResults.Select(w => string.Format(CultureInfo.CurrentCulture, "{0} ({1:0%})", w.Source.Name, w.PercentMatch)));
        }
    }
}