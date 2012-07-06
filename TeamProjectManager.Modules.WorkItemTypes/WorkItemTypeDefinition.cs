using System;
using System.IO;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeDefinition
    {
        #region Properties

        public string Name { get; private set; }
        public XmlDocument XmlDefinition { get; set; }

        #endregion

        #region Constructors

        public WorkItemTypeDefinition(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var xmlDefinition = new XmlDocument();
                    xmlDefinition.Load(path);
                    Initialize(xmlDefinition);
                }
                catch
                {
                    // Ignore exceptions from XML parsing.
                }
            }
            if (string.IsNullOrEmpty(this.Name))
            {
                this.Name = new FileInfo(path).Name;
            }
        }

        public WorkItemTypeDefinition(XmlDocument xmlDefinition)
        {
            Initialize(xmlDefinition);
        }

        private void Initialize(XmlDocument xmlDefinition)
        {
            this.XmlDefinition = xmlDefinition;
            var nameNode = this.XmlDefinition.SelectSingleNode("//WORKITEMTYPE/@name");
            this.Name = nameNode == null ? null : nameNode.Value;
        }

        #endregion
    }
}