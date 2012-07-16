using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeDefinition : WorkItemConfigurationItem
    {
        #region Static Factory Methods

        public static new WorkItemTypeDefinition FromFile(string path)
        {
            return WorkItemConfigurationItem.FromFile<WorkItemTypeDefinition>(path);
        }

        public static new WorkItemTypeDefinition FromXml(string xmlDefinition)
        {
            return WorkItemConfigurationItem.FromXml<WorkItemTypeDefinition>(xmlDefinition);
        }

        public static new WorkItemTypeDefinition FromXml(XmlDocument xmlDefinition)
        {
            return WorkItemConfigurationItem.FromXml<WorkItemTypeDefinition>(xmlDefinition);
        }

        #endregion

        #region Constructors

        public WorkItemTypeDefinition(string name, XmlDocument xmlDefinition)
            : base(WorkItemConfigurationItemType.WorkItemType, name, xmlDefinition)
        {
        }

        #endregion
    }
}