using Microsoft.Practices.Prism.Events;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event that is raised when a log message is published.
    /// </summary>
    public class LogMessagePublishedEvent : CompositePresentationEvent<LogMessage>
    {
    }
}
