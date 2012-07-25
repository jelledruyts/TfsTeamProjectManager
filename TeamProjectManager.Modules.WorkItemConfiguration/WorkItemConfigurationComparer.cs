using System;
using System.Collections.Generic;
using System.Linq;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public static class WorkItemConfigurationComparer
    {
        #region Compare

        public static WorkItemConfigurationComparisonResult Compare(TfsMajorVersion tfsMajorVersion, WorkItemConfiguration source, WorkItemConfiguration target)
        {
            var itemResults = new List<WorkItemConfigurationItemComparisonResult>();

            foreach (var itemOnlyInSource in source.Items.Where(s => !target.Items.Any(t => s.Type == t.Type && string.Equals(s.Name, t.Name, StringComparison.OrdinalIgnoreCase))))
            {
                itemResults.Add(new WorkItemConfigurationItemComparisonResult(itemOnlyInSource.XmlDefinition, null, itemOnlyInSource.Name, itemOnlyInSource.Type, ComparisonStatus.ExistsOnlyInSource));
            }

            foreach (var targetItem in target.Items)
            {
                var sourceItem = source.Items.FirstOrDefault(s => s.Type == targetItem.Type && string.Equals(s.Name, targetItem.Name, StringComparison.OrdinalIgnoreCase));
                if (sourceItem == null)
                {
                    itemResults.Add(new WorkItemConfigurationItemComparisonResult(null, targetItem.XmlDefinition, targetItem.Name, targetItem.Type, ComparisonStatus.ExistsOnlyInTarget));
                }
                else
                {
                    // To allow better comparisons between source XML files from Process Templates
                    // and the actual exported work item configuration item, normalize and process some special cases.
                    var sourceNormalizedXmlDefinition = XmlNormalizer.GetNormalizedXmlDefinition(sourceItem, tfsMajorVersion);
                    var targetNormalizedXmlDefinition = XmlNormalizer.GetNormalizedXmlDefinition(targetItem, tfsMajorVersion);

                    var partResults = new List<WorkItemConfigurationItemPartComparisonResult>();
                    var sourceParts = sourceItem.GetParts(sourceNormalizedXmlDefinition);
                    var targetParts = targetItem.GetParts(targetNormalizedXmlDefinition);
                    var totalSize = sourceParts.Sum(p => p.NormalizedXmlDefinition == null ? 0 : p.NormalizedXmlDefinition.OuterXml.Length);
                    foreach (WorkItemConfigurationItemPart sourcePart in sourceParts)
                    {
                        var targetPart = targetParts.Single(p => p.Name == sourcePart.Name);
                        var status = string.Equals(XmlNormalizer.GetValue(sourcePart.NormalizedXmlDefinition), XmlNormalizer.GetValue(targetPart.NormalizedXmlDefinition), StringComparison.Ordinal) ? ComparisonStatus.AreEqual : ComparisonStatus.AreDifferent;
                        var relativeSize = (sourcePart.NormalizedXmlDefinition == null ? 0 : sourcePart.NormalizedXmlDefinition.OuterXml.Length) / (double)totalSize;
                        partResults.Add(new WorkItemConfigurationItemPartComparisonResult(sourcePart.Name, status, relativeSize));
                    }

                    itemResults.Add(new WorkItemConfigurationItemComparisonResult(sourceNormalizedXmlDefinition, targetNormalizedXmlDefinition, sourceItem.Name, sourceItem.Type, partResults));
                }
            }

            return new WorkItemConfigurationComparisonResult(source, target, itemResults);
        }

        #endregion
    }
}