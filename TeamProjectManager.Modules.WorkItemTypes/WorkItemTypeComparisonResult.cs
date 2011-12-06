using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeComparisonResult
    {
        #region Properties

        public string WorkItemTypeName { get; private set; }
        public XmlNode NormalizedSourceDefinition { get; private set; }
        public XmlNode NormalizedTargetDefinition { get; private set; }
        public ComparisonStatus Status { get; private set; }
        public ICollection<WorkItemTypePartComparisonResult> WorkItemTypePartResults { get; private set; }
        public double PercentMatch { get; private set; }

        #endregion

        #region Constructors

        public WorkItemTypeComparisonResult(XmlNode normalizedSourceDefinition, XmlNode normalizedTargetDefinition, string workItemTypeName, ComparisonStatus status)
            : this(normalizedSourceDefinition, normalizedTargetDefinition, workItemTypeName, null, status)
        {
        }

        public WorkItemTypeComparisonResult(XmlNode normalizedSourceDefinition, XmlNode normalizedTargetDefinition, string workItemTypeName, ICollection<WorkItemTypePartComparisonResult> workItemTypePartResults)
            : this(normalizedSourceDefinition, normalizedTargetDefinition, workItemTypeName, workItemTypePartResults, null)
        {
        }

        private WorkItemTypeComparisonResult(XmlNode normalizedSourceDefinition, XmlNode normalizedTargetDefinition, string workItemTypeName, ICollection<WorkItemTypePartComparisonResult> workItemTypePartResults, ComparisonStatus? status)
        {
            this.NormalizedSourceDefinition = normalizedSourceDefinition;
            this.NormalizedTargetDefinition = normalizedTargetDefinition;
            this.WorkItemTypeName = workItemTypeName;
            this.WorkItemTypePartResults = workItemTypePartResults ?? new WorkItemTypePartComparisonResult[0];
            this.Status = status.HasValue ? status.Value : (this.WorkItemTypePartResults.Any(r => r.Status == ComparisonStatus.AreDifferent) ? ComparisonStatus.AreDifferent : ComparisonStatus.AreEqual);
            this.PercentMatch = this.WorkItemTypePartResults.Sum(r => r.Status == ComparisonStatus.AreEqual ? r.RelativeSize : 0.0);
        }

        #endregion
    }
}