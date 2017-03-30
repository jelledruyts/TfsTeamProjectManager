using System.Collections.Generic;
using System.Linq;

namespace TeamProjectManager.Modules.Activity
{
    public class TeamProjectActivityInfo
    {
        public string TeamProject { get; private set; }
        public IList<ComponentActivityInfo> Activities { get; private set; }
        public ComponentActivityInfo MostRecentActivity { get; private set; }

        public TeamProjectActivityInfo(string teamProject, IList<ComponentActivityInfo> activities)
        {
            this.TeamProject = teamProject;
            this.Activities = (activities ?? new ComponentActivityInfo[0]).OrderByDescending(a => a.Time).ToArray();
            this.MostRecentActivity = this.Activities.FirstOrDefault();
        }
    }
}