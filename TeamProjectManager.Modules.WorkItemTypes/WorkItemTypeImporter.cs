using System;
using System.Collections.Generic;
using System.Xml.Schema;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public static class WorkItemTypeImporter
    {
        #region ImportWorkItemTypes

        public static void ImportWorkItemTypes(ApplicationTask task, ImportOptions options, WorkItemStore store, Dictionary<TeamProjectInfo, List<WorkItemTypeDefinition>> teamProjectsWithWorkItemTypes)
        {
            var step = 0;
            var importValidationFailed = false;
            ImportEventHandler importEventHandler = (sender, e) =>
            {
                if (e.Severity == ImportSeverity.Error)
                {
                    importValidationFailed = true;
                    var message = e.Message;
                    var schemaValidationException = e.Exception as XmlSchemaValidationException;
                    if (schemaValidationException != null)
                    {
                        message = string.Format("ERROR - XML validation error at row {0}, column {1}: {2}", schemaValidationException.LineNumber, schemaValidationException.LinePosition, message);
                    }
                    task.SetError(message);
                }
                else if (e.Severity == ImportSeverity.Warning)
                {
                    task.SetWarning(e.Message);
                }
            };

            // Validate.
            if (options.HasFlag(ImportOptions.Validate))
            {
                WorkItemType.ValidationEventHandler += importEventHandler;
                foreach (var teamProjectWithWorkItemTypes in teamProjectsWithWorkItemTypes)
                {
                    var project = store.Projects[teamProjectWithWorkItemTypes.Key.Name];
                    foreach (var workItemTypeFile in teamProjectWithWorkItemTypes.Value)
                    {
                        task.SetProgress(step++, string.Format("Validating work item type \"{0}\" in project \"{1}\"", workItemTypeFile.Name, teamProjectWithWorkItemTypes.Key.Name));
                        try
                        {
                            WorkItemType.Validate(project, workItemTypeFile.XmlDefinition.OuterXml);
                        }
                        catch (Exception exc)
                        {
                            task.SetError("ERROR - " + exc.Message);
                        }
                    }
                }
                WorkItemType.ValidationEventHandler -= importEventHandler;
            }

            // Import.
            if (!importValidationFailed && options.HasFlag(ImportOptions.Import))
            {
                foreach (var teamProjectWithWorkItemTypes in teamProjectsWithWorkItemTypes)
                {
                    var project = store.Projects[teamProjectWithWorkItemTypes.Key.Name];
                    project.WorkItemTypes.ImportEventHandler += importEventHandler;
                    foreach (var workItemTypeFile in teamProjectWithWorkItemTypes.Value)
                    {
                        task.SetProgress(step++, string.Format("Importing work item type \"{0}\" in project \"{1}\"", workItemTypeFile.Name, teamProjectWithWorkItemTypes.Key.Name));
                        try
                        {
                            project.WorkItemTypes.Import(workItemTypeFile.XmlDefinition.OuterXml);
                        }
                        catch (Exception exc)
                        {
                            task.SetError("ERROR - " + exc.Message);
                        }
                    }
                    project.WorkItemTypes.ImportEventHandler -= importEventHandler;
                }
            }
        }

        #endregion
    }
}