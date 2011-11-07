using System;
using System.Diagnostics;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Common.Events
{
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public string Details { get; private set; }
        public TraceEventType EventType { get; private set; }
        public Exception Exception { get; private set; }
        public ApplicationTask Task { get; private set; }

        public StatusEventArgs(string message)
            : this(message, null, null, TraceEventType.Information, null)
        {
        }

        public StatusEventArgs(string message, string details)
            : this(message, details, null, TraceEventType.Information, null)
        {
        }

        public StatusEventArgs(string message, TraceEventType eventType)
            : this(message, null, null, eventType, null)
        {
        }

        public StatusEventArgs(string message, string details, TraceEventType eventType)
            : this(message, details, null, eventType, null)
        {
        }

        public StatusEventArgs(string message, Exception exception)
            : this(message, null, exception, TraceEventType.Error, null)
        {
        }

        public StatusEventArgs(string message, string details, Exception exception)
            : this(message, details, exception, TraceEventType.Error, null)
        {
        }

        public StatusEventArgs(string message, Exception exception, TraceEventType eventType)
            : this(message, null, exception, TraceEventType.Error, null)
        {
        }

        public StatusEventArgs(string message, string details, Exception exception, TraceEventType eventType)
            : this(message, null, exception, eventType, null)
        {
        }

        public StatusEventArgs(ApplicationTask task)
            : this(task.Name, null, null, TraceEventType.Information, task)
        {
        }

        private StatusEventArgs(string message, string details, Exception exception, TraceEventType eventType, ApplicationTask task)
        {
            this.Message = message;
            this.Details = details;
            this.Exception = exception;
            this.EventType = eventType;
            this.Task = task;
        }
    }
}