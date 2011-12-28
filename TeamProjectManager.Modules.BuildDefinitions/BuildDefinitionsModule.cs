using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.BuildDefinitions
{
    [ModuleExport(typeof(BuildDefinitionsModule))]
    public class BuildDefinitionsModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(BuildDefinitionsView));
        }
    }
}