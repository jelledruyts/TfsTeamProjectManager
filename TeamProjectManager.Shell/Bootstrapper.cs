using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions;
using TeamProjectManager.Shell.Infrastructure;

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