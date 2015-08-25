using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.Security
{
    [DataContract(Namespace = "http://schemas.teamprojectmanager.codeplex.com/permissionchange/2013/04")]
    public class PermissionChangePersistenceData
    {
        #region Properties

        [DataMember]
        public PermissionScope Scope { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public PermissionChangeAction Action { get; set; }

        #endregion

        #region Constructors

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

        #endregion

        #region Static Load & Save

        public static IList<PermissionChangePersistenceData> Load(string fileName)
        {
            return SerializationProvider.Read<PermissionChangePersistenceData[]>(fileName);
        }

        public static void Save(string fileName, IList<PermissionChangePersistenceData> data)
        {
            SerializationProvider.Write<PermissionChangePersistenceData[]>(data.ToArray(), fileName);
        }

        #endregion
    }
}