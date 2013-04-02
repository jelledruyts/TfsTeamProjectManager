
namespace TeamProjectManager.Modules.Security
{
    public class Permission
    {
        public PermissionScope Scope { get; private set; }
        public string PermissionConstant { get; private set; }
        public int PermissionId { get; private set; }
        public string DisplayName { get; private set; }

        public Permission(PermissionScope scope, string displayName, string permissionConstant, int permissionId)
        {
            this.Scope = scope;
            this.PermissionConstant = permissionConstant;
            this.DisplayName = displayName;
            this.PermissionId = permissionId;
        }
    }
}