using Microsoft.Practices.Prism.Events;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event that is raised when a status message is published.
    /// </summary>
    public class StatusEvent : CompositePresentationEvent<StatusEventArgs>
    {
    }
}
