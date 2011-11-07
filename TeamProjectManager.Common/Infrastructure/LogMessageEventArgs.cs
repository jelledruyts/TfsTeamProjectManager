using System;

namespace TeamProjectManager.Common.Infrastructure
{
    public class LogMessageEventArgs : EventArgs
    {
        public LogMessage LogMessage { get; private set; }

        public LogMessageEventArgs(LogMessage logMessage)
        {
            this.LogMessage = logMessage;
        }
    }
}