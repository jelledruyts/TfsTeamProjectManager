using System.Collections.Generic;
using System.Linq;

namespace TeamProjectManager.Modules.Security
{
    public class SecurityGroupChange
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IList<PermissionChange> PermissionChanges { get; private set; }
        public string UsersToAdd { get; set; }
        public string UsersToRemove { get; set; }
        public bool RemoveAllUsers { get; set; }
        public IList<PermissionChange> TeamProjectPermissions { get; private set; }
        public IList<PermissionChange> TeamBuildPermissions { get; private set; }
        public IList<PermissionChange> WorkItemAreasPermissions { get; private set; }
        public IList<PermissionChange> WorkItemIterationsPermissions { get; private set; }
        public IList<PermissionChange> SourceControlPermissions { get; private set; }
        public IList<PermissionChange> TaggingPermissions { get; private set; }

        public SecurityGroupChange()
        {
            this.PermissionChanges = SecurityManager.Permissions.Select(p => new PermissionChange(p)).ToList();
            this.TeamProjectPermissions = this.PermissionChanges.Where(p => p.Permission.DisplayScope == PermissionScope.TeamProject).ToList();
            this.TeamBuildPermissions = this.PermissionChanges.Where(p => p.Permission.DisplayScope == PermissionScope.TeamBuild).ToList();
            this.WorkItemAreasPermissions = this.PermissionChanges.Where(p => p.Permission.DisplayScope == PermissionScope.WorkItemAreas).ToList();
            this.WorkItemIterationsPermissions = this.PermissionChanges.Where(p => p.Permission.DisplayScope == PermissionScope.WorkItemIterations).ToList();
            this.SourceControlPermissions = this.PermissionChanges.Where(p => p.Permission.DisplayScope == PermissionScope.SourceControl).ToList();
            this.TaggingPermissions = this.PermissionChanges.Where(p => p.Permission.DisplayScope == PermissionScope.Tagging).ToList();
        }

        public void ResetPermissionChanges()
        {
            foreach (var permissionChange in this.PermissionChanges)
            {
                permissionChange.Action = PermissionChangeAction.None;
            }
        }

        public IList<PermissionChange> GetFunctionalChanges(PermissionScope scope)
        {
            return this.PermissionChanges.Where(p => p.Permission.FunctionalScope == scope).ToArray();
        }
    }
}