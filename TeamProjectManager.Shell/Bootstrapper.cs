using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Prism.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Shell.Infrastructure;
using Microsoft.Practices.ServiceLocation;
using Prism.Logging;
using Prism.Mef;
using Prism.Regions;
using TeamProjectManager.Common;

namespace TeamProjectManager.Shell
{
    internal class Bootstrapper : MefBootstrapper
    {
        private Logger logger;

        public Bootstrapper(Logger logger)
        {
            this.logger = logger;
        }

        protected override ILoggerFacade CreateLogger()
        {
            return new LoggerAdapter(this.logger);
        }

        protected override void ConfigureAggregateCatalog()
        {
            base.ConfigureAggregateCatalog();
            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Shell).Assembly));
            this.AggregateCatalog.Catalogs.Add(new DirectoryCatalog("."));
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            this.Container.ComposeExportedValue<ILogger>(this.logger);
            ((LoggerAdapter)this.Logger).EventAggregator = this.Container.GetExportedValue<IEventAggregator>();
        }

        protected override DependencyObject CreateShell()
        {
            return this.Container.GetExportedValue<Shell>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();
            App.Current.MainWindow = (Window)this.Shell;
            App.Current.MainWindow.Show();

            // Ensure that the first tab is selected upon startup.
            var regionManager = ServiceLocator.Current.GetInstance<IRegionManager>();
            var region = regionManager.Regions[RegionNames.Modules];
            region.Views.CollectionChanged += (sender, e) =>
            {
                if (region.Views.Any())
                {
                    region.Activate(region.Views.First());
                }
            };
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            foreach (var module in this.ModuleCatalog.Modules)
            {
                this.logger.Log("Initialized module: " + module.ModuleType, TraceEventType.Verbose);
            }
        }
    }
}