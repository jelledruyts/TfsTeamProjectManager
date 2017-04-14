using Microsoft.Practices.Prism.Events;
using System.ComponentModel.Composition;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Modules.BuildAndRelease.BuildDefinitions;
using TeamProjectManager.Modules.BuildAndRelease.BuildTemplates;
using TeamProjectManager.Modules.BuildAndRelease.ServiceEndpoints;

namespace TeamProjectManager.Modules.BuildAndRelease
{
    [Export]
    public class BuildAndReleaseViewModel : ViewModelBase
    {
        #region Properties

        [Import]
        public BuildDefinitionsView BuildDefinitionsView { get; set; }

        [Import]
        public BuildTemplatesView BuildTemplatesView { get; set; }

        [Import]
        public ServiceEndpointsView ServiceEndpointsView { get; set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected BuildAndReleaseViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Build & Release")
        {
        }

        #endregion

        #region Overrides

        protected override bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return server.MajorVersion >= TfsMajorVersion.V14;
        }

        #endregion
    }
}