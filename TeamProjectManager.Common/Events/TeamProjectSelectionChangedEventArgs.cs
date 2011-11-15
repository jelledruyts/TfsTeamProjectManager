using System;
using System.Collections.Generic;

namespace TeamProjectManager.Common.Events
{
    public class TeamProjectSelectionChangedEventArgs : EventArgs
    {
        public ICollection<TeamProjectInfo> SelectedTeamProjects { get; private set; }

        public TeamProjectSelectionChangedEventArgs(ICollection<TeamProjectInfo> selectedTeamProjects)
        {
            this.SelectedTeamProjects = selectedTeamProjects;
        }
    }
}