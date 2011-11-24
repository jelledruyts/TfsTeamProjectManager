using System;
using System.Collections.Generic;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event arguments for Team Project selection change events.
    /// </summary>
    public class TeamProjectSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the Team Projects that have been selected.
        /// </summary>
        public ICollection<TeamProjectInfo> SelectedTeamProjects { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamProjectSelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="selectedTeamProjects">The Team Projects that have been selected.</param>
        public TeamProjectSelectionChangedEventArgs(ICollection<TeamProjectInfo> selectedTeamProjects)
        {
            this.SelectedTeamProjects = selectedTeamProjects;
        }
    }
}