using System;
using System.Collections.Generic;

namespace TeamProjectManager.Common.Events
{
    public class TeamProjectSelectionChangedEventArgs : EventArgs
    {
        public TeamProjectCollectionInfo SelectedTeamProjectCollection { get; private set; }
        public ICollection<TeamProjectInfo> SelectedTeamProjects { get; private set; }

        public TeamProjectSelectionChangedEventArgs(TeamProjectCollectionInfo selectedTeamProjectCollection, ICollection<TeamProjectInfo> selectedTeamProjects)
        {
            this.SelectedTeamProjectCollection = selectedTeamProjectCollection;
            this.SelectedTeamProjects = selectedTeamProjects;
        }
    }
}