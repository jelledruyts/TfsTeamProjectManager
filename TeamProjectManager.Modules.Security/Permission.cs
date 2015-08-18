
namespace TeamProjectManager.Modules.Security
{
    public class Permission
    {
        public PermissionScope DisplayScope { get; private set; }
        public PermissionScope FunctionalScope { get; private set; }
        public string PermissionConstant { get; private set; }
        public int PermissionId { get; private set; }
        public string DisplayName { get; private set; }

        public Permission(PermissionScope scope, string displayName, string permissionConstant, int permissionId)
            : this(scope, scope, displayName, permissionConstant, permissionId)
        {
        }

        public Permission(PermissionScope displayScope, PermissionScope functionalScope, string displayName, string permissionConstant, int permissionId)
        {
            this.DisplayScope = displayScope;
            this.FunctionalScope = functionalScope;
            this.PermissionConstant = permissionConstant;
            this.DisplayName = displayName;
            this.PermissionId = permissionId;
        }
    }
}