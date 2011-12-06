using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [DataContract(Namespace = "http://schemas.teamprojectmanager.codeplex.com/workitemtypes/2011/12")]
    public class PersistedComparisonSource
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> WorkItemTypeDefinitions { get; set; }

        public PersistedComparisonSource()
        {
        }

        public PersistedComparisonSource(ComparisonSource source)
        {
            this.Name = source.Name;
            this.WorkItemTypeDefinitions = source.WorkItemTypes.Select(w => w.XmlDefinition.OuterXml).ToList();
        }
    }
}