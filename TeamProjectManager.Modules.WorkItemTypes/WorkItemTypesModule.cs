using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [ModuleExport(typeof(WorkItemTypesModule))]
    public class WorkItemTypesModule : IModule
    {
        [Import]
        private IRegionViewRegistry RegionViewRegistry { get; set; }

        public void Initialize()
        {
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(WorkItemTypesView));
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(WorkItemCategoriesView));
            this.RegionViewRegistry.RegisterViewWithRegion(RegionNames.Modules, typeof(WorkItemConfigurationView));
        }
    }
}