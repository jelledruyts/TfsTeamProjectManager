using Microsoft.TeamFoundation.DistributedTask.WebApi;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.BuildAndRelease.ServiceEndpoints
{
    public class ServiceEndpointInfo
    {
        public TeamProjectInfo TeamProject { get; private set; }
        public ServiceEndpoint ServiceEndpoint { get; private set; }

        public ServiceEndpointInfo(TeamProjectInfo teamProject, ServiceEndpoint serviceEndpoint)
        {
            this.TeamProject = teamProject;
            this.ServiceEndpoint = serviceEndpoint;
        }
    }
}