using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
using Prism.Modularity;
using Prism.Regions;

namespace TeamProjectManager.Shell.Modules.Logo
{
    [ModuleExport(typeof(LogoModule))]
    public class LogoModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(InternalConstants.RegionNameLogo, typeof(LogoView));
        }
    }
}