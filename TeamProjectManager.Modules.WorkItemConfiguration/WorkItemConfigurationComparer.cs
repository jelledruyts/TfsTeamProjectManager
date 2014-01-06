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

            var sourceItems = source.Items.ToList();
            var targetItems = target.Items.ToList();

            // Ignore TFS 2012 agile/common config when both source and target have TFS 2013 process config.
            if (sourceItems.Any(i => i.Type == WorkItemConfigurationItemType.ProcessConfiguration) && targetItems.Any(i => i.Type == WorkItemConfigurationItemType.ProcessConfiguration))
            {
                sourceItems.RemoveAll(i => i.Type == WorkItemConfigurationItemType.CommonConfiguration || i.Type == WorkItemConfigurationItemType.AgileConfiguration);
                targetItems.RemoveAll(i => i.Type == WorkItemConfigurationItemType.CommonConfiguration || i.Type == WorkItemConfigurationItemType.AgileConfiguration);
            }

            // If the source doesn't have categories or agile/common/process config, ignore them in the target as well.
            if (!sourceItems.Any(i => i.Type == WorkItemConfigurationItemType.Categories))
            {
                targetItems.RemoveAll(i => i.Type == WorkItemConfigurationItemType.Categories);
            }
            if (!sourceItems.Any(i => i.Type == WorkItemConfigurationItemType.AgileConfiguration))
            {
                targetItems.RemoveAll(i => i.Type == WorkItemConfigurationItemType.AgileConfiguration);
            }
            if (!sourceItems.Any(i => i.Type == WorkItemConfigurationItemType.CommonConfiguration))
            {
                targetItems.RemoveAll(i => i.Type == WorkItemConfigurationItemType.CommonConfiguration);
            }
            if (!sourceItems.Any(i => i.Type == WorkItemConfigurationItemType.ProcessConfiguration))
            {
                targetItems.RemoveAll(i => i.Type == WorkItemConfigurationItemType.ProcessConfiguration);
            }

            foreach (var itemOnlyInSource in sourceItems.Where(s => !targetItems.Any(t => s.Type == t.Type && string.Equals(s.Name, t.Name, StringComparison.OrdinalIgnoreCase))))
            {
                itemResults.Add(new WorkItemConfigurationItemComparisonResult(itemOnlyInSource.XmlDefinition, null, itemOnlyInSource.Name, itemOnlyInSource.Type, ComparisonStatus.ExistsOnlyInSource));
            }

            foreach (var targetItem in targetItems)
            {
                var sourceItem = sourceItems.FirstOrDefault(s => s.Type == targetItem.Type && string.Equals(s.Name, targetItem.Name, StringComparison.OrdinalIgnoreCase));
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