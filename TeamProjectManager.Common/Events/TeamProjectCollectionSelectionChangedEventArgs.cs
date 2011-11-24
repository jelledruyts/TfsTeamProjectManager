using System;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event arguments for Team Project Collection selection change events.
    /// </summary>
    public class TeamProjectCollectionSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the Team Project Collection that has been selected.
        /// </summary>
        public TeamProjectCollectionInfo SelectedTeamProjectCollection { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamProjectCollectionSelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="selectedTeamProjectCollection">The Team Project Collection that has been selected.</param>
        public TeamProjectCollectionSelectionChangedEventArgs(TeamProjectCollectionInfo selectedTeamProjectCollection)
        {
            this.SelectedTeamProjectCollection = selectedTeamProjectCollection;
        }
    }
}