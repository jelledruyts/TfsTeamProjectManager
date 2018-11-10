using Prism.Modularity;
using Prism.Regions;
using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.BuildAndRelease
{
    [ModuleExport(typeof(BuildAndReleaseModule))]
    public class BuildAndReleaseModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(BuildAndReleaseView));
        }
    }
}