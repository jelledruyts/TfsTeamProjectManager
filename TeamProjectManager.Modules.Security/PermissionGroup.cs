using System.Collections.Generic;

namespace TeamProjectManager.Modules.Security
{
    public class PermissionGroup
    {
        public PermissionScope Scope { get; private set; }
        public string DisplayName { get; private set; }
        public IList<Permission> Permissions { get; private set; }

        public PermissionGroup(PermissionScope scope, string displayName, IList<Permission> permissions)
        {
            this.Scope = scope;
            this.DisplayName = displayName;
            this.Permissions = permissions;
        }
    }
}