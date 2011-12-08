using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.Security
{
    [ModuleExport(typeof(SecurityModule))]
    public class SecurityModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(SecurityView));
        }
    }
}