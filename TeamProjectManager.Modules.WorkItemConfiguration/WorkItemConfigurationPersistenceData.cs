using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [DataContract(Namespace = "http://schemas.teamprojectmanager.codeplex.com/workitemtypes/2012/07")]
    public class WorkItemConfigurationPersistenceData
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> WorkItemConfigurationItems { get; set; }

        public WorkItemConfigurationPersistenceData()
        {
        }

        public WorkItemConfigurationPersistenceData(WorkItemConfiguration source)
        {
            this.Name = source.Name;
            this.WorkItemConfigurationItems = source.Items.Select(w => w.XmlDefinition.OuterXml).ToList();
        }
    }
}