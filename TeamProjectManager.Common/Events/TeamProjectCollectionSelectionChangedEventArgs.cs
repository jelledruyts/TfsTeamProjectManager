using System;

namespace TeamProjectManager.Common.Events
{
    public class TeamProjectCollectionSelectionChangedEventArgs : EventArgs
    {
        public TeamProjectCollectionInfo SelectedTeamProjectCollection { get; private set; }

        public TeamProjectCollectionSelectionChangedEventArgs(TeamProjectCollectionInfo selectedTeamProjectCollection)
        {
            this.SelectedTeamProjectCollection = selectedTeamProjectCollection;
        }
    }
}