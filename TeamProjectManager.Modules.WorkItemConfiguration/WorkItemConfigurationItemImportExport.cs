using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.ProcessConfiguration.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Schema;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public static class WorkItemConfigurationItemImportExport
    {
        private static string productName = string.Empty;
        private static string ProductName
        {
            get
            {
                Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    if ((customAttributes != null) && (customAttributes.Length > 0))
                    {
                        productName = ((AssemblyProductAttribute)customAttributes[0]).Product;
                    }
                    if (string.IsNullOrEmpty(productName))
                    {
                        productName = string.Empty;
                    }
                }
                return productName;
            }
        }

        #region Import

        public static void Import(ILogger logger, ApplicationTask task, bool setTaskProgress, WorkItemStore store, Dictionary<TeamProjectInfo, List<WorkItemConfigurationItem>> teamProjectsWithConfigurationItems, ImportOptions options)
        {
            // Replace any macros.
            if (!task.IsCanceled)
            {
                foreach (var teamProject in teamProjectsWithConfigurationItems.Keys.ToArray())
                {
                    var transformedList = new List<WorkItemConfigurationItem>();
                    foreach (var workItemConfigurationItem in teamProjectsWithConfigurationItems[teamProject])
                    {
                        // Clone the item so that any callers aren't affected by a changed XML definitions.
                        var clone = workItemConfigurationItem.Clone();
                        ReplaceTeamProjectMacros(clone.XmlDefinition, teamProject);
                        transformedList.Add(clone);
                    }
                    teamProjectsWithConfigurationItems[teamProject] = transformedList;
                }
            }

            // Save a temporary copy for troubleshooting if requested.
            if (!task.IsCanceled && options.HasFlag(ImportOptions.SaveCopy))
            {
                foreach (var teamProjectWithConfigurationItems in teamProjectsWithConfigurationItems)
                {
                    var teamProject = teamProjectWithConfigurationItems.Key;
                    foreach (var workItemConfigurationItem in teamProjectWithConfigurationItems.Value)
                    {
                        var directoryName = Path.Combine(Path.GetTempPath(), WorkItemConfigurationItemImportExport.ProductName, teamProject.Name);
                        Directory.CreateDirectory(directoryName);
                        var fileName = Path.Combine(directoryName, workItemConfigurationItem.Name + ".xml");
                        using (var writer = XmlWriter.Create(fileName, new XmlWriterSettings { Indent = true }))
                        {
                            workItemConfigurationItem.XmlDefinition.WriteTo(writer);
                        }
                        var message = "{0} for Team Project \"{1}\" was saved to \"{2}\"".FormatCurrent(workItemConfigurationItem.DisplayName, teamProject.Name, fileName);
                        logger.Log(message, TraceEventType.Verbose);
                        task.Status = message;
                    }
                }
            }

            var step = 0;
            foreach (var teamProjectWithConfigurationItems in teamProjectsWithConfigurationItems)
            {
                var teamProject = teamProjectWithConfigurationItems.Key;
                var configurationItems = teamProjectWithConfigurationItems.Value;
                var project = store.Projects[teamProjectWithConfigurationItems.Key.Name];

                // First apply all work item types in batch.
                var workItemTypes = configurationItems.Where(t => t.Type == WorkItemConfigurationItemType.WorkItemType).Cast<WorkItemTypeDefinition>().ToList();
                if (workItemTypes.Any())
                {
                    var teamProjectsWithWorkItemTypes = new Dictionary<TeamProjectInfo, List<WorkItemTypeDefinition>>() { { teamProject, workItemTypes } };
                    ImportWorkItemTypes(logger, task, setTaskProgress, ref step, options, store, teamProjectsWithWorkItemTypes);
                }

                // Then apply the other configuration items.
                foreach (var configurationItem in configurationItems.Where(w => w.Type != WorkItemConfigurationItemType.WorkItemType))
                {
                    if (options.HasFlag(ImportOptions.Simulate))
                    {
                        var status = "Simulating import of {0} in Team Project \"{1}\"".FormatCurrent(configurationItem.DisplayName, teamProject.Name);
                        if (setTaskProgress)
                        {
                            task.SetProgress(step++, status);
                        }
                        else
                        {
                            task.Status = status;
                        }
                    }
                    else
                    {
                        var status = "Importing {0} in Team Project \"{1}\"".FormatCurrent(configurationItem.DisplayName, teamProject.Name);
                        if (setTaskProgress)
                        {
                            task.SetProgress(step++, status);
                        }
                        else
                        {
                            task.Status = status;
                        }
                        try
                        {
                            switch (configurationItem.Type)
                            {
                                case WorkItemConfigurationItemType.Categories:
                                    SetCategories(project, configurationItem);
                                    break;
                                case WorkItemConfigurationItemType.CommonConfiguration:
                                    SetCommonConfiguration(project, configurationItem);
                                    break;
                                case WorkItemConfigurationItemType.AgileConfiguration:
                                    SetAgileConfiguration(project, configurationItem);
                                    break;
                                case WorkItemConfigurationItemType.ProcessConfiguration:
                                    SetProcessConfiguration(project, configurationItem);
                                    break;
                                default:
                                    throw new ArgumentException("The Work Item Configuration Item Type is unknown: " + configurationItem.Type.ToString());
                            }
                        }
                        catch (Exception exc)
                        {
                            var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while importing {0} in Team Project \"{1}\"", configurationItem.DisplayName, teamProjectWithConfigurationItems.Key.Name);
                            logger.Log(message, exc);
                            task.SetError(message, exc);
                        }
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

        #region Export

        public static void Export(ILogger logger, ApplicationTask task, IList<WorkItemConfigurationItemExport> workItemConfigurationItems)
        {
            if (workItemConfigurationItems.Count > 0)
            {
                var step = 0;
                foreach (var workItemConfigurationItem in workItemConfigurationItems)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Exporting {0} from Team Project \"{1}\"", workItemConfigurationItem.Item.DisplayName, workItemConfigurationItem.TeamProject.Name));
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
                        var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while exporting {0}", workItemConfigurationItem.Item.DisplayName);
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

        #region Get & Set Categories

        public static XmlDocument GetCategoriesXml(Project project)
        {
            return project.Categories.Export();
        }

        public static WorkItemConfigurationItem GetCategories(Project project)
        {
            var categoriesXml = GetCategoriesXml(project);
            return WorkItemConfigurationItem.FromXml(categoriesXml);
        }

        public static void SetCategories(Project project, WorkItemConfigurationItem categories)
        {
            SetCategories(project, categories.XmlDefinition);
        }

        public static void SetCategories(Project project, XmlDocument categories)
        {
            project.Categories.Import(categories.DocumentElement);
        }

        #endregion

        #region Get & Set Process Configuration

        public static WorkItemConfigurationItem GetCommonConfiguration(Project project)
        {
            return GetProcessConfiguration(project, WorkItemConfigurationItemType.CommonConfiguration);
        }

        public static void SetCommonConfiguration(Project project, WorkItemConfigurationItem commonConfiguration)
        {
            SetProcessConfiguration(project, WorkItemConfigurationItemType.CommonConfiguration, commonConfiguration);
        }

        public static WorkItemConfigurationItem GetAgileConfiguration(Project project)
        {
            return GetProcessConfiguration(project, WorkItemConfigurationItemType.AgileConfiguration);
        }

        public static void SetAgileConfiguration(Project project, WorkItemConfigurationItem agileConfiguration)
        {
            SetProcessConfiguration(project, WorkItemConfigurationItemType.AgileConfiguration, agileConfiguration);
        }

        public static WorkItemConfigurationItem GetProcessConfiguration(Project project)
        {
            return GetProcessConfiguration(project, WorkItemConfigurationItemType.ProcessConfiguration);
        }

        public static void SetProcessConfiguration(Project project, WorkItemConfigurationItem processConfiguration)
        {
            SetProcessConfiguration(project, WorkItemConfigurationItemType.ProcessConfiguration, processConfiguration);
        }

        #endregion

        #region Macro Support

        private const string MacroNameProjectName = "$$PROJECTNAME$$";
        private const string MacroNameProjectCollectionName = "$$PROJECTCOLLECTIONNAME$$";
        private const string MacroNameProjectCollectionUrl = "$$PROJECTCOLLECTIONURL$$";
        private const string MacroNameServerName = "$$SERVERNAME$$";
        private const string MacroNameServerUrl = "$$SERVERURL$$";

        public static IList<MacroDefinition> GetSupportedMacroDefinitions()
        {
            return new List<MacroDefinition> {
                new MacroDefinition(MacroNameProjectName, "FabrikamFiber", "The name of the Team Project"),
                new MacroDefinition(MacroNameProjectCollectionName, @"tfs\FabrikamFiberCollection", "The name of the Team Project Collection"),
                new MacroDefinition(MacroNameProjectCollectionUrl, "http://tfs:8080/tfs/fabrikamfibercollection", "The URL of the Team Project Collection"),
                new MacroDefinition(MacroNameServerName, "tfs", "The name of the Team Foundation Server"),
                new MacroDefinition(MacroNameServerUrl, "http://tfs:8080/tfs", "The URL of the Team Foundation Server")
            };
        }

        public static IDictionary<string, string> GetTeamProjectMacros(TeamProjectInfo teamProject)
        {
            if (teamProject == null)
            {
                return new Dictionary<string, string>();
            }
            else
            {
                return new Dictionary<string, string>() {
                    { MacroNameProjectName, teamProject.Name },
                    { MacroNameProjectCollectionName, teamProject.TeamProjectCollection.Name },
                    { MacroNameProjectCollectionUrl, teamProject.TeamProjectCollection.Uri.ToString() },
                    { MacroNameServerName, teamProject.TeamProjectCollection.TeamFoundationServer.Name },
                    { MacroNameServerUrl, teamProject.TeamProjectCollection.TeamFoundationServer.Uri.ToString() }
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

        private static void ImportWorkItemTypes(ILogger logger, ApplicationTask task, bool setTaskProgress, ref int step, ImportOptions options, WorkItemStore store, Dictionary<TeamProjectInfo, List<WorkItemTypeDefinition>> teamProjectsWithWorkItemTypes)
        {
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

            // Validate.
            if (!task.IsCanceled)
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
                            task.Status = string.Format("Validating {0} for Team Project \"{1}\"", workItemTypeFile.DisplayName, teamProject.Name);
                            try
                            {
                                WorkItemType.Validate(project, workItemTypeFile.XmlDefinition.OuterXml);
                            }
                            catch (Exception exc)
                            {
                                var message = string.Format("An error occurred while validating {0} for Team Project \"{1}\"", workItemTypeFile.DisplayName, teamProject.Name);
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
            if (!task.IsCanceled && !importValidationFailed)
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
                            if (options.HasFlag(ImportOptions.Simulate))
                            {
                                var status = string.Format("Simulating import of {0} in Team Project \"{1}\"", workItemTypeFile.DisplayName, teamProject.Name);
                                if (setTaskProgress)
                                {
                                    task.SetProgress(step++, status);
                                }
                                else
                                {
                                    task.Status = status;
                                }
                            }
                            else
                            {
                                var status = string.Format("Importing {0} in Team Project \"{1}\"", workItemTypeFile.DisplayName, teamProject.Name);
                                if (setTaskProgress)
                                {
                                    task.SetProgress(step++, status);
                                }
                                else
                                {
                                    task.Status = status;
                                }
                                try
                                {
                                    project.WorkItemTypes.Import(workItemTypeFile.XmlDefinition.DocumentElement);
                                }
                                catch (Exception exc)
                                {
                                    var message = string.Format("An error occurred while importing {0} in Team Project \"{1}\"", workItemTypeFile.DisplayName, teamProject.Name);
                                    logger.Log(message, exc);
                                    task.SetError(message, exc);
                                }
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

        private static WorkItemConfigurationItem GetProcessConfiguration(Project project, WorkItemConfigurationItemType type)
        {
            var configService = project.Store.TeamProjectCollection.GetService<ProjectProcessConfigurationService>();
            string processConfigXml;
            try
            {
                if (type == WorkItemConfigurationItemType.CommonConfiguration)
                {
                    var commonConfig = configService.GetCommonConfiguration(project.Uri.ToString());
                    var configXml = new StringBuilder();
                    using (var writer = XmlWriter.Create(configXml, new XmlWriterSettings { Indent = true }))
                    {
                        commonConfig.ToXml(writer, WorkItemConfigurationItem.CommonConfigurationXmlElementName);
                    }
                    processConfigXml = configXml.ToString();
                }
                else if (type == WorkItemConfigurationItemType.AgileConfiguration)
                {
                    var agileConfig = configService.GetAgileConfiguration(project.Uri.ToString());
                    var configXml = new StringBuilder();
                    using (var writer = XmlWriter.Create(configXml, new XmlWriterSettings { Indent = true }))
                    {
                        agileConfig.ToXml(writer, WorkItemConfigurationItem.AgileConfigurationXmlElementName);
                    }
                    processConfigXml = configXml.ToString();
                }
                else if (type == WorkItemConfigurationItemType.ProcessConfiguration)
                {
                    var processConfig = configService.GetProcessConfiguration(project.Uri.ToString());
                    var configXml = new StringBuilder();
                    using (var writer = XmlWriter.Create(configXml, new XmlWriterSettings { Indent = true }))
                    {
                        processConfig.ToXml(writer, WorkItemConfigurationItem.ProcessConfigurationXmlElementName);
                    }
                    processConfigXml = configXml.ToString();
                }
                else
                {
                    throw new ArgumentException("The type argument must be either a CommonConfiguration, AgileConfiguration or ProcessConfiguration.");
                }
            }
            catch (NullReferenceException)
            {
                // Working with the ProjectProcessConfigurationService throws NullReferenceException on TFS 2010 or earlier.
                return null;
            }
            catch (SoapException)
            {
                // A Team Project that doesn't have a process configuration yet throws a SOAP exception
                // "TF400572: The Project Process Settings must be configured for this feature to be used".
                return null;
            }
            return WorkItemConfigurationItem.FromXml(processConfigXml);
        }

        private static void SetProcessConfiguration(Project project, WorkItemConfigurationItemType type, WorkItemConfigurationItem item)
        {
            var tfs = project.Store.TeamProjectCollection;
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
                else if (type == WorkItemConfigurationItemType.ProcessConfiguration)
                {
                    ProjectProcessConfiguration processConfig;
                    using (var xmlStringReader = new StringReader(item.XmlDefinition.DocumentElement.OuterXml))
                    using (var xmlReader = XmlReader.Create(xmlStringReader))
                    {
                        while (xmlReader.NodeType != XmlNodeType.Element)
                        {
                            xmlReader.Read();
                        }
                        processConfig = ProjectProcessConfiguration.FromXml(tfs, xmlReader);
                    }
                    configService.SetProcessConfiguration(project.Uri.ToString(), processConfig);
                }
                else
                {
                    throw new ArgumentException("The type argument must be either a CommonConfiguration, AgileConfiguration or ProcessConfiguration.");
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