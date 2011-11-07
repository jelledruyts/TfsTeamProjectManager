using System.Diagnostics;

namespace TeamProjectManager.Common.Infrastructure
{
    public class LogMessage
    {
        public string Message { get; private set; }
        public string Details { get; private set; }
        public TraceEventType EventType { get; private set; }

        public LogMessage(string message, string details, TraceEventType eventType)
        {
            this.Message = message;
            this.Details = details;
            this.EventType = eventType;
        }
    }
}