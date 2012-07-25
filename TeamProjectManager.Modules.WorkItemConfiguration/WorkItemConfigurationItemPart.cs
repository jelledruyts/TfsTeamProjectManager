using System.Xml;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfigurationItemPart
    {
        public string Name { get; private set; }
        public XmlElement NormalizedXmlDefinition { get; private set; }

        public WorkItemConfigurationItemPart(string name, XmlElement normalizedXmlDefinition)
        {
            this.Name = name;
            this.NormalizedXmlDefinition = normalizedXmlDefinition;
        }
    }
}