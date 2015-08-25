using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TeamProjectManager.Modules.Security
{
    public class SecurityGroupChange
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IList<PermissionGroup> PermissionGroups { get; private set; }
        public ObservableCollection<PermissionGroupChange> PermissionGroupChanges { get; private set; }
        public string UsersToAdd { get; set; }
        public string UsersToRemove { get; set; }
        public bool RemoveAllUsers { get; set; }

        public SecurityGroupChange()
        {
            this.PermissionGroupChanges = new ObservableCollection<PermissionGroupChange>();
        }

        public void SetPermissionGroups(IList<PermissionGroup> permissionGroups)
        {
            this.PermissionGroups = permissionGroups;
            ResetPermissionChanges();
        }

        public void ResetPermissionChanges()
        {
            this.PermissionGroupChanges.Clear();
            if (this.PermissionGroups != null)
            {
                foreach (var permissionGroup in this.PermissionGroups)
                {
                    this.PermissionGroupChanges.Add(new PermissionGroupChange(permissionGroup));
                }
            }
        }
    }
}