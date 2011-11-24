using System;
using System.Diagnostics;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Defines a logger.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message that an exception occurred.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception that occurred.</param>
        void Log(string message, Exception exception);

        /// <summary>
        /// Logs a message that an exception occurred.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="eventType">The type of event.</param>
        void Log(string message, Exception exception, TraceEventType eventType);

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="eventType">The type of event.</param>
        void Log(string message, TraceEventType eventType);

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="eventType">The type of event.</param>
        /// <param name="details">The details.</param>
        void Log(string message, TraceEventType eventType, string details);

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        void Log(LogMessage logMessage);
    }
}