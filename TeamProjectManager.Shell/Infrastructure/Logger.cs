using System;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Shell.Infrastructure
{
    [Export(typeof(ILogger))]
    internal class Logger : ILogger
    {
        #region Fields

        /// <summary>
        /// The trace source to log to.
        /// </summary>
        private TraceSource tracer;

        /// <summary>
        /// The synchronisation object to lock on.
        /// </summary>
        private object lockObject;

        #endregion

        #region Properties

        public string LogFilePath { get; private set; }

        #endregion

        #region Constructors

        public Logger()
        {
            lockObject = new object();
            tracer = new TraceSource("TeamProjectManager");
            var logFileOverride = ConfigurationManager.AppSettings["LogFilePath"];
            if (!string.IsNullOrEmpty(logFileOverride))
            {
                this.LogFilePath = logFileOverride;
            }
            else
            {
                this.LogFilePath = Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, "TeamProjectManager.log");
            }
            tracer.Listeners.Add(new TextWriterTraceListener(this.LogFilePath));
        }

        #endregion

        #region Events

        public event EventHandler<LogMessageEventArgs> LogMessagePublished;

        private void OnLogMessagePublished(LogMessageEventArgs e)
        {
            if (LogMessagePublished != null)
            {
                LogMessagePublished(null, e);
            }
        }

        #endregion

        #region Log

        public void Log(string message, Exception exception)
        {
            Log(message, exception, TraceEventType.Error);
        }

        public void Log(string message, Exception exception, TraceEventType eventType)
        {
            var errorMessage = message;
            string details = null;
            if (exception != null)
            {
                errorMessage += string.Format(CultureInfo.CurrentCulture, ": {0}", exception.Message);
                details = exception.ToString();
            }
            Log(errorMessage, eventType, details);
        }

        public void Log(string message, TraceEventType eventType)
        {
            Log(message, eventType, null);
        }

        public void Log(string message, TraceEventType eventType, string details)
        {
            Log(new LogMessage(message, details, eventType));
        }

        public void Log(LogMessage logMessage)
        {
            if (logMessage != null)
            {
                lock (this.lockObject)
                {
                    var messageDetails = (string.IsNullOrEmpty(logMessage.Details) ? null : Environment.NewLine + logMessage.Details);
                    var fullMessage = string.Format(CultureInfo.CurrentCulture, "{0}[T{1:00}] [{2}] {3}{4}", new string(' ', 11 - logMessage.EventType.ToString().Length), Thread.CurrentThread.ManagedThreadId, DateTime.Now, logMessage.Message, messageDetails);
                    tracer.TraceEvent(logMessage.EventType, 0, fullMessage);
                    tracer.Flush();
                    OnLogMessagePublished(new LogMessageEventArgs(logMessage));
                }
            }
        }

        #endregion
    }
}