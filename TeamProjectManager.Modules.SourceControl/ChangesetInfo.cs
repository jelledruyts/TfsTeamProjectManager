using System;

namespace TeamProjectManager.Modules.SourceControl
{
    public class ChangesetInfo
    {
        public string TeamProject { get; private set; }
        public int Id { get; private set; }
        public string Committer { get; private set; }
        public DateTime CreationTime { get; private set; }
        public string Comment { get; private set; }

        public ChangesetInfo(string teamProject, int id, string committer, DateTime creationTime, string comment)
        {
            this.Id = id;
            this.TeamProject = teamProject;
            this.Committer = committer;
            this.CreationTime = creationTime;
            this.Comment = comment;
        }
    }
}