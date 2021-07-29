using Microsoft.TeamFoundation.DistributedTask.WebApi;

using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.BuildAndRelease.TaskGroups
{
    public class TaskGroupInfo
    {
        public TaskGroup TaskGroup { get; private set; }
        public TeamProjectInfo TeamProject { get; private set; }

        public TaskGroupInfo(TeamProjectInfo teamProject, TaskGroup taskGroup)
        {
            this.TeamProject = teamProject;
            this.TaskGroup = taskGroup;
        }
    }
}