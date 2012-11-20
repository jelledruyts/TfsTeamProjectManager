using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public class WorkItemConfigurationViewModel : ViewModelBase
    {
        #region Properties

        [Import]
        public WorkItemTypesView WorkItemTypesView { get; set; }

        [Import]
        public WorkItemCategoriesView WorkItemCategoriesView { get; set; }

        [Import]
        public ComparisonView ComparisonView { get; set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemConfigurationViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Configuration", "Allows you to compare work item configurations.")
        {
        }

        #endregion
    }
}