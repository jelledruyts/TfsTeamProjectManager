using System.Diagnostics;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Defines a log message.
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// The message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The details.
        /// </summary>
        public string Details { get; private set; }

        /// <summary>
        /// The type of event.
        /// </summary>
        public TraceEventType EventType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="details">The details.</param>
        /// <param name="eventType">The type of event.</param>
        public LogMessage(string message, string details, TraceEventType eventType)
        {
            this.Message = message;
            this.Details = details;
            this.EventType = eventType;
        }
    }
}