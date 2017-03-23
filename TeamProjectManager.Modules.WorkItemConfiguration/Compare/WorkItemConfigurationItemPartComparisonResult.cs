
namespace TeamProjectManager.Modules.WorkItemConfiguration.Compare
{
    public class WorkItemConfigurationItemPartComparisonResult
    {
        public string PartName { get; private set; }
        public ComparisonStatus Status { get; private set; }
        public double RelativeSize { get; private set; }

        public WorkItemConfigurationItemPartComparisonResult(string partName, ComparisonStatus status, double relativeSize)
        {
            this.PartName = partName;
            this.Status = status;
            this.RelativeSize = relativeSize;
        }
    }
}