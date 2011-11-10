using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;

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