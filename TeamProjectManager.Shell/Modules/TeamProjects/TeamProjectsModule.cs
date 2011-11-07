using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Shell.Modules.TeamProjects
{
    [ModuleExport(typeof(TeamProjectsModule))]
    public class TeamProjectsModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.TeamProjects, typeof(TeamProjectsView));
        }
    }
}