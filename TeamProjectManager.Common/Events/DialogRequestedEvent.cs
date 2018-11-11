using Prism.Events;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event that is raised when the task history is requested.
    /// </summary>
    public class DialogRequestedEvent : PubSubEvent<DialogType>
    {
    }
}