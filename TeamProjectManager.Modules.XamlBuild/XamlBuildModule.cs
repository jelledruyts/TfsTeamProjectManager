using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using System.ComponentModel.Composition;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.XamlBuild
{
    [ModuleExport(typeof(XamlBuildModule))]
    public class XamlBuildModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(XamlBuildView));
        }
    }
}