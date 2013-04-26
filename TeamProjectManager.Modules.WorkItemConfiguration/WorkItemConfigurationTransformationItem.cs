using System.Runtime.Serialization;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [DataContract(Namespace = "http://schemas.teamprojectmanager.codeplex.com/workitemconfigurationtransform/2013/04")]
    public class WorkItemConfigurationTransformationItem
    {
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public WorkItemConfigurationItemType WorkItemConfigurationItemType { get; set; }

        [DataMember]
        public string WorkItemTypeName { get; set; }

        [DataMember]
        public TransformationType TransformationType { get; set; }

        [DataMember]
        public string TransformationXml { get; set; }

        public WorkItemConfigurationTransformationItem()
        {
        }
    }
}