using System;
using System.IO;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeDefinition
    {
        #region Properties

        public string Name { get; private set; }
        public XmlDocument XmlDefinition { get; private set; }

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

        #region GetPart

        public XmlElement GetPart(WorkItemTypeDefinitionPart part)
        {
            string xpath;
            switch (part)
            {
                case WorkItemTypeDefinitionPart.Description:
                    xpath = "//WORKITEMTYPE/DESCRIPTION";
                    break;
                case WorkItemTypeDefinitionPart.Fields:
                    xpath = "//WORKITEMTYPE/FIELDS";
                    break;
                case WorkItemTypeDefinitionPart.Workflow:
                    xpath = "//WORKITEMTYPE/WORKFLOW";
                    break;
                case WorkItemTypeDefinitionPart.Form:
                    xpath = "//WORKITEMTYPE/FORM";
                    break;
                default:
                    throw new ArgumentException("The requested part is invalid: " + part.ToString());
            }
            return (XmlElement)this.XmlDefinition.SelectSingleNode(xpath);
        }

        #endregion
    }
}