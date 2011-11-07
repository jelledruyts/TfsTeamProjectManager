using System;
using System.Diagnostics;

namespace TeamProjectManager.Common.Infrastructure
{
    public interface ILogger
    {
        void Log(string message, Exception exception);
        void Log(string message, Exception exception, TraceEventType eventType);
        void Log(string message, TraceEventType eventType);
        void Log(string message, TraceEventType eventType, string details);
        void Log(LogMessage logMessage);
    }
}