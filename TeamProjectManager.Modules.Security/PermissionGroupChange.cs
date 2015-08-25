using System.Collections.Generic;
using System.Linq;

namespace TeamProjectManager.Modules.Security
{
    public class PermissionGroupChange
    {
        public PermissionGroup PermissionGroup { get; private set; }
        public IList<PermissionChange> PermissionChanges { get; private set; }

        public PermissionGroupChange(PermissionGroup permissionGroup)
        {
            this.PermissionGroup = permissionGroup;
            this.PermissionChanges = permissionGroup.Permissions.Select(p => new PermissionChange(p)).ToArray();
        }

        // This is used for the tab item header in the UI (due to the custom item template on the tab control this is difficult to change).
        public override string ToString()
        {
            return this.PermissionGroup.DisplayName;
        }
    }
}