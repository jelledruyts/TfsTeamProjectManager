using Microsoft.Practices.Prism.Events;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event that is raised when the selected Team Projects changed.
    /// </summary>
    public class TeamProjectSelectionChangedEvent : CompositePresentationEvent<TeamProjectSelectionChangedEventArgs>
    {
    }
}