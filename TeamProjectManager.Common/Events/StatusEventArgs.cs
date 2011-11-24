using System;
using System.Diagnostics;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event arguments for status events.
    /// </summary>
    public class StatusEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the status message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the status details.
        /// </summary>
        public string Details { get; private set; }

        /// <summary>
        /// Gets the type of event.
        /// </summary>
        public TraceEventType EventType { get; private set; }

        /// <summary>
        /// Gets the exception that occurred, if any.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets the application task that can be used to track further progress of the status event.
        /// </summary>
        public ApplicationTask Task { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        public StatusEventArgs(string message)
            : this(message, null, null, TraceEventType.Information, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="details">The status details.</param>
        public StatusEventArgs(string message, string details)
            : this(message, details, null, TraceEventType.Information, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="eventType">The type of event.</param>
        public StatusEventArgs(string message, TraceEventType eventType)
            : this(message, null, null, eventType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="details">The status details.</param>
        /// <param name="eventType">The type of event.</param>
        public StatusEventArgs(string message, string details, TraceEventType eventType)
            : this(message, details, null, eventType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        public StatusEventArgs(string message, Exception exception)
            : this(message, null, exception, TraceEventType.Error, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="details">The status details.</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        public StatusEventArgs(string message, string details, Exception exception)
            : this(message, details, exception, TraceEventType.Error, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        /// <param name="eventType">The type of event.</param>
        public StatusEventArgs(string message, Exception exception, TraceEventType eventType)
            : this(message, null, exception, TraceEventType.Error, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="details">The status details.</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        /// <param name="eventType">The type of event.</param>
        public StatusEventArgs(string message, string details, Exception exception, TraceEventType eventType)
            : this(message, null, exception, eventType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="task">The application task that can be used to track further progress of the status event.</param>
        public StatusEventArgs(ApplicationTask task)
            : this(task.Name, null, null, TraceEventType.Information, task)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="message">The status message.</param>
        /// <param name="details">The status details.</param>
        /// <param name="exception">The exception that occurred, if any.</param>
        /// <param name="eventType">The type of event.</param>
        /// <param name="task">The application task that can be used to track further progress of the status event.</param>
        private StatusEventArgs(string message, string details, Exception exception, TraceEventType eventType, ApplicationTask task)
        {
            this.Message = message;
            this.Details = details;
            this.Exception = exception;
            this.EventType = eventType;
            this.Task = task;
        }

        #endregion
    }
}