using Microsoft.Practices.Prism.Events;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event that is raised when the selected Team Project Collection changed.
    /// </summary>
    public class TeamProjectCollectionSelectionChangedEvent : CompositePresentationEvent<TeamProjectCollectionSelectionChangedEventArgs>
    {
    }
}