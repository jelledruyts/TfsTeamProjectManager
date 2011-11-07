using System;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TeamProjectManager.Modules.LastChangesets
{
    public class ChangesetInfo
    {
        public string TeamProject { get; private set; }
        public Changeset Changeset { get; private set; }
        public int? Id { get; private set; }
        public string Committer { get; private set; }
        public DateTime? CreationTime { get; private set; }
        public string Comment { get; private set; }

        public ChangesetInfo(string teamProject, int? id, string committer, DateTime? creationTime, string comment)
        {
            this.Id = id;
            this.TeamProject = teamProject;
            this.Committer = committer;
            this.CreationTime = creationTime;
            this.Comment = comment;
        }

        public ChangesetInfo(string teamProject, Changeset changeset)
            : this(teamProject, changeset.ChangesetId, changeset.CommitterDisplayName, changeset.CreationDate, changeset.Comment)
        {
            this.Changeset = changeset;
        }
    }
}