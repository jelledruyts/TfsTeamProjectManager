using Microsoft.Practices.Prism.Events;
using System.ComponentModel.Composition;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Modules.BuildAndRelease.BuildDefinitions;
using TeamProjectManager.Modules.BuildAndRelease.BuildTemplates;
using TeamProjectManager.Modules.BuildAndRelease.ServiceEndpoints;
using TeamProjectManager.Modules.BuildAndRelease.TaskGroups;

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
        public TaskGroupsView TaskGroupsView { get; set; }

        [Import]
        public ServiceEndpointsView ServiceEndpointsView { get; set; }

        #endregion

        #region Observable Properties

        public Visibility TaskGroupsViewVisibility
        {
            get { return this.GetValue(TaskGroupsViewVisibilityProperty); }
            set { this.SetValue(TaskGroupsViewVisibilityProperty, value); }
        }

        public static readonly ObservableProperty<Visibility> TaskGroupsViewVisibilityProperty = new ObservableProperty<Visibility, BuildAndReleaseViewModel>(o => o.TaskGroupsViewVisibility);

        public Visibility ServiceEndpointsViewVisibility
        {
            get { return this.GetValue(ServiceEndpointsViewVisibilityProperty); }
            set { this.SetValue(ServiceEndpointsViewVisibilityProperty, value); }
        }

        public static readonly ObservableProperty<Visibility> ServiceEndpointsViewVisibilityProperty = new ObservableProperty<Visibility, BuildAndReleaseViewModel>(o => o.ServiceEndpointsViewVisibility);

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected BuildAndReleaseViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Build & Release")
        {
        }

        #endregion

        #region Overrides

        protected override void OnSelectedTeamProjectCollectionChanged()
        {
            // Only TFS 2017 and upwards support the Task Groups and Service Endpoints API.
            this.TaskGroupsViewVisibility = this.SelectedTeamProjectCollection != null && this.SelectedTeamProjectCollection.TeamFoundationServer != null && this.SelectedTeamProjectCollection.TeamFoundationServer.MajorVersion >= TfsMajorVersion.V15 ? Visibility.Visible : Visibility.Collapsed;
            this.ServiceEndpointsViewVisibility = this.TaskGroupsViewVisibility;
        }

        protected override bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return server.MajorVersion >= TfsMajorVersion.V14;
        }

        #endregion
    }
}