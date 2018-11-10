using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
using Prism.Modularity;
using Prism.Regions;

namespace TeamProjectManager.Shell.Modules.StatusPanel
{
    [ModuleExport(typeof(StatusPanelModule))]
    public class StatusPanelModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(InternalConstants.RegionNameStatusPanel, typeof(StatusPanelView));
        }
    }
}