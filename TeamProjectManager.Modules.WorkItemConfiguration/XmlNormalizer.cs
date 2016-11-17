using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using TeamProjectManager.Common;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public static class XmlNormalizer
    {
        #region Static Fields & Constructor

        private static readonly string[] KnownFieldsWithReportableDimension;
        private static readonly string[] KnownFieldsWithReportableDetail;
        private static readonly string[] KnownFieldsWithReportableMeasure;
        private static readonly string[] KnownFieldsWithSyncNameChanges;
        private static readonly string[] KnownFieldsWithFormulaSum;
        private static readonly string[] KnownFieldsWithNamesToRemoveSpaces;
        private static readonly string[] FieldRuleElementNames;
        private static readonly Dictionary<TfsMajorVersion, Dictionary<string, Dictionary<string, string>>> SystemFields;

        static XmlNormalizer()
        {
            KnownFieldsWithReportableDimension = new string[] { "System.AreaPath", "System.AssignedTo", "System.ChangedBy", "System.ChangedDate", "System.CreatedBy", "System.CreatedDate", "System.Id", "System.IterationPath", "System.Reason", "System.State", "System.Title", "Microsoft.VSTS.Common.Severity", "Microsoft.VSTS.Common.StateChangeDate", "System.AuthorizedAs", "System.NodeName", "Microsoft.VSTS.CMMI.RootCause", "Microsoft.VSTS.CMMI.Probability", "Microsoft.VSTS.Common.ReviewedBy" };
            KnownFieldsWithReportableDetail = new string[] { "System.RevisedDate" };
            KnownFieldsWithReportableMeasure = new string[] { "System.AttachedFileCount", "System.ExternalLinkCount", "System.HyperLinkCount" };
            KnownFieldsWithSyncNameChanges = new string[] { "System.AuthorizedAs", "Microsoft.VSTS.Common.ActivatedBy", "Microsoft.VSTS.Common.ClosedBy", "Microsoft.VSTS.Common.ResolvedBy", "System.AssignedTo", "System.ChangedBy", "System.CreatedBy", "Microsoft.VSTS.Common.ReviewedBy", "Microsoft.VSTS.CMMI.SubjectMatterExpert", "Microsoft.VSTS.CMMI.ActualAttendee", "Microsoft.VSTS.CMMI.OptionalAttendee", "Microsoft.VSTS.CMMI.RequiredAttendee", "Microsoft.VSTS.CMMI.CalledBy" };
            KnownFieldsWithFormulaSum = new string[] { "Microsoft.VSTS.Scheduling.OriginalEstimate", "Microsoft.VSTS.Scheduling.StoryPoints", "Microsoft.VSTS.Scheduling.CompletedWork", "Microsoft.VSTS.Scheduling.RemainingWork", "Microsoft.VSTS.Scheduling.BaselineWork", "System.AttachedFileCount", "System.ExternalLinkCount", "System.HyperLinkCount" };
            KnownFieldsWithNamesToRemoveSpaces = new string[] { "Microsoft.VSTS.TCM.ReproSteps", "System.AreaId", "System.AttachedFileCount", "System.ExternalLinkCount", "System.HyperLinkCount", "System.IterationId", "System.RelatedLinkCount", "Microsoft.VSTS.TCM.AutomatedTestId", "Microsoft.VSTS.TCM.AutomatedTestName", "Microsoft.VSTS.TCM.AutomatedTestStorage", "Microsoft.VSTS.TCM.AutomatedTestType", "Microsoft.VSTS.TCM.LocalDataSource" };
            FieldRuleElementNames = new string[] { "WHEN", "WHENCHANGED", "WHENNOT", "WHENNOTCHANGED" };

            // Create a lookup table of common system fields where the key is the refname.
            var baseSystemFields = new Dictionary<string, Dictionary<string, string>>
            {
                { "System.AreaId", new Dictionary<string, string> { { "refname", "System.AreaId" }, { "name", "Area ID" }, { "type", "Integer" } } },
                { "System.AreaPath", new Dictionary<string, string> { { "refname", "System.AreaPath" }, { "name", "Area Path" }, { "type", "TreePath" }, { "reportable", "dimension" } } },
                { "System.AssignedTo", new Dictionary<string, string> { { "refname", "System.AssignedTo" }, { "name", "Assigned To" }, { "type", "String" }, { "reportable", "dimension" }, { "syncnamechanges", "true" } } },
                { "System.AttachedFileCount", new Dictionary<string, string> { { "refname", "System.AttachedFileCount" }, { "name", "Attached File Count" }, { "type", "Integer" } } },
                { "System.AuthorizedAs", new Dictionary<string, string> { { "refname", "System.AuthorizedAs" }, { "name", "Authorized As" }, { "type", "String" }, { "syncnamechanges", "true" } } },
                { "System.AuthorizedDate", new Dictionary<string, string> { { "refname", "System.AuthorizedDate" }, { "name", "Authorized Date" }, { "type", "DateTime" } } },
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
                { "System.Tags", new Dictionary<string, string> { { "refname", "System.Tags" }, { "name", "Tags" }, { "type", "PlainText" } } },
                { "System.TeamProject", new Dictionary<string, string> { { "refname", "System.TeamProject" }, { "name", "Team Project" }, { "type", "String" }, { "reportable", "dimension" } } },
                { "System.Title", new Dictionary<string, string> { { "refname", "System.Title" }, { "name", "Title" }, { "type", "String" }, { "reportable", "dimension" } } },
                { "System.Watermark", new Dictionary<string, string> { { "refname", "System.Watermark" }, { "name", "Watermark" }, { "type", "Integer" } } },
                { "System.WorkItemType", new Dictionary<string, string> { { "refname", "System.WorkItemType" }, { "name", "Work Item Type" }, { "type", "String" }, { "reportable", "dimension" } } },
                { "System.BoardColumn", new Dictionary<string, string> { { "refname", "System.BoardColumn" }, { "name", "Board Column" }, { "type", "String" }, { "reportable", "dimension" } } },
                { "System.BoardColumnDone", new Dictionary<string, string> { { "refname", "System.BoardColumnDone" }, { "name", "Board Column Done" }, { "type", "Boolean" }, { "reportable", "dimension" } } },
                { "System.BoardLane", new Dictionary<string, string> { { "refname", "System.BoardLane" }, { "name", "Board Lane" }, { "type", "String" }, { "reportable", "dimension" } } }
            };

            // TFS 11 by default now uses HTML for the description.
            var tfs11SystemFields = new Dictionary<string, Dictionary<string, string>>(baseSystemFields);
            tfs11SystemFields["System.Description"] = new Dictionary<string, string> { { "refname", "System.Description" }, { "name", "Description" }, { "type", "HTML" } };

            // Create a lookup table of system fields per major version of TFS.
            SystemFields = new Dictionary<TfsMajorVersion, Dictionary<string, Dictionary<string, string>>>
            {
                { TfsMajorVersion.Unknown, baseSystemFields },
                { TfsMajorVersion.V11, tfs11SystemFields }
            };
        }

        #endregion

        #region GetNormalizedXmlDefinition

        public static XmlDocument GetNormalizedXmlDefinition(WorkItemConfigurationItem item, TfsMajorVersion tfsMajorVersion)
        {
            var normalizedXmlDefinition = new XmlDocument();
            normalizedXmlDefinition.LoadXml(item.XmlDefinition.OuterXml);

            // Perform pre-normalization.
            PreNormalizeXml(normalizedXmlDefinition);

            // Perform type-specific normalization.
            if (item.Type == WorkItemConfigurationItemType.WorkItemType)
            {
                NormalizeWorkItemTypeDefinition(normalizedXmlDefinition, tfsMajorVersion);
            }
            else if (item.Type == WorkItemConfigurationItemType.AgileConfiguration)
            {
                NormalizeAgileConfiguration(normalizedXmlDefinition);
            }
            else if (item.Type == WorkItemConfigurationItemType.CommonConfiguration)
            {
                NormalizeCommonConfiguration(normalizedXmlDefinition);
            }
            else if (item.Type == WorkItemConfigurationItemType.ProcessConfiguration)
            {
                NormalizeProcessConfiguration(normalizedXmlDefinition);
            }
            else if (item.Type == WorkItemConfigurationItemType.Categories)
            {
                NormalizeCategories(normalizedXmlDefinition);
            }

            // Perform other normalization after the specific normalization (nodes could have been added or removed).
            PostNormalizeXml(normalizedXmlDefinition);

            return normalizedXmlDefinition;
        }

        #endregion

        #region Pre & Post NormalizeXml

        private static void PreNormalizeXml(XmlDocument normalizedXmlDefinition)
        {
            // First normalize the entire node.
            normalizedXmlDefinition.Normalize();

            // Remove the XML declaration.
            var xmlDeclaration = normalizedXmlDefinition.ChildNodes.Cast<XmlNode>().Where(n => n.NodeType == XmlNodeType.XmlDeclaration).SingleOrDefault();
            if (xmlDeclaration != null)
            {
                normalizedXmlDefinition.RemoveChild(xmlDeclaration);
            }

            // Remove all comments.
            foreach (var comment in normalizedXmlDefinition.SelectNodes("//comment()").Cast<XmlNode>())
            {
                comment.ParentNode.RemoveChild(comment);
            }
        }

        private static void PostNormalizeXml(XmlDocument normalizedXmlDefinition)
        {
            // Close non-empty tags without child elements.
            foreach (XmlElement emptyNode in normalizedXmlDefinition.SelectNodes("//*[not(node())]").Cast<XmlElement>().Where(n => !n.IsEmpty && !n.HasChildNodes))
            {
                emptyNode.IsEmpty = true;
            }

            // Sort all attributes.
            foreach (XmlNode node in normalizedXmlDefinition.SelectNodes("//*"))
            {
                SortAttributes(node);
            }
        }

        #endregion

        #region NormalizeWorkItemTypeDefinition

        private static void NormalizeWorkItemTypeDefinition(XmlDocument normalizedXmlDefinition, TfsMajorVersion tfsMajorVersion)
        {
            var rawXml = normalizedXmlDefinition.OuterXml;

            // Replace "[Project]\" with "[project]\" everywhere.
            rawXml = rawXml.Replace(@"[Project]\", @"[project]\");

            normalizedXmlDefinition.LoadXml(rawXml);

            // Remove the work item type refname if present, since it is not returned from the project.
            var workitemTypeNode = normalizedXmlDefinition.SelectSingleNode("//WORKITEMTYPE");
            var refnameAttribute = workitemTypeNode.Attributes["refname"];
            if (refnameAttribute != null)
            {
                workitemTypeNode.Attributes.Remove(refnameAttribute);
            }

            // Set the default page layout mode if needed.
            foreach (var pageNode in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/FORM/WebLayout/Page").Cast<XmlElement>())
            {
                AddAttributeIfNeeded(pageNode, "LayoutMode", "FirstColumnWide");
            }

            // Remove all empty CustomControlOptions.
            foreach (var customControlOptionsNode in normalizedXmlDefinition.SelectNodes("//CustomControlOptions").Cast<XmlElement>().Where(n => !n.HasChildNodes))
            {
                customControlOptionsNode.ParentNode.RemoveChild(customControlOptionsNode);
            }

            // Remove the "expanditems" attribute for ALLOWEDVALUES, SUGGESTEDVALUES and PROHIBITEDVALUES when it is true (the default).
            foreach (XmlAttribute expandItemsAttribute in normalizedXmlDefinition.SelectNodes("//ALLOWEDVALUES[@expanditems='true']/@expanditems | //SUGGESTEDVALUES[@expanditems='true']/@expanditems | //PROHIBITEDVALUES[@expanditems='true']/@expanditems"))
            {
                expandItemsAttribute.OwnerElement.Attributes.Remove(expandItemsAttribute);
            }

            // Remove global list references (these are typically lists of builds for the Team Project).
            foreach (XmlNode buildsGlobalList in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/FIELDS/FIELD/SUGGESTEDVALUES[@filteritems='excludegroups' and count(GLOBALLIST) = 1]"))
            {
                buildsGlobalList.ParentNode.RemoveChild(buildsGlobalList);
            }

            // Normalize certain casing differences.
            foreach (XmlAttribute closedInErrorAttribute in normalizedXmlDefinition.SelectNodes("//DEFAULTREASON[@value='Closed in error']/@value | //REASON[@value='Closed in error']/@value"))
            {
                closedInErrorAttribute.Value = "Closed in Error";
            }
            foreach (XmlAttribute overtakenByEventsAttribute in normalizedXmlDefinition.SelectNodes("//DEFAULTREASON[@value='Overtaken by events']/@value | //REASON[@value='Overtaken by events']/@value"))
            {
                overtakenByEventsAttribute.Value = "Overtaken by Events";
            }
            foreach (XmlAttribute notFixedAttribute in normalizedXmlDefinition.SelectNodes("//DEFAULTREASON[@value='Not fixed']/@value | //REASON[@value='Not fixed']/@value"))
            {
                notFixedAttribute.Value = "Not Fixed";
            }
            foreach (XmlAttribute reconsideringAttribute in normalizedXmlDefinition.SelectNodes("//DEFAULTREASON[@value='Reconsidering the feature']/@value | //REASON[@value='Reconsidering the feature']/@value"))
            {
                reconsideringAttribute.Value = "Reconsidering the Feature";
            }
            foreach (XmlAttribute reconsideringAttribute in normalizedXmlDefinition.SelectNodes("//DEFAULTREASON[@value='Reconsidering the epic']/@value | //REASON[@value='Reconsidering the epic']/@value"))
            {
                reconsideringAttribute.Value = "Reconsidering the Epic";
            }
            foreach (XmlAttribute fieldIdAttribute in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/FIELDS/FIELD[@refname='System.Id']/@name"))
            {
                fieldIdAttribute.Value = "ID";
            }

            // Add certain fields that are auto-generated if they're not present.
            var fieldDefinitionsNode = normalizedXmlDefinition.SelectSingleNode("//WORKITEMTYPE/FIELDS");
            var highestMatchingTfsVersionWithSystemFields = SystemFields.Keys.OrderBy(v => (int)v).Last(v => v <= tfsMajorVersion);
            var currentSystemFields = SystemFields[highestMatchingTfsVersionWithSystemFields];
            foreach (var refname in currentSystemFields.Keys)
            {
                var field = fieldDefinitionsNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture, "FIELD[@refname='{0}']", refname));
                if (field == null)
                {
                    field = normalizedXmlDefinition.CreateElement("FIELD");
                    foreach (var attributeEntry in currentSystemFields[refname])
                    {
                        var attribute = normalizedXmlDefinition.CreateAttribute(attributeEntry.Key);
                        attribute.Value = attributeEntry.Value;
                        field.Attributes.Append(attribute);
                    }
                    fieldDefinitionsNode.AppendChild(field);
                }
            }

            // Process fields.
            foreach (XmlElement field in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/FIELDS/FIELD"))
            {
                var refname = field.Attributes["refname"].Value;

                // The ALLOWEXISTINGVALUE rule, when applied to a field, is in fact a shortcut
                // to apply it to all its rules (within the field but also in workflow states and transitions).
                var allowExistingValueNode = field.SelectSingleNode("ALLOWEXISTINGVALUE");
                if (allowExistingValueNode != null)
                {
                    // The ALLOWEXISTINGVALUE doesn't make sense if there are only DEFAULT or COPY rules in the list,
                    // in which case the ALLOWEXISTINGVALUE rule isn't present in an exported WITD.
                    Predicate<XmlNode> predicate = parentNode => parentNode.ChildNodes.Cast<XmlNode>().Any(childNode => !(string.Equals(childNode.LocalName, "DEFAULT", StringComparison.OrdinalIgnoreCase) || string.Equals(childNode.LocalName, "COPY", StringComparison.OrdinalIgnoreCase)));
                    foreach (var ruleElementName in FieldRuleElementNames)
                    {
                        AddElementIfNeeded(field.SelectSingleNode(ruleElementName), "ALLOWEXISTINGVALUE", predicate);
                    }

                    foreach (XmlNode fieldReference in normalizedXmlDefinition.SelectNodes(string.Format(CultureInfo.InvariantCulture, "//WORKITEMTYPE/WORKFLOW/STATES/STATE/FIELDS/FIELD[@refname='{0}'] | //WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/FIELDS/FIELD[@refname='{0}']", refname)))
                    {
                        AddElementIfNeeded(fieldReference, "ALLOWEXISTINGVALUE", predicate);
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
                if (KnownFieldsWithNamesToRemoveSpaces.Contains(refname))
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
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("//WORKITEMTYPE/FIELDS"), n => GetValue(n.Attributes["refname"]));
            foreach (XmlNode stateFieldsNode in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/WORKFLOW/STATES/STATE/FIELDS"))
            {
                SortChildNodes(stateFieldsNode, n => GetValue(n.Attributes["refname"]));
            }
            foreach (XmlNode transitionFieldsNode in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/FIELDS"))
            {
                SortChildNodes(transitionFieldsNode, n => GetValue(n.Attributes["refname"]));
            }

            // Sort workflow states by "value".
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("//WORKITEMTYPE/WORKFLOW/STATES"), n => GetValue(n.Attributes["value"]));

            // Sort transitions by combination of "from" and "to".
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("//WORKITEMTYPE/WORKFLOW/TRANSITIONS"), n => string.Concat(GetValue(n.Attributes["from"]), " -> ", GetValue(n.Attributes["to"])));

            // Sort child nodes of fields anywhere.
            foreach (XmlNode field in normalizedXmlDefinition.SelectNodes("//FIELDS/FIELD"))
            {
                SortChildNodes(field);
            }

            // Sort child nodes of FIELD/WHEN, FIELD/WHENNOT, FIELD/WHENCHANGED, FIELD/WHENNOTCHANGED.
            foreach (XmlNode field in normalizedXmlDefinition.SelectNodes("//FIELDS/FIELD/WHEN"))
            {
                SortChildNodes(field);
            }
            foreach (XmlNode field in normalizedXmlDefinition.SelectNodes("//FIELDS/FIELD/WHENNOT"))
            {
                SortChildNodes(field);
            }
            foreach (XmlNode field in normalizedXmlDefinition.SelectNodes("//FIELDS/FIELD/WHENCHANGED"))
            {
                SortChildNodes(field);
            }
            foreach (XmlNode field in normalizedXmlDefinition.SelectNodes("//FIELDS/FIELD/WHENNOTCHANGED"))
            {
                SortChildNodes(field);
            }

            // Sort child nodes of allowed values.
            foreach (XmlNode allowedValuesList in normalizedXmlDefinition.SelectNodes("//ALLOWEDVALUES"))
            {
                SortChildNodes(allowedValuesList);
            }

            // Sort child nodes of suggested values.
            foreach (XmlNode suggestedValuesList in normalizedXmlDefinition.SelectNodes("//SUGGESTEDVALUES"))
            {
                SortChildNodes(suggestedValuesList);
            }

            // Sort child nodes of prohibited values.
            foreach (XmlNode prohibitedValuesList in normalizedXmlDefinition.SelectNodes("//PROHIBITEDVALUES"))
            {
                SortChildNodes(prohibitedValuesList);
            }

            // Sort child nodes of transitions.
            foreach (XmlNode transitionList in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION"))
            {
                SortChildNodes(transitionList);
            }

            // Sort child nodes of transition reasons.
            foreach (XmlNode transitionReasonList in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/REASONS"))
            {
                SortChildNodes(transitionReasonList);
            }

            // Sort child nodes of transition actions.
            foreach (XmlNode transitionActionList in normalizedXmlDefinition.SelectNodes("//WORKITEMTYPE/WORKFLOW/TRANSITIONS/TRANSITION/ACTIONS"))
            {
                SortChildNodes(transitionActionList);
            }

            // Sort child nodes of links control options.
            foreach (XmlNode linksControlOptionList in normalizedXmlDefinition.SelectNodes("//LinksControlOptions"))
            {
                SortChildNodes(linksControlOptionList);
            }
        }

        #endregion

        #region NormalizeAgileConfiguration

        private static void NormalizeAgileConfiguration(XmlDocument normalizedXmlDefinition)
        {
            // Sort the root node's child nodes.
            SortChildNodes(normalizedXmlDefinition.DocumentElement);

            // Sort the child nodes of the iteration and product backlog nodes.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/AgileProjectConfiguration/IterationBacklog"));
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/AgileProjectConfiguration/ProductBacklog"));
        }

        #endregion

        #region NormalizeCommonConfiguration

        private static void NormalizeCommonConfiguration(XmlDocument normalizedXmlDefinition)
        {
            // Sort the root node's child nodes.
            SortChildNodes(normalizedXmlDefinition.DocumentElement);

            // Sort all type fields by refname.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/CommonProjectConfiguration/TypeFields"), n => GetValue(n.Attributes["refname"]));

            // Sort all type field values by type.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/CommonProjectConfiguration/TypeFields/TypeField/TypeFieldValues"), n => GetValue(n.Attributes["type"]));

            // Sort the child nodes of the weekends node.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/CommonProjectConfiguration/Weekends"));

            // Sort all states by type.
            foreach (XmlNode statesNode in normalizedXmlDefinition.SelectNodes("//States"))
            {
                SortChildNodes(statesNode, n => GetValue(n.Attributes["type"]));
            }
        }

        #endregion

        #region NormalizeProcessConfiguration

        private static void NormalizeProcessConfiguration(XmlDocument normalizedXmlDefinition)
        {
            var requirementBacklogNode = normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/RequirementBacklog");
            var taskBacklogNode = normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/TaskBacklog");

            // Add the "parent" attribute for well-known backlogs.
            if (requirementBacklogNode != null && requirementBacklogNode.Attributes["parent"] == null)
            {
                var parentAttribute = normalizedXmlDefinition.CreateAttribute("parent");
                parentAttribute.Value = "Microsoft.FeatureCategory";
                requirementBacklogNode.Attributes.Append(parentAttribute);
            }
            if (taskBacklogNode != null && taskBacklogNode.Attributes["parent"] == null)
            {
                var parentAttribute = normalizedXmlDefinition.CreateAttribute("parent");
                parentAttribute.Value = "Microsoft.RequirementCategory";
                taskBacklogNode.Attributes.Append(parentAttribute);
            }

            // Set the "workItemCountLimit" attribute to its default value on all backlogs.
            var workItemCountLimitAttributeName = "workItemCountLimit";
            var workItemCountLimitAttributeValue = "500";
            AddAttributeIfNeeded(requirementBacklogNode, workItemCountLimitAttributeName, workItemCountLimitAttributeValue);
            AddAttributeIfNeeded(taskBacklogNode, workItemCountLimitAttributeName, workItemCountLimitAttributeValue);
            foreach (XmlNode portfolioBacklogNode in normalizedXmlDefinition.SelectNodes("/ProjectProcessConfiguration/PortfolioBacklogs/PortfolioBacklog"))
            {
                AddAttributeIfNeeded(portfolioBacklogNode, workItemCountLimitAttributeName, workItemCountLimitAttributeValue);
            }

            // Sort the root node's child nodes.
            SortChildNodes(normalizedXmlDefinition.DocumentElement);

            // Sort the child nodes and attributes of the backlog nodes.
            foreach (XmlNode portfolioBacklogNode in normalizedXmlDefinition.SelectNodes("/ProjectProcessConfiguration/PortfolioBacklogs/PortfolioBacklog"))
            {
                SortChildNodes(portfolioBacklogNode);
            }
            SortChildNodes(requirementBacklogNode);
            SortAttributes(requirementBacklogNode);
            SortChildNodes(taskBacklogNode);
            SortAttributes(taskBacklogNode);

            // Sort all type fields by refname.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/TypeFields"), n => GetValue(n.Attributes["refname"]));

            // Sort all type field values by type.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/TypeFields/TypeField/TypeFieldValues"), n => GetValue(n.Attributes["type"]));

            // Sort the child nodes of the weekends node.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/Weekends"));

            // Sort all states by type.
            foreach (XmlNode statesNode in normalizedXmlDefinition.SelectNodes("//States"))
            {
                SortChildNodes(statesNode, n => GetValue(n.Attributes["type"]));
            }

            // Sort all work item colors by name.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/WorkItemColors"), n => GetValue(n.Attributes["name"]));

            // Sort all properties by name.
            SortChildNodes(normalizedXmlDefinition.SelectSingleNode("/ProjectProcessConfiguration/Properties"), n => GetValue(n.Attributes["name"]));
        }

        #endregion

        #region NormalizeCategories

        private static void NormalizeCategories(XmlDocument normalizedXmlDefinition)
        {
            // Sort the root node's child nodes by refname.
            SortChildNodes(normalizedXmlDefinition.DocumentElement, n => GetValue(n.Attributes["refname"]));

            // Sort the child nodes of categories.
            foreach (XmlNode categoryNode in normalizedXmlDefinition.SelectNodes("//CATEGORY"))
            {
                SortChildNodes(categoryNode);
            }
        }

        #endregion

        #region Helper Methods

        private static void SetAttributeIfNeeded(XmlNode field, string refname, IEnumerable<string> refnameLookupList, string attributeName, string attributeValue)
        {
            if (refnameLookupList.Any(r => refname.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
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

        private static void AddElementIfNeeded(XmlNode node, string elementName, Predicate<XmlNode> predicateOnParentNode)
        {
            if (node != null && predicateOnParentNode(node))
            {
                var existingElement = node.SelectSingleNode(elementName);
                if (existingElement == null)
                {
                    var childNode = node.OwnerDocument.CreateElement(elementName);
                    node.AppendChild(childNode);
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
            if (node != null)
            {
                foreach (var nodeElement in node.ChildNodes.Cast<XmlNode>().OrderBy(n => identitySelector == null ? n.OuterXml : identitySelector(n)).ToList())
                {
                    node.RemoveChild(nodeElement);
                    node.AppendChild(nodeElement);
                }
            }
        }

        private static void SortAttributes(XmlNode node)
        {
            if (node != null)
            {
                foreach (var nodeAttribute in node.Attributes.Cast<XmlAttribute>().OrderBy(a => a.Name).ToList())
                {
                    node.Attributes.Remove(nodeAttribute);
                    node.Attributes.Append(nodeAttribute);
                }
            }
        }

        private static void AddAttributeIfNeeded(XmlNode node, string attributeName, string attributeValue)
        {
            if (node != null && node.Attributes[attributeName] == null)
            {
                var attribute = node.OwnerDocument.CreateAttribute(attributeName);
                attribute.Value = attributeValue;
                node.Attributes.Append(attribute);
            }
        }

        public static string GetValue(XmlNode node)
        {
            return (node == null ? null : node is XmlAttribute ? node.Value : node.OuterXml);
        }

        #endregion
    }
}