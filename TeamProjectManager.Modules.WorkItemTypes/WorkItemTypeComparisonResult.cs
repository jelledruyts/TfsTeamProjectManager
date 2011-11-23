using System.Collections.Generic;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeComparisonResult
    {
        #region Constants

        private static readonly ICollection<WorkItemTypeDefinitionPart> allParts = new WorkItemTypeDefinitionPart[] { WorkItemTypeDefinitionPart.Description, WorkItemTypeDefinitionPart.Fields, WorkItemTypeDefinitionPart.Workflow, WorkItemTypeDefinitionPart.Form };
        private static readonly ICollection<WorkItemTypeDefinitionPart> noneParts = new WorkItemTypeDefinitionPart[0];

        #endregion

        #region Properties

        public string WorkItemTypeName { get; private set; }
        public WorkItemTypeDefinition SourceWorkItemType { get; private set; }
        public WorkItemTypeDefinition TargetWorkItemType { get; private set; }
        public ComparisonStatus Status { get; private set; }
        public ICollection<WorkItemTypeDefinitionPart> MatchingParts { get; private set; }
        public double PercentMatch { get; private set; }

        #endregion

        #region Constructors

        public WorkItemTypeComparisonResult(WorkItemTypeDefinition sourceWorkItemType, WorkItemTypeDefinition targetWorkItemType, ComparisonStatus status)
            : this(sourceWorkItemType, targetWorkItemType, status, status == ComparisonStatus.AreEqual ? allParts : noneParts, status == ComparisonStatus.AreEqual ? 1.0 : 0.0)
        {
        }

        public WorkItemTypeComparisonResult(WorkItemTypeDefinition sourceWorkItemType, WorkItemTypeDefinition targetWorkItemType, ComparisonStatus status, ICollection<WorkItemTypeDefinitionPart> matchingParts, double percentMatch)
        {
            this.SourceWorkItemType = sourceWorkItemType;
            this.TargetWorkItemType = targetWorkItemType;
            this.WorkItemTypeName = this.SourceWorkItemType == null ? this.TargetWorkItemType.Name : this.SourceWorkItemType.Name;
            this.Status = status;
            this.MatchingParts = matchingParts ?? noneParts;
            this.PercentMatch = percentMatch;
        }

        #endregion
    }
}