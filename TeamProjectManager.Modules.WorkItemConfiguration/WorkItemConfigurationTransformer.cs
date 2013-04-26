using Microsoft.Web.XmlTransform;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public static class WorkItemConfigurationTransformer
    {
        internal class Logger : IXmlTransformationLogger
        {
            #region IXmlTransformationLogger Implementation

            public void StartSection(string message, params object[] messageArgs)
            {
                StartSection(MessageType.Normal, message, messageArgs);
            }

            public void StartSection(MessageType type, string message, params object[] messageArgs)
            {
                Log(type == MessageType.Normal ? TraceEventType.Information : TraceEventType.Verbose, "[Start] " + string.Format(CultureInfo.CurrentCulture, message, messageArgs));
            }

            public void EndSection(string message, params object[] messageArgs)
            {
                EndSection(MessageType.Normal, message, messageArgs);
            }

            public void EndSection(MessageType type, string message, params object[] messageArgs)
            {
                Log(type == MessageType.Normal ? TraceEventType.Information : TraceEventType.Verbose, "[End] " + string.Format(CultureInfo.CurrentCulture, message, messageArgs));
            }

            public void LogError(string message, params object[] messageArgs)
            {
                LogError(null, -1, -1, message, messageArgs);
            }

            public void LogError(string file, string message, params object[] messageArgs)
            {
                LogError(file, -1, -1, message, messageArgs);
            }

            public void LogError(string file, int lineNumber, int linePosition, string message, params object[] messageArgs)
            {
                Log(TraceEventType.Error, GetLogMessageForFile(file, lineNumber, linePosition, message, messageArgs));
            }

            public void LogErrorFromException(Exception ex)
            {
                LogErrorFromException(ex, null, -1, -1);
            }

            public void LogErrorFromException(Exception ex, string file)
            {
                LogErrorFromException(ex, file, -1, -1);
            }

            public void LogErrorFromException(Exception ex, string file, int lineNumber, int linePosition)
            {
                Log(TraceEventType.Error, GetLogMessageForFile(file, lineNumber, linePosition, ex.ToString()));
            }

            public void LogMessage(string message, params object[] messageArgs)
            {
                LogMessage(MessageType.Normal, message, messageArgs);
            }

            public void LogMessage(MessageType type, string message, params object[] messageArgs)
            {
                Log(type == MessageType.Normal ? TraceEventType.Information : TraceEventType.Verbose, string.Format(CultureInfo.CurrentCulture, message, messageArgs));
            }

            public void LogWarning(string message, params object[] messageArgs)
            {
                LogWarning(null, -1, -1, message, messageArgs);
            }

            public void LogWarning(string file, string message, params object[] messageArgs)
            {
                LogWarning(file, -1, -1, message, messageArgs);
            }

            public void LogWarning(string file, int lineNumber, int linePosition, string message, params object[] messageArgs)
            {
                Log(TraceEventType.Warning, GetLogMessageForFile(file, lineNumber, linePosition, message, messageArgs));
            }

            #endregion

            #region Logging

            private void Log(TraceEventType type, string message)
            {
                // We don't actually log anything at the moment since the transformation logging is probably not too interesting.
            }

            #endregion

            #region Helper Methods

            private static string GetLogMessageForFile(string file, int lineNumber, int linePosition, string message, params object[] messageArgs)
            {
                var logMessage = new StringBuilder();
                if (!string.IsNullOrEmpty(file))
                {
                    logMessage.Append(file);
                    if (lineNumber >= 0)
                    {
                        logMessage.AppendFormat(" ({0}", lineNumber);
                        if (linePosition >= 0)
                        {
                            logMessage.AppendFormat(",{0}", linePosition);
                        }
                        logMessage.Append(")");
                    }
                    logMessage.Append(": ");
                }
                logMessage.AppendFormat(message, messageArgs);
                return logMessage.ToString();
            }

            #endregion
        }

        #region Transform

        public static string Transform(TransformationType type, string sourceXml, string transformXml)
        {
            switch (type)
            {
                case TransformationType.Xdt:
                    return TransformXdt(sourceXml, transformXml);
                case TransformationType.Xslt:
                    return TransformXslt(sourceXml, transformXml);
                default:
                    throw new ArgumentException("The transformation type is unknown: " + type.ToString());
            }
        }

        public static string TransformXdt(string sourceXml, string transformXml)
        {
            var source = new XmlDocument();
            source.LoadXml(sourceXml);
            var transformation = new XmlTransformation(transformXml, false, new Logger());
            var succeeded = transformation.Apply(source);
            var output = new StringBuilder();
            using (var writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true }))
            {
                source.Save(writer);
            }
            return output.ToString();
        }

        public static string TransformXslt(string sourceXml, string transformXml)
        {
            var source = new XmlDocument();
            source.LoadXml(sourceXml);
            var transform = new XmlDocument();
            transform.LoadXml(transformXml);
            var transformation = new XslCompiledTransform();
            transformation.Load(transform);
            var output = new StringBuilder();
            using (var writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true }))
            {
                transformation.Transform(source, writer);
            }
            return output.ToString();
        }

        #endregion
    }
}