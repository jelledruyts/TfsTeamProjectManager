using System;
using System.Collections.Generic;
using System.Xml.Schema;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public class WorkItemTypeImporter
    {
        #region Fields

        private ApplicationTask task;
        private bool importValidationFailed;

        #endregion

        #region ImportWorkItemTypes

        public void ImportWorkItemTypes(ApplicationTask task, ImportOptions options, Uri projectCollectionUri, ICollection<string> teamProjectNames, ICollection<WorkItemTypeDefinition> workItemTypeFiles)
        {
            this.task = task;

            var step = 0;
            using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(projectCollectionUri))
            {
                var store = tfs.GetService<WorkItemStore>();

                // Validate.
                if (options.HasFlag(ImportOptions.Validate))
                {
                    WorkItemType.ValidationEventHandler += ImportEventHandler;
                    foreach (var teamProjectName in teamProjectNames)
                    {
                        var project = store.Projects[teamProjectName];
                        foreach (var workItemTypeFile in workItemTypeFiles)
                        {
                            task.SetProgress(step++, string.Format("Validating work item type \"{0}\" in project \"{1}\"", workItemTypeFile.Name, teamProjectName));
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
                    WorkItemType.ValidationEventHandler -= ImportEventHandler;
                }

                // Import.
                if (!this.importValidationFailed && options.HasFlag(ImportOptions.Import))
                {
                    foreach (var teamProjectName in teamProjectNames)
                    {
                        var project = store.Projects[teamProjectName];
                        project.WorkItemTypes.ImportEventHandler += ImportEventHandler;
                        foreach (var workItemTypeFile in workItemTypeFiles)
                        {
                            task.SetProgress(step++, string.Format("Importing work item type \"{0}\" in project \"{1}\"", workItemTypeFile.Name, teamProjectName));
                            try
                            {
                                project.WorkItemTypes.Import(workItemTypeFile.XmlDefinition.OuterXml);
                            }
                            catch (Exception exc)
                            {
                                task.SetError("ERROR - " + exc.Message);
                            }
                        }
                        project.WorkItemTypes.ImportEventHandler -= ImportEventHandler;
                    }
                }
            }
        }

        private void ImportEventHandler(object sender, ImportEventArgs e)
        {
            if (e.Severity == ImportSeverity.Error)
            {
                this.importValidationFailed = true;
                var message = e.Message;
                var schemaValidationException = e.Exception as XmlSchemaValidationException;
                if (schemaValidationException != null)
                {
                    message = string.Format("ERROR - XML validation error at row {0}, column {1}: {2}", schemaValidationException.LineNumber, schemaValidationException.LinePosition, message);
                }
                this.task.SetError(message);
            }
            else if (e.Severity == ImportSeverity.Warning)
            {
                this.task.SetWarning(e.Message);
            }
        }

        #endregion
    }
}