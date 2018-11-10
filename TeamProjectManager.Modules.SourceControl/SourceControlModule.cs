using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
using Prism.Modularity;
using Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.SourceControl
{
    [ModuleExport(typeof(SourceControlModule))]
    public class SourceControlModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(SourceControlView));
        }
    }
}