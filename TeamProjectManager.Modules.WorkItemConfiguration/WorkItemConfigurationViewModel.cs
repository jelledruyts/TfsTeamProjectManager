using Microsoft.Practices.Prism.Events;
using System.ComponentModel.Composition;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public class WorkItemConfigurationViewModel : ViewModelBase
    {
        #region Properties

        [Import]
        public WorkItemTypesView WorkItemTypesView { get; set; }

        [Import]
        public WorkItemQueriesView WorkItemQueriesView { get; set; }

        [Import]
        public WorkItemCategoriesView WorkItemCategoriesView { get; set; }

        [Import]
        public ComparisonView ComparisonView { get; set; }

        [Import]
        public WorkItemProcessConfigurationView WorkItemProcessConfigurationView { get; set; }

        [Import]
        public WorkItemConfigurationTransformationView WorkItemConfigurationTransformationView { get; set; }

        #endregion

        #region Observable Properties

        public Visibility VstsWarningVisibility
        {
            get { return this.GetValue(VstsWarningVisibilityProperty); }
            set { this.SetValue(VstsWarningVisibilityProperty, value); }
        }

        public static readonly ObservableProperty<Visibility> VstsWarningVisibilityProperty = new ObservableProperty<Visibility, WorkItemConfigurationViewModel>(o => o.VstsWarningVisibility, Visibility.Collapsed);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemConfigurationViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Configuration", "Allows you to compare work item configurations.")
        {
        }

        #endregion

        #region Overrides

        protected override void OnSelectedTeamProjectCollectionChanged()
        {
            this.VstsWarningVisibility = this.SelectedTeamProjectCollection.TeamFoundationServer.MajorVersion == TfsMajorVersion.TeamServices ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion
    }
}