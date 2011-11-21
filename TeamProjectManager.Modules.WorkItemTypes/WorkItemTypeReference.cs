using System.Xml.Serialization;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeReference
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        public override int GetHashCode()
        {
            return this.Name == null ? 0 : this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var reference = obj as WorkItemTypeReference;
            if (reference != null)
            {
                return reference.Name == this.Name;
            }
            return false;
        }
    }
}