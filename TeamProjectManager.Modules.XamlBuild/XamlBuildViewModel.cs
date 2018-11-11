using Prism.Events;
using System.ComponentModel.Composition;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Modules.XamlBuild.BuildDefinitions;
using TeamProjectManager.Modules.XamlBuild.BuildProcessTemplates;

namespace TeamProjectManager.Modules.XamlBuild
{
    [Export]
    public class XamlBuildViewModel : ViewModelBase
    {
        #region Properties

        [Import]
        public BuildDefinitionsView BuildDefinitionsView { get; set; }

        [Import]
        public BuildProcessTemplatesView BuildProcessTemplatesView { get; set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public XamlBuildViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "XAML Builds", "Allows you to work with XAML build definitions and templates.")
        {
        }

        #endregion

        #region Overrides

        protected override bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return server.MajorVersion >= TfsMajorVersion.V10;
        }

        #endregion
    }
}