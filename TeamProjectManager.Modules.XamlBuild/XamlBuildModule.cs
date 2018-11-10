using Prism.Modularity;
using Prism.Regions;
using System.ComponentModel.Composition;
using Prism.Mef.Modularity;
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