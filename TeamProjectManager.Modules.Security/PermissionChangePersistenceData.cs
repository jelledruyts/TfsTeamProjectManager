using System.Runtime.Serialization;

namespace TeamProjectManager.Modules.Security
{
    [DataContract(Namespace = "http://schemas.teamprojectmanager.codeplex.com/permissionchange/2013/04")]
    public class PermissionChangePersistenceData
    {
        [DataMember]
        public PermissionScope Scope { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public PermissionChangeAction Action { get; set; }

        public PermissionChangePersistenceData()
        {
        }

        public PermissionChangePersistenceData(PermissionChange permissionChange)
            : this(permissionChange.Permission.Scope, permissionChange.Permission.PermissionConstant, permissionChange.Action)
        {
        }

        public PermissionChangePersistenceData(PermissionScope scope, string name, PermissionChangeAction action)
        {
            this.Scope = scope;
            this.Name = name;
            this.Action = action;
        }
    }
}