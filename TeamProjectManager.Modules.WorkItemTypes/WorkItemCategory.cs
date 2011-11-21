using System.Collections.Generic;
using System.Xml.Serialization;

namespace TeamProjectManager.Modules.WorkItemTypes
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
        public List<WorkItemTypeReference> WorkItemTypes { get; set; }

        public WorkItemCategory()
        {
            this.WorkItemTypes = new List<WorkItemTypeReference>();
        }
    }
}