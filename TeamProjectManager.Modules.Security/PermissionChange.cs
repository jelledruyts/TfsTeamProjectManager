using System;

namespace TeamProjectManager.Modules.Security
{
    public class PermissionChange
    {
        public Permission Permission { get; private set; }
        public PermissionChangeAction Action { get; set; }

        public PermissionChange(Permission permission)
        {
            if (permission == null)
            {
                throw new ArgumentNullException("permission");
            }
            this.Permission = permission;
            this.Action = PermissionChangeAction.None;
        }
    }
}