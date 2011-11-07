using System.Diagnostics;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Logging;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Shell.Infrastructure
{
    internal class LoggerAdapter : ILoggerFacade
    {
        private Logger logger;
        public IEventAggregator EventAggregator { get; set; }

        public LoggerAdapter(Logger logger)
        {
            this.logger = logger;
            this.logger.LogMessagePublished += OnLogMessagePublished;
        }

        private void OnLogMessagePublished(object sender, LogMessageEventArgs e)
        {
            if (this.EventAggregator != null)
            {
                this.EventAggregator.GetEvent<LogMessagePublishedEvent>().Publish(e.LogMessage);
            }
        }

        public void Log(string message, Category category, Priority priority)
        {
            TraceEventType eventType;
            switch (category)
            {
                case Category.Debug:
                    eventType = TraceEventType.Verbose;
                    break;
                case Category.Exception:
                    eventType = TraceEventType.Error;
                    break;
                case Category.Info:
                    eventType = TraceEventType.Information;
                    break;
                case Category.Warn:
                    eventType = TraceEventType.Warning;
                    break;
                default:
                    eventType = TraceEventType.Information;
                    break;
            }
            this.logger.Log(message, eventType);
        }
    }
}