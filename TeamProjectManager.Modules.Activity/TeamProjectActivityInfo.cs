using System.Collections.Generic;
using System.Linq;

namespace TeamProjectManager.Modules.Activity
{
    public class TeamProjectActivityInfo
    {
        public string TeamProject { get; private set; }
        public IList<ComponentActivityInfo> SourceControlActivities { get; private set; }
        public IList<ComponentActivityInfo> WorkItemTrackingActivities { get; private set; }
        public IList<ComponentActivityInfo> TeamBuildActivities { get; private set; }
        public IList<ComponentActivityInfo> AllActivities { get; private set; }
        public ComponentActivityInfo MostRecentSourceControlActivity { get; private set; }
        public ComponentActivityInfo MostRecentWorkItemTrackingActivity { get; private set; }
        public ComponentActivityInfo MostRecentTeamBuildActivity { get; private set; }
        public ComponentActivityInfo MostRecentActivity { get; private set; }

        public TeamProjectActivityInfo(string teamProject, int numberOfActivities, IEnumerable<ComponentActivityInfo> sourceControlActivities, IEnumerable<ComponentActivityInfo> workItemTrackingActivities, IEnumerable<ComponentActivityInfo> teamBuildActivities)
        {
            this.TeamProject = teamProject;
            this.SourceControlActivities = (sourceControlActivities ?? new ComponentActivityInfo[0]).OrderByDescending(a => a.Time).ToArray();
            this.WorkItemTrackingActivities = (workItemTrackingActivities ?? new ComponentActivityInfo[0]).OrderByDescending(a => a.Time).ToArray();
            this.TeamBuildActivities = (teamBuildActivities ?? new ComponentActivityInfo[0]).OrderByDescending(a => a.Time).ToArray();
            this.MostRecentSourceControlActivity = this.SourceControlActivities.FirstOrDefault();
            this.MostRecentWorkItemTrackingActivity = this.WorkItemTrackingActivities.FirstOrDefault();
            this.MostRecentTeamBuildActivity = this.TeamBuildActivities.FirstOrDefault();
            this.AllActivities = this.SourceControlActivities.Concat(this.WorkItemTrackingActivities).Concat(this.TeamBuildActivities).OrderByDescending(a => a.Time).Take(numberOfActivities).ToArray();
            this.MostRecentActivity = this.AllActivities.FirstOrDefault();
        }
    }
}