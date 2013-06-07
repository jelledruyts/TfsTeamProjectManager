using System;
using System.Linq;
using System.Runtime.Serialization;

namespace TeamProjectManager.Modules.SourceControl
{
    [DataContract(Name = "Branch", Namespace = BranchInfo.XmlNamespace)]
    public class BranchInfo
    {
        public const string XmlNamespace = "http://schemas.teamprojectmanager.codeplex.com/branchhierarchy/2013/06";

        [IgnoreDataMember]
        public BranchInfo Parent { get; private set; }
        [DataMember(Order = 1)]
        public string Path { get; private set; }
        [DataMember(Order = 2)]
        public string Description { get; private set; }
        [DataMember(Order = 3)]
        public DateTime DateCreated { get; private set; }
        [DataMember(Order = 4)]
        public string Owner { get; private set; }
        [DataMember(Order = 5)]
        public string DirectoryName { get; private set; }
        [DataMember(Order = 6)]
        public string TeamProjectName { get; private set; }
        [IgnoreDataMember]
        public int BranchDepth { get; private set; }
        [IgnoreDataMember]
        public int MaxTreeDepth { get; private set; }
        [IgnoreDataMember]
        public int RecursiveChildCount { get; private set; }

        private BranchInfo[] children;
        [DataMember(Order = 99)]
        public BranchInfo[] Children
        {
            get
            {
                return this.children;
            }
            set
            {
                this.children = value;
                this.RecursiveChildCount = this.Children.Length + this.Children.Sum(c => c.RecursiveChildCount);
                this.MaxTreeDepth = (this.Children.Any() ? this.Children.Max(c => c.MaxTreeDepth) : this.BranchDepth);
            }
        }

        public BranchInfo(BranchInfo parent, string path, string description, DateTime dateCreated, string owner)
        {
            this.Parent = parent;
            this.Path = path;
            this.Description = description;
            this.DateCreated = dateCreated;
            this.Owner = owner;

            this.DirectoryName = this.Path.Substring(this.Path.LastIndexOf('/') + 1);
            var secondSlashIndex = this.Path.IndexOf('/', 2);
            this.TeamProjectName = secondSlashIndex < 0 ? this.DirectoryName : this.Path.Substring(2, secondSlashIndex - 2);
            this.BranchDepth = this.Parent == null ? 1 : this.Parent.BranchDepth + 1;
        }
    }
}