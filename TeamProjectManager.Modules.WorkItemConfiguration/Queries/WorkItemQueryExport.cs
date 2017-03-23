using System;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Queries
{
    public class WorkItemQueryExport
    {
        public WorkItemQueryInfo Query { get; private set; }
        public string SaveAsFileName { get; private set; }

        public WorkItemQueryExport(WorkItemQueryInfo query, string saveAsFileName)
        {
            this.Query = query;
            this.SaveAsFileName = saveAsFileName;
        }

        public XmlDocument WrapInXmlDocument()
        {
            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", null, null));
            var queryWrapperElement = xml.CreateElement("WorkItemQuery");
            xml.AppendChild(queryWrapperElement);
            queryWrapperElement.SetAttribute("Version", "1");
            var queryElement = xml.CreateElement("Wiql");
            queryWrapperElement.AppendChild(queryElement);
            queryElement.InnerText = this.Query.Text;
            return xml;
        }

        public static string GetQueryTextFromXml(XmlDocument wiql)
        {
            if (wiql == null)
            {
                throw new ArgumentNullException("wiql");
            }
            var wiqlNode = wiql.SelectSingleNode("/WorkItemQuery/Wiql");
            if (wiqlNode == null)
            {
                return null;
            }
            return wiqlNode.InnerText;
        }

        public WorkItemQueryExport Clone()
        {
            return new WorkItemQueryExport(this.Query.Clone(), this.SaveAsFileName);
        }
    }
}