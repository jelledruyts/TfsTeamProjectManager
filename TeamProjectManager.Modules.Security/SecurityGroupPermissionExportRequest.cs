namespace TeamProjectManager.Modules.Security
{
    public class SecurityGroupPermissionExportRequest
    {
        public SecurityGroupInfo SecurityGroup { get; private set; }
        public string FileName { get; private set; }

        public SecurityGroupPermissionExportRequest(SecurityGroupInfo securityGroup, string fileName)
        {
            this.SecurityGroup = securityGroup;
            this.FileName = fileName;
        }
    }
}