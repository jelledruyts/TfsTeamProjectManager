using System;

namespace TeamProjectManager.Modules.Activity
{
    public class ActivityInfo
    {
        public string TeamProject { get; private set; }
        public DateTimeOffset? LastActivityInVersionControlTime { get; private set; }
        public string LastActivityInVersionControlDescription { get; private set; }
        public DateTimeOffset? LastActivityInWorkItemTrackingTime { get; private set; }
        public string LastActivityInWorkItemTrackingDescription { get; private set; }
        public DateTimeOffset? LastActivityInTeamBuildTime { get; private set; }
        public string LastActivityInTeamBuildDescription { get; private set; }
        public DateTimeOffset? LastActivityTime { get; private set; }
        public string LastActivityDescription { get; private set; }

        public ActivityInfo(string teamProject, DateTimeOffset? lastActivityInVersionControlTime, string lastActivityInVersionControlDescription, DateTimeOffset? lastActivityInWorkItemTrackingTime, string lastActivityInWorkItemTrackingDescription, DateTimeOffset? lastActivityInTeamBuildTime, string lastActivityInTeamBuildDescription)
        {
            this.TeamProject = teamProject;
            this.LastActivityInVersionControlTime = lastActivityInVersionControlTime;
            this.LastActivityInVersionControlDescription = lastActivityInVersionControlDescription;
            this.LastActivityInWorkItemTrackingTime = lastActivityInWorkItemTrackingTime;
            this.LastActivityInWorkItemTrackingDescription = lastActivityInWorkItemTrackingDescription;
            this.LastActivityInTeamBuildTime = lastActivityInTeamBuildTime;
            this.LastActivityInTeamBuildDescription = lastActivityInTeamBuildDescription;

            // TODO: Set LastActivityTime and LastActivityDescription.
        }
    }
}