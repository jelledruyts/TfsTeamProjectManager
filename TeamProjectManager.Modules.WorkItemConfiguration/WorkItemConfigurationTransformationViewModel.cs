using Microsoft.Practices.Prism.Events;
using System.ComponentModel.Composition;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    // TODO: Enable transformation "recipes" that match WITD names with transforms as well as Agile/Common Config and work item categories
    [Export]
    public class WorkItemConfigurationTransformationViewModel : ViewModelBase
    {
        #region Constructors

        [ImportingConstructor]
        public WorkItemConfigurationTransformationViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Configuration Transformation", "Allows you to transform the XML files that define the work item configuration (i.e. work item type definitions, work item categories and common and agile process configuration). This can be useful if you want have upgraded Team Foundation Server and want to take advantage of new features.")
        {
        }

        #endregion
    }
}