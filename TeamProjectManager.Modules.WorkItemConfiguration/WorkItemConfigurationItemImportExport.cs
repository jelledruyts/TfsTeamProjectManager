using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.ProcessConfiguration.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public static class WorkItemConfigurationItemImportExport
    {
        #region ImportWorkItemTypes

        public static void ImportWorkItemTypes(ILogger logger, ApplicationTask task, ImportOptions options, WorkItemStore store, Dictionary<TeamProjectInfo, List<WorkItemTypeDefinition>> teamProjectsWithWorkItemTypes)
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
                        message = string.Format("XML validation error at row {0}, column {1}: {2}", schemaValidationException.LineNumber, schemaValidationException.LinePosition, message);
                    }
                    task.SetError(message);
                }
                else if (e.Severity == ImportSeverity.Warning)
                {
                    task.SetWarning(e.Message);
                }
            };

            // Replace any macros.
            // TODO: Make macros discoverable in UI
            if (!task.IsCanceled)
            {
                foreach (var teamProjectWithWorkItemTypes in teamProjectsWithWorkItemTypes)
                {
                    var teamProject = teamProjectWithWorkItemTypes.Key;
                    var workItemTypeList = teamProjectWithWorkItemTypes.Value;
                    foreach (var workItemTypeFile in workItemTypeList.ToArray())
                    {
                        // Clone the work item type definition so that any callers aren't affected by changed XML definitions.
                        var clone = (WorkItemTypeDefinition)workItemTypeFile.Clone();
                        ReplaceTeamProjectMacros(clone.XmlDefinition, teamProject);
                        workItemTypeList.Remove(workItemTypeFile);
                        workItemTypeList.Add(clone);
                    }
                }
            }

            // Save a temporary copy for troubleshooting if requested.
            // TODO: Properly support SaveCopy in the UI
            if (!task.IsCanceled && options.HasFlag(ImportOptions.SaveCopy))
            {
                foreach (var teamProjectWithWorkItemTypes in teamProjectsWithWorkItemTypes)
                {
                    var teamProject = teamProjectWithWorkItemTypes.Key;
                    foreach (var workItemTypeFile in teamProjectWithWorkItemTypes.Value)
                    {
                        var directoryName = Path.Combine(Path.GetTempPath(), teamProject.Name);
                        Directory.CreateDirectory(directoryName);
                        var fileName = Path.Combine(directoryName, workItemTypeFile.Name + ".xml");
                        using (var writer = XmlWriter.Create(fileName, new XmlWriterSettings { Indent = true }))
                        {
                            workItemTypeFile.XmlDefinition.WriteTo(writer);
                        }
                        var message = "Work item type \"{0}\" for Team Project \"{1}\" was saved to \"{2}\"".FormatCurrent(workItemTypeFile.Name, teamProject.Name, fileName);
                        logger.Log(message, TraceEventType.Verbose);
                        task.Status = message;
                    }
                }
            }

            // Validate.
            if (!task.IsCanceled && options.HasFlag(ImportOptions.Validate))
            {
                WorkItemType.ValidationEventHandler += importEventHandler;
                try
                {
                    foreach (var teamProjectWithWorkItemTypes in teamProjectsWithWorkItemTypes)
                    {
                        var teamProject = teamProjectWithWorkItemTypes.Key;
                        var project = store.Projects[teamProject.Name];
                        foreach (var workItemTypeFile in teamProjectWithWorkItemTypes.Value)
                        {
                            task.SetProgress(step++, string.Format("Validating work item type \"{0}\" for Team Project \"{1}\"", workItemTypeFile.Name, teamProject.Name));
                            try
                            {
                                WorkItemType.Validate(project, workItemTypeFile.XmlDefinition.OuterXml);
                            }
                            catch (Exception exc)
                            {
                                var message = string.Format("An error occurred while validating work item type \"{0}\" for Team Project \"{1}\"", workItemTypeFile.Name, teamProject.Name);
                                logger.Log(message, exc);
                                task.SetError(message, exc);
                            }
                            if (task.IsCanceled)
                            {
                                break;
                            }
                        }
                        if (task.IsCanceled)
                        {
                            task.Status = "Canceled";
                            break;
                        }
                    }
                }
                finally
                {
                    WorkItemType.ValidationEventHandler -= importEventHandler;
                }
            }

            // Import.
            if (!task.IsCanceled && !importValidationFailed && options.HasFlag(ImportOptions.Import))
            {
                foreach (var teamProjectWithWorkItemTypes in teamProjectsWithWorkItemTypes)
                {
                    var teamProject = teamProjectWithWorkItemTypes.Key;
                    var project = store.Projects[teamProject.Name];
                    project.WorkItemTypes.ImportEventHandler += importEventHandler;
                    try
                    {
                        foreach (var workItemTypeFile in teamProjectWithWorkItemTypes.Value)
                        {
                            task.SetProgress(step++, string.Format("Importing work item type \"{0}\" in Team Project \"{1}\"", workItemTypeFile.Name, teamProject.Name));
                            try
                            {
                                project.WorkItemTypes.Import(workItemTypeFile.XmlDefinition.DocumentElement);
                            }
                            catch (Exception exc)
                            {
                                var message = string.Format("An error occurred while importing work item type \"{0}\" in Team Project \"{1}\"", workItemTypeFile.Name, teamProject.Name);
                                logger.Log(message, exc);
                                task.SetError(message, exc);
                            }
                            if (task.IsCanceled)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        project.WorkItemTypes.ImportEventHandler -= importEventHandler;
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
            }
        }

        #endregion

        #region ImportProcessConfigurations

        public static void ImportProcessConfigurations(ILogger logger, ApplicationTask task, TfsTeamProjectCollection tfs, WorkItemStore store, Dictionary<TeamProjectInfo, List<WorkItemConfigurationItem>> teamProjectsWithProcessConfigurations)
        {
            var step = 0;
            foreach (var teamProjectWithProcessConfigurations in teamProjectsWithProcessConfigurations)
            {
                var project = store.Projects[teamProjectWithProcessConfigurations.Key.Name];
                foreach (var processConfiguration in teamProjectWithProcessConfigurations.Value)
                {
                    task.SetProgress(step++, string.Format("Importing {0} in Team Project \"{1}\"", processConfiguration.Name, teamProjectWithProcessConfigurations.Key.Name));
                    try
                    {
                        if (processConfiguration.Type == WorkItemConfigurationItemType.CommonConfiguration)
                        {
                            SetCommonConfiguration(tfs, project, processConfiguration);
                        }
                        else if (processConfiguration.Type == WorkItemConfigurationItemType.AgileConfiguration)
                        {
                            SetAgileConfiguration(tfs, project, processConfiguration);
                        }
                        else
                        {
                            throw new ArgumentException("The process configuration item must be either a CommonConfiguration or AgileConfiguration.");
                        }
                    }
                    catch (Exception exc)
                    {
                        var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while importing {0} in Team Project \"{1}\"", processConfiguration.Name, teamProjectWithProcessConfigurations.Key.Name);
                        logger.Log(message, exc);
                        task.SetError(message, exc);
                    }
                }
                if (task.IsCanceled)
                {
                    task.Status = "Canceled";
                    break;
                }
            }
        }

        #endregion

        #region ExportWorkItemConfigurationItems

        public static void ExportWorkItemConfigurationItems(ILogger logger, ApplicationTask task, string itemType, IList<WorkItemConfigurationItemExport> workItemConfigurationItems)
        {
            if (workItemConfigurationItems.Count > 0)
            {
                var step = 0;
                foreach (var workItemConfigurationItem in workItemConfigurationItems)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Exporting {0} \"{1}\" from Team Project \"{2}\"", itemType, workItemConfigurationItem.Item.Name, workItemConfigurationItem.TeamProject.Name));
                    try
                    {
                        if (!string.IsNullOrEmpty(workItemConfigurationItem.SaveAsFileName))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(workItemConfigurationItem.SaveAsFileName));
                            workItemConfigurationItem.Item.XmlDefinition.Save(workItemConfigurationItem.SaveAsFileName);
                        }
                    }
                    catch (Exception exc)
                    {
                        var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while exporting {0} \"{1}\"", itemType, workItemConfigurationItem.Item.Name);
                        logger.Log(message, exc);
                        task.SetError(message, exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
            }
        }

        #endregion

        #region Get & Set CommonConfiguration

        public static WorkItemConfigurationItem GetCommonConfiguration(TfsTeamProjectCollection tfs, Project project)
        {
            return GetProcessConfiguration(tfs, project, WorkItemConfigurationItemType.CommonConfiguration);
        }

        public static void SetCommonConfiguration(TfsTeamProjectCollection tfs, Project project, WorkItemConfigurationItem commonConfiguration)
        {
            SetProcessConfiguration(tfs, project, WorkItemConfigurationItemType.CommonConfiguration, commonConfiguration);
        }

        public static WorkItemConfigurationItem GetAgileConfiguration(TfsTeamProjectCollection tfs, Project project)
        {
            return GetProcessConfiguration(tfs, project, WorkItemConfigurationItemType.AgileConfiguration);
        }

        public static void SetAgileConfiguration(TfsTeamProjectCollection tfs, Project project, WorkItemConfigurationItem agileConfiguration)
        {
            SetProcessConfiguration(tfs, project, WorkItemConfigurationItemType.AgileConfiguration, agileConfiguration);
        }

        #endregion

        #region Macro Support

        public static IDictionary<string, string> GetTeamProjectMacros(TeamProjectInfo teamProject)
        {
            if (teamProject == null)
            {
                return new Dictionary<string, string>();
            }
            else
            {
                return new Dictionary<string, string>() {
                    { "$$PROJECTNAME$$", teamProject.Name },
                    { "$$PROJECTURL$$", teamProject.Uri.ToString() },
                    { "$$COLLECTIONNAME$$", teamProject.TeamProjectCollection.Name },
                    { "$$COLLECTIONURL$$", teamProject.TeamProjectCollection.Uri.ToString() },
                    { "$$SERVERNAME$$", teamProject.TeamProjectCollection.TeamFoundationServer.Name },
                    { "$$SERVERURL$$", teamProject.TeamProjectCollection.TeamFoundationServer.Uri.ToString() }
                };
            }
        }

        public static void ReplaceTeamProjectMacros(XmlDocument xml, TeamProjectInfo teamProject)
        {
            ReplaceTeamProjectMacros(xml, GetTeamProjectMacros(teamProject));
        }

        public static void ReplaceTeamProjectMacros(XmlDocument xml, IDictionary<string, string> macros)
        {
            if (xml != null)
            {
                var resultXml = ReplaceTeamProjectMacros(xml.OuterXml, macros);
                xml.LoadXml(resultXml);
            }
        }

        public static string ReplaceTeamProjectMacros(string value, TeamProjectInfo teamProject)
        {
            return ReplaceTeamProjectMacros(value, GetTeamProjectMacros(teamProject));
        }

        public static string ReplaceTeamProjectMacros(string value, IDictionary<string, string> macros)
        {
            if (!string.IsNullOrEmpty(value) && macros != null && macros.Any())
            {
                foreach (var macro in macros)
                {
                    value = value.Replace(macro.Key, macro.Value);
                }
            }
            return value;
        }

        #endregion

        #region Helper Methods

        private static WorkItemConfigurationItem GetProcessConfiguration(TfsTeamProjectCollection tfs, Project project, WorkItemConfigurationItemType type)
        {
            var configService = tfs.GetService<ProjectProcessConfigurationService>();
            string processConfigXml;
            try
            {
                if (type == WorkItemConfigurationItemType.CommonConfiguration)
                {
                    var commonConfig = configService.GetCommonConfiguration(project.Uri.ToString());
                    var configXml = new StringBuilder();
                    using (var writer = XmlWriter.Create(configXml, new XmlWriterSettings { Indent = true }))
                    {
                        commonConfig.ToXml(writer, "CommonProjectConfiguration");
                    }
                    processConfigXml = configXml.ToString();
                }
                else if (type == WorkItemConfigurationItemType.AgileConfiguration)
                {
                    var agileConfig = configService.GetAgileConfiguration(project.Uri.ToString());
                    var configXml = new StringBuilder();
                    using (var writer = XmlWriter.Create(configXml, new XmlWriterSettings { Indent = true }))
                    {
                        agileConfig.ToXml(writer, "AgileProjectConfiguration");
                    }
                    processConfigXml = configXml.ToString();
                }
                else
                {
                    throw new ArgumentException("The type argument must be either a CommonConfiguration or AgileConfiguration.");
                }
            }
            catch (NullReferenceException)
            {
                // Working with the ProjectProcessConfigurationService throws NullReferenceException on TFS 2010 or earlier.
                return null;
            }
            return WorkItemConfigurationItem.FromXml(processConfigXml);
        }

        private static void SetProcessConfiguration(TfsTeamProjectCollection tfs, Project project, WorkItemConfigurationItemType type, WorkItemConfigurationItem item)
        {
            var configService = tfs.GetService<ProjectProcessConfigurationService>();
            try
            {
                if (type == WorkItemConfigurationItemType.CommonConfiguration)
                {
                    CommonProjectConfiguration commonConfig;
                    using (var xmlStringReader = new StringReader(item.XmlDefinition.DocumentElement.OuterXml))
                    using (var xmlReader = XmlReader.Create(xmlStringReader))
                    {
                        while (xmlReader.NodeType != XmlNodeType.Element)
                        {
                            xmlReader.Read();
                        }
                        commonConfig = CommonProjectConfiguration.FromXml(tfs, xmlReader);
                    }
                    configService.SetCommonConfiguration(project.Uri.ToString(), commonConfig);
                }
                else if (type == WorkItemConfigurationItemType.AgileConfiguration)
                {
                    AgileProjectConfiguration agileConfig;
                    using (var xmlStringReader = new StringReader(item.XmlDefinition.DocumentElement.OuterXml))
                    using (var xmlReader = XmlReader.Create(xmlStringReader))
                    {
                        while (xmlReader.NodeType != XmlNodeType.Element)
                        {
                            xmlReader.Read();
                        }
                        agileConfig = AgileProjectConfiguration.FromXml(tfs, xmlReader);
                    }
                    configService.SetAgileConfiguration(project.Uri.ToString(), agileConfig);
                }
                else
                {
                    throw new ArgumentException("The type argument must be either a CommonConfiguration or AgileConfiguration.");
                }
            }
            catch (NullReferenceException exc)
            {
                // Working with the ProjectProcessConfigurationService throws NullReferenceException on TFS 2010 or earlier.
                throw new InvalidOperationException("The process configuration could not be saved.", exc);
            }
        }

        #endregion
    }
}