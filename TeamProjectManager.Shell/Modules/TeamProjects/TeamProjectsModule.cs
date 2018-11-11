using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
using Prism.Modularity;
using Prism.Regions;

namespace TeamProjectManager.Shell.Modules.TeamProjects
{
    [ModuleExport(typeof(TeamProjectsModule))]
    public class TeamProjectsModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(InternalConstants.RegionNameTeamProjects, typeof(TeamProjectsView));
        }
    }
}