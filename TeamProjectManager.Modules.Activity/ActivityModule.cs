using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
using Prism.Modularity;
using Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.Activity
{
    [ModuleExport(typeof(ActivityModule))]
    public class ActivityModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(ActivityView));
        }
    }
}