using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;

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