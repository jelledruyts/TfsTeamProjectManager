using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfigurationItem
    {
        #region Constants

        public const string WorkItemTypeDefinitionName = "Work Item Type Definition";
        public const string CategoriesName = "Work Item Categories";

        #endregion

        #region Static Factory Methods

        public static T FromFile<T>(string path) where T : WorkItemConfigurationItem
        {
            var item = FromFile(path) as T;
            if (item == null)
            {
                throw new ArgumentException("The work item configuration item was not of the right type: " + typeof(T).Name);
            }
            return item;
        }

        public static T FromXml<T>(string xmlDefinition) where T : WorkItemConfigurationItem
        {
            var item = FromXml(xmlDefinition) as T;
            if (item == null)
            {
                throw new ArgumentException("The work item configuration item was not of the right type: " + typeof(T).Name);
            }
            return item;
        }

        public static T FromXml<T>(XmlDocument xmlDefinition) where T : WorkItemConfigurationItem
        {
            var item = FromXml(xmlDefinition) as T;
            if (item == null)
            {
                throw new ArgumentException("The work item configuration item was not of the right type: " + typeof(T).Name);
            }
            return item;
        }

        public static WorkItemConfigurationItem FromFile(string path)
        {
            var xmlDefinition = new XmlDocument();
            xmlDefinition.Load(path);
            return FromXml(xmlDefinition);
        }

        public static WorkItemConfigurationItem FromXml(string xmlDefinition)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlDefinition);
            return FromXml(xmlDocument);
        }

        public static WorkItemConfigurationItem FromXml(XmlDocument xmlDefinition)
        {
            if (xmlDefinition == null)
            {
                throw new ArgumentNullException("xmlDefinition");
            }
            WorkItemConfigurationItemType type;
            string name;
            var typeName = xmlDefinition.DocumentElement.LocalName;
            if (typeName.Equals("WITD", StringComparison.OrdinalIgnoreCase))
            {
                type = WorkItemConfigurationItemType.WorkItemType;
                var nameNode = xmlDefinition.SelectSingleNode("//WORKITEMTYPE/@name");
                name = nameNode == null ? null : nameNode.Value;
            }
            else if (typeName.Equals("CATEGORIES", StringComparison.OrdinalIgnoreCase))
            {
                type = WorkItemConfigurationItemType.Categories;
                name = CategoriesName;
            }
            else
            {
                throw new ArgumentException("The work item configuration item's type could not be determined from the XML definition.");
            }
            if (type == WorkItemConfigurationItemType.WorkItemType)
            {
                return new WorkItemTypeDefinition(name, xmlDefinition);
            }
            else
            {
                return new WorkItemConfigurationItem(type, name, xmlDefinition);
            }
        }

        #endregion

        #region Properties

        public WorkItemConfigurationItemType Type { get; private set; }
        public string Name { get; private set; }
        public XmlDocument XmlDefinition { get; set; }

        #endregion

        #region Constructors

        public WorkItemConfigurationItem(WorkItemConfigurationItemType type, string name, XmlDocument xmlDefinition)
        {
            this.Type = type;
            this.Name = name;
            this.XmlDefinition = xmlDefinition;
        }

        #endregion

        #region GetParts

        public IList<WorkItemConfigurationItemPart> GetParts(XmlDocument normalizedXmlDefinition)
        {
            switch (this.Type)
            {
                case WorkItemConfigurationItemType.WorkItemType:
                    return Enum.GetValues(typeof(WorkItemTypeDefinitionPart)).Cast<WorkItemTypeDefinitionPart>().Select(p => GetPart(normalizedXmlDefinition, p)).ToList();
                case WorkItemConfigurationItemType.Categories:
                    return new WorkItemConfigurationItemPart[] { new WorkItemConfigurationItemPart(CategoriesName, normalizedXmlDefinition.DocumentElement) };
                default:
                    throw new InvalidOperationException("The work item configuration item type does not have any parts defined.");
            }
        }

        private static WorkItemConfigurationItemPart GetPart(XmlDocument xmlDefinition, WorkItemTypeDefinitionPart part)
        {
            var xpath = "//WORKITEMTYPE/" + part.ToString().ToUpperInvariant();
            return new WorkItemConfigurationItemPart(part.ToString(), (XmlElement)xmlDefinition.SelectSingleNode(xpath));
        }

        #endregion
    }
}