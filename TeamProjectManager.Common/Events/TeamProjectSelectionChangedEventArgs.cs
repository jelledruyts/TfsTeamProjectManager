using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace TeamProjectManager.Common.Events
{
    public class TeamProjectSelectionChangedEventArgs : EventArgs
    {
        public RegisteredProjectCollection SelectedTeamProjectCollection { get; private set; }
        public ICollection<ProjectInfo> SelectedTeamProjects { get; private set; }

        public TeamProjectSelectionChangedEventArgs(RegisteredProjectCollection selectedTeamProjectCollection, ICollection<ProjectInfo> selectedTeamProjects)
        {
            this.SelectedTeamProjectCollection = selectedTeamProjectCollection;
            this.SelectedTeamProjects = selectedTeamProjects;
        }
    }
}