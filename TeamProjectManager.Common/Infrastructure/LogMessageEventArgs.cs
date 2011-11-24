using System;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Defines event arguments for log message events.
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the log message.
        /// </summary>
        public LogMessage LogMessage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessageEventArgs"/> class.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        public LogMessageEventArgs(LogMessage logMessage)
        {
            this.LogMessage = logMessage;
        }
    }
}