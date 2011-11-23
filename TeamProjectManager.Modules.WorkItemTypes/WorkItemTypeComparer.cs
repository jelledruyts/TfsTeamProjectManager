using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public static class WorkItemTypeComparer
    {
        public static ComparisonSourceComparisonResult Compare(ComparisonSource source, ICollection<WorkItemTypeDefinition> targetWorkItemTypes)
        {
            var workItemTypeResults = new List<WorkItemTypeComparisonResult>();

            foreach (var workItemTypeOnlyInSource in source.WorkItemTypeFiles.Where(s => !targetWorkItemTypes.Any(t => string.Equals(s.Name, t.Name, StringComparison.OrdinalIgnoreCase))))
            {
                workItemTypeResults.Add(new WorkItemTypeComparisonResult(workItemTypeOnlyInSource, null, ComparisonStatus.ExistsOnlyInSource));
            }

            foreach (var targetWorkItemType in targetWorkItemTypes)
            {
                var sourceWorkItemType = source.WorkItemTypeFiles.FirstOrDefault(s => string.Equals(s.Name, targetWorkItemType.Name, StringComparison.OrdinalIgnoreCase));
                if (sourceWorkItemType == null)
                {
                    workItemTypeResults.Add(new WorkItemTypeComparisonResult(null, targetWorkItemType, ComparisonStatus.ExistsOnlyInTarget));
                }
                else
                {
                    var sourceDefinition = sourceWorkItemType.XmlDefinition.DocumentElement;
                    var targetDefinition = targetWorkItemType.XmlDefinition.DocumentElement;
                    if (string.Equals(sourceDefinition.OuterXml, targetDefinition.OuterXml, StringComparison.Ordinal))
                    {
                        workItemTypeResults.Add(new WorkItemTypeComparisonResult(sourceWorkItemType, targetWorkItemType, ComparisonStatus.AreEqual));
                    }
                    else
                    {
                        var equalParts = new List<WorkItemTypeDefinitionPart>();
                        var percentMatch = 0.0;
                        foreach (WorkItemTypeDefinitionPart part in Enum.GetValues(typeof(WorkItemTypeDefinitionPart)))
                        {
                            if (AreEqual(sourceWorkItemType.GetPart(part), targetWorkItemType.GetPart(part)))
                            {
                                equalParts.Add(part);
                                if (part == WorkItemTypeDefinitionPart.Description)
                                {
                                    percentMatch += 0.05;
                                }
                                if (part == WorkItemTypeDefinitionPart.Fields)
                                {
                                    percentMatch += 0.45;
                                }
                                if (part == WorkItemTypeDefinitionPart.Workflow)
                                {
                                    percentMatch += 0.25;
                                }
                                if (part == WorkItemTypeDefinitionPart.Form)
                                {
                                    percentMatch += 0.25;
                                }
                            }
                        }

                        workItemTypeResults.Add(new WorkItemTypeComparisonResult(sourceWorkItemType, targetWorkItemType, ComparisonStatus.AreDifferent, equalParts, percentMatch));
                    }
                }
            }

            return new ComparisonSourceComparisonResult(source, workItemTypeResults);
        }

        private static bool AreEqual(XmlNode sourceNode, XmlNode targetNode)
        {
            var sourceXml = sourceNode == null ? null : sourceNode.OuterXml;
            var targetXml = targetNode == null ? null : targetNode.OuterXml;
            return string.Equals(sourceXml, targetXml, StringComparison.Ordinal);
        }
    }
}