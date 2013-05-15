using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfigurationItem
    {
        #region Constants

        public const string AgileConfigurationName = "Agile Configuration";
        public const string CommonConfigurationName = "Common Configuration";
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
            var typeName = xmlDefinition.DocumentElement.LocalName;
            if (typeName.Equals("WITD", StringComparison.OrdinalIgnoreCase))
            {
                var nameNode = xmlDefinition.SelectSingleNode("//WORKITEMTYPE/@name");
                var name = nameNode == null ? null : nameNode.Value;
                return new WorkItemTypeDefinition(name, xmlDefinition);
            }
            else if (typeName.Equals("AgileProjectConfiguration", StringComparison.OrdinalIgnoreCase))
            {
                return new WorkItemConfigurationItem(WorkItemConfigurationItemType.AgileConfiguration, xmlDefinition);
            }
            else if (typeName.Equals("CommonProjectConfiguration", StringComparison.OrdinalIgnoreCase))
            {
                return new WorkItemConfigurationItem(WorkItemConfigurationItemType.CommonConfiguration, xmlDefinition);
            }
            else if (typeName.Equals("CATEGORIES", StringComparison.OrdinalIgnoreCase))
            {
                return new WorkItemConfigurationItem(WorkItemConfigurationItemType.Categories, xmlDefinition);
            }
            else
            {
                throw new ArgumentException("The work item configuration item's type could not be determined from the XML definition.");
            }
        }

        #endregion

        #region Static GetDisplayName Helper

        public static string GetDisplayName(WorkItemConfigurationItemType type)
        {
            return GetDisplayName(type, null);
        }

        public static string GetDisplayName(WorkItemConfigurationItemType type, string workItemTypeName)
        {
            switch (type)
            {
                case WorkItemConfigurationItemType.AgileConfiguration:
                    return WorkItemConfigurationItem.AgileConfigurationName;
                case WorkItemConfigurationItemType.CommonConfiguration:
                    return WorkItemConfigurationItem.CommonConfigurationName;
                case WorkItemConfigurationItemType.WorkItemType:
                    if (!string.IsNullOrEmpty(workItemTypeName))
                    {
                        return "{0}: {1}".FormatCurrent(WorkItemConfigurationItem.WorkItemTypeDefinitionName, workItemTypeName);
                    }
                    else
                    {
                        return WorkItemConfigurationItem.WorkItemTypeDefinitionName;
                    }
                case WorkItemConfigurationItemType.Categories:
                    return WorkItemConfigurationItem.CategoriesName;
                default:
                    return type.ToString();
            }
        }

        #endregion

        #region Properties

        public WorkItemConfigurationItemType Type { get; private set; }
        public string Name { get; private set; }
        public XmlDocument XmlDefinition { get; set; }

        #endregion

        #region Constructors

        public WorkItemConfigurationItem(WorkItemConfigurationItemType type, XmlDocument xmlDefinition)
            : this(type, GetDisplayName(type), xmlDefinition)
        {
        }

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
                case WorkItemConfigurationItemType.CommonConfiguration:
                    return Enum.GetValues(typeof(CommonProjectConfigurationPart)).Cast<CommonProjectConfigurationPart>().Select(p => GetPart(normalizedXmlDefinition, p)).ToList();
                case WorkItemConfigurationItemType.AgileConfiguration:
                    return Enum.GetValues(typeof(AgileProjectConfigurationPart)).Cast<AgileProjectConfigurationPart>().Select(p => GetPart(normalizedXmlDefinition, p)).ToList();
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

        private static WorkItemConfigurationItemPart GetPart(XmlDocument xmlDefinition, AgileProjectConfigurationPart part)
        {
            var xpath = "/AgileProjectConfiguration/" + part.ToString();
            return new WorkItemConfigurationItemPart(part.ToString(), (XmlElement)xmlDefinition.SelectSingleNode(xpath));
        }

        private static WorkItemConfigurationItemPart GetPart(XmlDocument xmlDefinition, CommonProjectConfigurationPart part)
        {
            var xpath = "/CommonProjectConfiguration/" + part.ToString();
            return new WorkItemConfigurationItemPart(part.ToString(), (XmlElement)xmlDefinition.SelectSingleNode(xpath));
        }

        #endregion

        #region Clone

        public virtual WorkItemConfigurationItem Clone()
        {
            return new WorkItemConfigurationItem(this.Type, this.Name, (XmlDocument)this.XmlDefinition.Clone());
        }

        #endregion
    }
}