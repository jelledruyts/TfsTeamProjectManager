using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemCategory
    {
        [XmlAttribute(AttributeName = "refname")]
        public string RefName { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "DEFAULTWORKITEMTYPE", Namespace = "")]
        public WorkItemTypeReference DefaultWorkItemType { get; set; }

        [XmlElement(ElementName = "WORKITEMTYPE", Namespace = "")]
        public ObservableCollection<WorkItemTypeReference> WorkItemTypes { get; set; }

        public WorkItemCategory()
        {
            this.WorkItemTypes = new ObservableCollection<WorkItemTypeReference>();
        }
    }
}