using System.Xml.Serialization;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeReference
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }
}