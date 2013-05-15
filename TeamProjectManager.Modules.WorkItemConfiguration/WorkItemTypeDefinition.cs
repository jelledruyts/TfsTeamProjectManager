using System.Xml;

namespace TeamProjectManager.Modules.WorkItemConfiguration
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

        #region Clone

        public override WorkItemConfigurationItem Clone()
        {
            return new WorkItemTypeDefinition(this.Name, (XmlDocument)this.XmlDefinition.Clone());
        }

        #endregion
    }
}