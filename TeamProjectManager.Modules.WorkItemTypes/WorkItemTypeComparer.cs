using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public static class WorkItemTypeComparer
    {
        #region Fields

        private static readonly string[] KnownFieldsWithReportableDimension = new string[] { "System.AreaPath", "System.AssignedTo", "System.ChangedBy", "System.ChangedDate", "System.CreatedBy", "System.CreatedDate", "System.Id", "System.IterationPath", "System.Reason", "System.State", "System.Title", "Microsoft.VSTS.Common.Severity", "Microsoft.VSTS.Common.StateChangeDate", "System.AuthorizedAs", "System.NodeName" };
        private static readonly string[] KnownFieldsWithReportableDetail = new string[] { "System.RevisedDate" };
        private static readonly string[] KnownFieldsWithReportableMeasure = new string[] { "System.AttachedFileCount", "System.ExternalLinkCount", "System.HyperLinkCount" };
        private static readonly string[] KnownFieldsWithSyncNameChanges = new string[] { "System.AuthorizedAs", "Microsoft.VSTS.Common.ActivatedBy", "Microsoft.VSTS.Common.ClosedBy", "Microsoft.VSTS.Common.ResolvedBy", "System.AssignedTo", "System.ChangedBy", "System.CreatedBy" };
        private static readonly string[] KnownFieldsWithFormulaSum = new string[] { "Microsoft.VSTS.Scheduling.OriginalEstimate", "Microsoft.VSTS.Scheduling.StoryPoints", "Microsoft.VSTS.Scheduling.CompletedWork", "Microsoft.VSTS.Scheduling.RemainingWork", "Microsoft.VSTS.Scheduling.BaselineWork", "System.AttachedFileCount", "System.ExternalLinkCount", "System.HyperLinkCount" };
        private static readonly string[] KnwonFieldsWithNamesToRemoveSpaces = new string[] { "Microsoft.VSTS.TCM.ReproSteps", "System.AreaId", "System.AttachedFileCount", "System.ExternalLinkCount", "System.HyperLinkCount", "System.IterationId", "System.RelatedLinkCount", "Microsoft.VSTS.TCM.AutomatedTestId", "Microsoft.VSTS.TCM.AutomatedTestName", "Microsoft.VSTS.TCM.AutomatedTestStorage", "Microsoft.VSTS.TCM.AutomatedTestType", "Microsoft.VSTS.TCM.LocalDataSource" };
        private static readonly string[] FieldRuleElementNames = new string[] { "WHEN", "WHENCHANGED", "WHENNOT", "WHENNOTCHANGED" };
        private static readonly Dictionary<string, Dictionary<string, string>> SystemFields = new Dictionary<string, Dictionary<string, string>>
        {
            { "System.AreaId", new Dictionary<string, string> { { "refname", "System.AreaId" }, { "name", "Area ID" }, { "type", "Integer" } } },
            { "System.AreaPath", new Dictionary<string, string> { { "refname", "System.AreaPath" }, { "name", "Area Path" }, { "type", "TreePath" }, { "reportable", "dimension" } } },
            { "System.AssignedTo", new Dictionary<string, string> { { "refname", "System.AssignedTo" }, { "name", "Assigned To" }, { "type", "String" }, { "reportable", "dimension" }, { "syncnamechanges", "true" } } },
            { "System.AttachedFileCount", new Dictionary<string, string> { { "refname", "System.AttachedFileCount" }, { "name", "Attached File Count" }, { "type", "Integer" } } },
            { "System.AuthorizedAs", new Dictionary<string, string> { { "refname", "System.AuthorizedAs" }, { "name", "Authorized As" }, { "type", "String" }, { "syncnamechanges", "true" } } },
            { "System.ChangedBy", new Dictionary<string, string> { { "refname", "System.ChangedBy" }, { "name", "Changed By" }, { "type", "String" }, { "reportable", "dimension" }, { "syncnamechanges", "true" } } },
            { "System.ChangedDate", new Dictionary<string, string> { { "refname", "System.ChangedDate" }, { "name", "Changed Date" }, { "type", "DateTime" }, { "reportable", "dimension" } } },
            { "System.CreatedBy", new Dictionary<string, string> { { "refname", "System.CreatedBy" }, { "name", "Created By" }, { "type", "String" }, { "reportable", "dimension" }, { "syncnamechanges", "true" } } },
            { "System.CreatedDate", new Dictionary<string, string> { { "refname", "System.CreatedDate" }, { "name", "Created Date" }, { "type", "DateTime" }, { "reportable", "dimension" } } },
            { "System.Description", new Dictionary<string, string> { { "refname", "System.Description" }, { "name", "Description" }, { "type", "PlainText" } } },
            { "System.ExternalLinkCount", new Dictionary<string, string> { { "refname", "System.ExternalLinkCount" }, { "name", "External Link Count" }, { "type", "Integer" } } },
            { "System.History", new Dictionary<string, string> { { "refname", "System.History" }, { "name", "History" }, { "type", "History" } } },
            { "System.HyperLinkCount", new Dictionary<string, string> { { "refname", "System.HyperLinkCount" }, { "name", "Hyperlink Count" }, { "type", "Integer" } } },
            { "System.Id", new Dictionary<string, string> { { "refname", "System.Id" }, { "name", "ID" }, { "type", "Integer" }, { "reportable", "dimension" } } },
            { "System.IterationId", new Dictionary<string, string> { { "refname", "System.IterationId" }, { "name", "Iteration ID" }, { "type", "Integer" } } },
            { "System.IterationPath", new Dictionary<string, string> { { "refname", "System.IterationPath" }, { "name", "Iteration Path" }, { "type", "TreePath" }, { "reportable", "dimension" } } },
            { "System.NodeName", new Dictionary<string, string> { { "refname", "System.NodeName" }, { "name", "Node Name" }, { "type", "String" } } },
            { "System.Reason", new Dictionary<string, string> { { "refname", "System.Reason" }, { "name", "Reason" }, { "type", "String" }, { "reportable", "dimension" } } },
            { "System.RelatedLinkCount", new Dictionary<string, string> { { "refname", "System.RelatedLinkCount" }, { "name", "Related Link Count" }, { "type", "Integer" } } },
            { "System.Rev", new Dictionary<string, string> { { "refname", "System.Rev" }, { "name", "Rev" }, { "type", "Integer" }, { "reportable", "dimension" } } },
            { "System.RevisedDate", new Dictionary<string, string> { { "refname", "System.RevisedDate" }, { "name", "Revised Date" }, { "type", "DateTime" }, { "reportable", "detail" } } },
            { "System.State", new Dictionary<string, string> { { "refname", "System.State" }, { "name", "State" }, { "type", "String" }, { "reportable", "dimension" } } },
            { "System.TeamProject", new Dictionary<string, string> { { "refname", "System.TeamProject" }, { "name", "Team Project" }, { "type", "String" }, { "reportable", "dimension" } } },
            { "System.Title", new Dictionary<string, string> { { "refname", "System.Title" }, { "name", "Title" }, { "type", "String" }, { "reportable", "dimension" } } },
            { "System.WorkItemType", new Dictionary<string, string> { { "refname", "System.WorkItemType" }, { "name", "Work Item Type" }, { "type", "String" }, { "reportable", "dimension" } } }
        };

        #endregion

        #region Compare

        public static ComparisonSourceComparisonResult Compare(ComparisonSource source, ICollection<WorkItemTypeDefinition> targetWorkItemTypes)
        {
            var workItemTypeResults = new List<WorkItemTypeComparisonResult>();

            foreach (var workItemTypeOnlyInSource in source.WorkItemTypes.Where(s => !targetWorkItemTypes.Any(t => string.Equals(s.Name, t.Name, StringComparison.OrdinalIgnoreCase))))
            {
                workItemTypeResults.Add(new WorkItemTypeComparisonResult(workItemTypeOnlyInSource.XmlDefinition, null, workItemTypeOnlyInSource.Name, ComparisonStatus.ExistsOnlyInSource));
            }

            foreach (var targetWorkItemType in targetWorkItemTypes)
            {
                var sourceWorkItemType = source.WorkItemTypes.FirstOrDefault(s => string.Equals(s.Name, targetWorkItemType.Name, StringComparison.OrdinalIgnoreCase));
                if (sourceWorkItemType == null)
                {
                    workItemTypeResults.Add(new WorkItemTypeComparisonResult(null, targetWorkItemType.XmlDefinition, targetWorkItemType.Name, ComparisonStatus.ExistsOnlyInTarget));
                }
                else
                {
                    var sourceDefinition = sourceWorkItemType.XmlDefinition.DocumentElement;
                    var targetDefinition = targetWorkItemType.XmlDefinition.DocumentElement;
                    var workItemTypeName = sourceWorkItemType.Name;

                    Normalize(sourceWorkItemType.XmlDefinition);
                    Normalize(targetWorkItemType.XmlDefinition);

                    var partResults = new List<WorkItemTypePartComparisonResult>();
                    var totalSize = Enum.GetValues(typeof(WorkItemTypeDefinitionPart)).Cast<WorkItemTypeDefinitionPart>().Sum(p => sourceWorkItemType.GetPart(p).OuterXml.Length);
                    foreach (WorkItemTypeDefinitionPart part in Enum.GetValues(typeof(WorkItemTypeDefinitionPart)))
                    {
                        var sourcePart = sourceWorkItemType.GetPart(part);
                        var targetPart = targetWorkItemType.GetPart(part);
                        var status = string.Equals(GetValue(sourcePart), GetValue(targetPart), StringComparison.Ordinal) ? ComparisonStatus.AreEqual : ComparisonStatus.AreDifferent;
                        var relativeSize = sourcePart.OuterXml.Length / (double)totalSize;
                        partResults.Add(new WorkItemTypePartComparisonResult(part, status, relativeSize));
                    }

                    workItemTypeResults.Add(new WorkItemTypeComparisonResult(sourceDefinition, targetDefinition, workItemTypeName, partResults));
                }
            }

            return new ComparisonSourceComparisonResult(source, workItemTypeResults);
        }

        #endregion

        #region Normalize

        private static void Normalize(XmlDocument root)
        {
            // To allow better comparisons between source XML files from Process Templates
            // and the actual exported work item type definition, process some special cases.

            // First normalize the entire node.
            root.Normalize();

            // Remove the XML declaration.
            var xmlDeclaration = root.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.XmlDeclaration).SingleOrDefault();
            if (xmlDeclaration != null)
            {
                root.RemoveChild(xmlDeclaration);
            }

            // Close non-empty tags without child elements.
            foreach (XmlElement emptyNode in root.SelectNodes("//*[not(node())]").Cast<XmlElement>().Where(n => !n.IsEmpty && !n.HasChildNodes))
            {
                emptyNode.IsEmpty = true;
            }

            // Remove all comments.
            foreach (var comment in root.SelectNodes("//comment()").Cast<XmlNode>())
            {
                comment.ParentNode.RemoveChild(comment);
            }

            // Remove all empty CustomControlOptions.
            foreach (var customControlOptionsNode in root.SelectNodes("//CustomControlOptions").Cast<XmlElement>().Where(n => !n.HasChildNodes))
            {
                customControlOptionsNode.ParentNode.RemoveChild(customControlOptionsNode);
            }

            // Remove the "expanditems" attribute for ALLOWEDVALUES and SUGGESTEDVALUES when it is true (the default).
            foreach (XmlAttribute expandItemsAttribute in root.SelectNodes("//ALLOWEDVALUES[@expanditems='true']/@expanditems | //SUGGESTEDVALUES[@expanditems='true']/@expanditems"))
            {
                expandItemsAttribute.OwnerElement.Attributes.Remove(expandItemsAttribute);
            }

            // Remove global list references (these are typically lists of builds for the Team Project).
            foreach (XmlNode buildsGlobalList in root.SelectNodes("//WORKITEMTYPE/FIELDS/FIELD/SUGGESTEDVALUES[@filteritems='excludegroups' and count(GLOBALLIST) = 1]"))
            {
                buildsGlobalList.ParentNode.RemoveChild(buildsGlobalList);
            }

            // Normalize certain casing differences.
            foreach (XmlAttribute closedInErrorAttribute in root.SelectNodes("//DEFAULTREASON[@value='Closed in error']/@value | //REASON[@value='Closed in error']/@value"))
            {
                closedInErrorAttribute.Value = "Closed in Error";
            }
            foreach (XmlAttribute overtakenByEventsAttribute in root.SelectNodes("//DEFAULTREASON[@value='Overtaken by events']/@value | //REASON[@value='Overtaken by events']/@value"))
            {
                overtakenByEventsAttribute.Value = "Overtaken by Events";
            }
            foreach (XmlAttribute fieldIdAttribute in root.SelectNodes("//WORKITEMTYPE/FIELDS/FIELD[@refname='System.Id']/@name"))
            {
                fieldIdAttribute.Value = "ID";
            }

            // Add certain fields that are auto-generated if they're not present.
            var fieldDefinitionsNode = root.SelectSingleNode("//WORKITEMTYPE/FIELDS");
            foreach (var refname in SystemFields.Keys)
            {
                var field = fieldDefinitionsNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture, "FIELD[@refname='{0}']", refname));
                if (field == null)
                {
                    field = root.CreateElement("FIELD");
                    foreach (var attributeEntry in SystemFields[refname])
                    {
                        var attribute = root.CreateAttribute(attributeEntry.Key);
                        attribute.Value = attributeEntry.Value;
                        field.Attributes.Append(attribute);
                    }
                    fieldDefinitionsNode.AppendChild(field);
                }
            }

            // Process fields.
            foreach (XmlElement field in root.SelectNodes("//WORKITEMTYPE/FIELDS/FIELD"))
            {
                var refname = field.Attributes["refname"].Value;

                // The ALLOWEXISTINGVALUE rule, when applied to a field, is in fact a shortcut
                // to apply it to all its rules (within the field but also in workflow states and transitions).
                var allowExistingValueNode = field.SelectSingleNode("ALLOWEXISTINGVALUE");
                if (allowExistingValueNode != null)
                {
                    foreach (var ruleElementName in FieldRuleElementNames)
                    {
                        AddElementIfNeeded(field.SelectSingleNode(ruleElementName), "ALLOWEXISTINGVALUE");
                    }

                    foreach (XmlNode fieldReference in root.SelectNodes(string.Format(CultureInfo.InvariantCulture, "//WORKITEMTYPE/WORKFLOW/STATES/STATE/FIELDS/FIELD[@refname='{0}'] | //WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/FIELDS/FIELD[@refname='{0}']", refname)))
                    {
                        AddElementIfNeeded(fieldReference, "ALLOWEXISTINGVALUE");
                    }

                    field.RemoveChild(allowExistingValueNode);
                }

                // Set the reportable and syncnamechanges attributes for certain system fields.
                SetAttributeIfNeeded(field, refname, KnownFieldsWithReportableDimension, "reportable", "dimension");
                SetAttributeIfNeeded(field, refname, KnownFieldsWithReportableDetail, "reportable", "detail");
                SetAttributeIfNeeded(field, refname, KnownFieldsWithReportableMeasure, "reportable", "measure");
                SetAttributeIfNeeded(field, refname, KnownFieldsWithSyncNameChanges, "syncnamechanges", "true");
                SetAttributeIfNeeded(field, refname, KnownFieldsWithFormulaSum, "formula", "sum");

                // Certain system fields have added spaces to their names, remove the spaces to normalize them.
                // See http://blogs.msdn.com/b/greggboer/archive/2010/02/25/names-changed-for-core-wit-fields-and-implications-thereof.aspx for more information.
                if (KnwonFieldsWithNamesToRemoveSpaces.Contains(refname))
                {
                    var nameAttribute = field.Attributes["name"];
                    nameAttribute.Value = nameAttribute.Value.Replace(" ", string.Empty);

                    // The HyperLinkCount field not only had spaces removed but also a case change on the 'L'.
                    if (string.Equals(nameAttribute.Value, "HyperlinkCount", StringComparison.OrdinalIgnoreCase))
                    {
                        nameAttribute.Value = "HyperLinkCount";
                    }

                    // The AreaID and IterationID field not only had spaces removed but also a case change on the 'D'.
                    if (string.Equals(nameAttribute.Value, "AreaId", StringComparison.OrdinalIgnoreCase))
                    {
                        nameAttribute.Value = "AreaID";
                    }
                    if (string.Equals(nameAttribute.Value, "IterationId", StringComparison.OrdinalIgnoreCase))
                    {
                        nameAttribute.Value = "IterationID";
                    }
                }
            }

            // Sort field definitions by "refname" everywhere.
            SortChildNodes(root.SelectSingleNode("//WORKITEMTYPE/FIELDS"), n => GetValue(n.Attributes["refname"]));
            foreach (XmlNode stateFieldsNode in root.SelectNodes("//WORKITEMTYPE/WORKFLOW/STATES/STATE/FIELDS"))
            {
                SortChildNodes(stateFieldsNode, n => GetValue(n.Attributes["refname"]));
            }
            foreach (XmlNode transitionFieldsNode in root.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/FIELDS"))
            {
                SortChildNodes(transitionFieldsNode, n => GetValue(n.Attributes["refname"]));
            }

            // Sort workflow states by "value".
            SortChildNodes(root.SelectSingleNode("//WORKITEMTYPE/WORKFLOW/STATES"), n => GetValue(n.Attributes["value"]));

            // Sort transitions by combination of "from" and "to".
            SortChildNodes(root.SelectSingleNode("//WORKITEMTYPE/WORKFLOW/TRANSITIONS"), n => string.Concat(GetValue(n.Attributes["from"]), " -> ", GetValue(n.Attributes["to"])));

            // Sort child nodes of fields anywhere.
            foreach (XmlNode field in root.SelectNodes("//FIELDS/FIELD"))
            {
                SortChildNodes(field);
            }

            // Sort child nodes of allowed values.
            foreach (XmlNode allowedValuesList in root.SelectNodes("//ALLOWEDVALUES"))
            {
                SortChildNodes(allowedValuesList);
            }

            // Sort child nodes of suggested values.
            foreach (XmlNode suggestedValuesList in root.SelectNodes("//SUGGESTEDVALUES"))
            {
                SortChildNodes(suggestedValuesList);
            }

            // Sort child nodes of transitions.
            foreach (XmlNode transitionList in root.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION"))
            {
                SortChildNodes(transitionList);
            }

            // Sort child nodes of transition reasons.
            foreach (XmlNode transitionReasonList in root.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/REASONS"))
            {
                SortChildNodes(transitionReasonList);
            }

            // Sort child nodes of links control options.
            foreach (XmlNode linksControlOptionList in root.SelectNodes("//LinksControlOptions"))
            {
                SortChildNodes(linksControlOptionList);
            }

            // Sort all attributes.
            foreach (XmlNode node in root.SelectNodes("//*"))
            {
                SortAttributes(node);
            }
        }

        #endregion

        #region Helper Methods

        private static void SetAttributeIfNeeded(XmlNode field, string refname, IEnumerable<string> refnameLookupList, string attributeName, string attributeValue)
        {
            if (refnameLookupList.Contains(refname, StringComparer.OrdinalIgnoreCase))
            {
                var reportableAttribute = field.Attributes[attributeName];
                if (reportableAttribute == null)
                {
                    reportableAttribute = field.OwnerDocument.CreateAttribute(attributeName);
                    reportableAttribute.Value = attributeValue;
                    field.Attributes.Append(reportableAttribute);
                }
            }
        }

        private static void AddElementIfNeeded(XmlNode node, string elementName)
        {
            if (node != null)
            {
                var existingElement = node.SelectSingleNode(elementName);
                if (existingElement == null)
                {
                    var n = node.OwnerDocument.CreateElement(elementName);
                    node.AppendChild(n);
                }
                SortChildNodes(node);
            }
        }

        private static void SortChildNodes(XmlNode node)
        {
            SortChildNodes(node, null);
        }

        private static void SortChildNodes(XmlNode node, Func<XmlNode, string> identitySelector)
        {
            foreach (var nodeElement in node.ChildNodes.Cast<XmlNode>().OrderBy(n => identitySelector == null ? n.OuterXml : identitySelector(n)).ToList())
            {
                node.RemoveChild(nodeElement);
                node.AppendChild(nodeElement);
            }
        }

        private static void SortAttributes(XmlNode node)
        {
            foreach (var nodeAttribute in node.Attributes.Cast<XmlAttribute>().OrderBy(a => a.Name).ToList())
            {
                node.Attributes.Remove(nodeAttribute);
                node.Attributes.Append(nodeAttribute);
            }
        }

        private static string GetValue(XmlNode node)
        {
            return (node == null ? null : node is XmlAttribute ? node.Value : node.OuterXml);
        }

        #endregion
    }
}