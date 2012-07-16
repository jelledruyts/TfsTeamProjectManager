using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemConfigurationItemComparisonResult
    {
        #region Properties

        public string ItemName { get; private set; }
        public WorkItemConfigurationItemType Type { get; private set; }
        public XmlDocument NormalizedSourceDefinition { get; private set; }
        public XmlDocument NormalizedTargetDefinition { get; private set; }
        public ComparisonStatus Status { get; private set; }
        public ICollection<WorkItemConfigurationItemPartComparisonResult> PartResults { get; private set; }
        public double PercentMatch { get; private set; }

        #endregion

        #region Constructors

        public WorkItemConfigurationItemComparisonResult(XmlDocument normalizedSourceDefinition, XmlDocument normalizedTargetDefinition, string itemName, WorkItemConfigurationItemType type, ComparisonStatus status)
            : this(normalizedSourceDefinition, normalizedTargetDefinition, itemName, type, null, status)
        {
        }

        public WorkItemConfigurationItemComparisonResult(XmlDocument normalizedSourceDefinition, XmlDocument normalizedTargetDefinition, string itemName, WorkItemConfigurationItemType type, ICollection<WorkItemConfigurationItemPartComparisonResult> partResults)
            : this(normalizedSourceDefinition, normalizedTargetDefinition, itemName, type, partResults, null)
        {
        }

        private WorkItemConfigurationItemComparisonResult(XmlDocument normalizedSourceDefinition, XmlDocument normalizedTargetDefinition, string itemName, WorkItemConfigurationItemType type, ICollection<WorkItemConfigurationItemPartComparisonResult> partResults, ComparisonStatus? status)
        {
            this.NormalizedSourceDefinition = normalizedSourceDefinition;
            this.NormalizedTargetDefinition = normalizedTargetDefinition;
            this.ItemName = itemName;
            this.Type = type;
            this.PartResults = partResults ?? new WorkItemConfigurationItemPartComparisonResult[0];
            this.Status = status.HasValue ? status.Value : (this.PartResults.Any(r => r.Status == ComparisonStatus.AreDifferent) ? ComparisonStatus.AreDifferent : ComparisonStatus.AreEqual);
            this.PercentMatch = this.PartResults.Sum(r => r.Status == ComparisonStatus.AreEqual ? r.RelativeSize : 0.0);
        }

        #endregion
    }
}