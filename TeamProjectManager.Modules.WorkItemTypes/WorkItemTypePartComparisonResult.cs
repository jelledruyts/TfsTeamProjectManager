
namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypePartComparisonResult
    {
        public WorkItemTypeDefinitionPart Part { get; private set; }
        public ComparisonStatus Status { get; private set; }
        public double RelativeSize { get; private set; }

        public WorkItemTypePartComparisonResult(WorkItemTypeDefinitionPart part, ComparisonStatus status, double relativeSize)
        {
            this.Part = part;
            this.Status = status;
            this.RelativeSize = relativeSize;
        }
    }
}