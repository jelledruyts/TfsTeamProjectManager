using System.Collections.Generic;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.Security
{
    public class SecurityGroupInfo
    {
        public TeamProjectInfo TeamProject { get; private set; }
        public string Sid { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ICollection<string> Members { get; private set; }
        public string MembersList { get; private set; }

        public SecurityGroupInfo(TeamProjectInfo teamProject, string sid, string name, string description, ICollection<string> members)
        {
            this.TeamProject = teamProject;
            this.Sid = sid;
            this.Name = name;
            this.Description = description;
            this.Members = members ?? new string[0];
            this.MembersList = string.Join("; ", this.Members);
        }
    }
}