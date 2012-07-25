using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.Build.Client;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.BuildDefinitions
{
    public class BuildDefinitionInfoUpdate : BuildDefinitionInfo
    {
        #region Properties

        public bool UpdateBuildControllerName { get; set; }
        public bool UpdateDefaultDropLocation { get; set; }
        public bool UpdateTriggerType { get; set; }
        public bool UpdateQueueStatus { get; set; }
        public bool UpdateProcessTemplate { get; set; }
        public bool UpdateBuildNumberFormat { get; set; }
        public bool UpdateCleanWorkspace { get; set; }
        public bool UpdateVerbosity { get; set; }
        public bool UpdateRunCodeAnalysis { get; set; }
        public bool UpdateSourceServerEnabled { get; set; }
        public bool UpdateSymbolServerPath { get; set; }
        public bool UpdateMSBuildArguments { get; set; }
        public bool UpdateMSBuildPlatform { get; set; }
        public bool UpdatePrivateDropLocation { get; set; }
        public bool UpdateSolutionSpecificBuildOutputs { get; set; }
        public bool UpdateMSBuildMultiProc { get; set; }
        public bool UpdateContinuousIntegrationQuietPeriod { get; set; }
        public bool UpdateBatchSize { get; set; }

        #endregion

        #region Constructors

        public BuildDefinitionInfoUpdate(ICollection<BuildDefinitionInfo> buildDefinitions)
        {
            buildDefinitions = buildDefinitions ?? new BuildDefinitionInfo[0];
            this.BuildControllerName = (buildDefinitions.Any() ? buildDefinitions.First().BuildControllerName : DefaultBuildControllerName);
            this.DefaultDropLocation = (buildDefinitions.Any() ? buildDefinitions.First().DefaultDropLocation : DefaultDefaultDropLocation);
            this.TriggerType = (buildDefinitions.Any() ? buildDefinitions.First().TriggerType : DefaultTriggerType);
            this.QueueStatus = (buildDefinitions.Any() ? buildDefinitions.First().QueueStatus : DefaultQueueStatus);
            this.ProcessTemplate = (buildDefinitions.Any() ? buildDefinitions.First().ProcessTemplate : DefaultProcessTemplate);
            this.BuildNumberFormat = (buildDefinitions.Any() ? buildDefinitions.First().BuildNumberFormat : DefaultBuildNumberFormat);
            this.CleanWorkspace = (buildDefinitions.Any() ? buildDefinitions.First().CleanWorkspace : DefaultCleanWorkspace);
            this.Verbosity = (buildDefinitions.Any() ? buildDefinitions.First().Verbosity : DefaultVerbosity);
            this.RunCodeAnalysis = (buildDefinitions.Any() ? buildDefinitions.First().RunCodeAnalysis : DefaultRunCodeAnalysis);
            this.SourceServerEnabled = (buildDefinitions.Any() ? buildDefinitions.First().SourceServerEnabled : DefaultSourceServerEnabled);
            this.SymbolServerPath = (buildDefinitions.Any() ? buildDefinitions.First().SymbolServerPath : DefaultSymbolServerPath);
            this.MSBuildArguments = (buildDefinitions.Any() ? buildDefinitions.First().MSBuildArguments : DefaultMSBuildArguments);
            this.MSBuildPlatform = (buildDefinitions.Any() ? buildDefinitions.First().MSBuildPlatform : DefaultMSBuildPlatform);
            this.PrivateDropLocation = (buildDefinitions.Any() ? buildDefinitions.First().PrivateDropLocation : DefaultPrivateDropLocation);
            this.SolutionSpecificBuildOutputs = (buildDefinitions.Any() ? buildDefinitions.First().SolutionSpecificBuildOutputs : DefaultSolutionSpecificBuildOutputs);
            this.MSBuildMultiProc = (buildDefinitions.Any() ? buildDefinitions.First().MSBuildMultiProc : DefaultMSBuildMultiProc);
            this.ContinuousIntegrationQuietPeriod = (buildDefinitions.Any() ? buildDefinitions.First().ContinuousIntegrationQuietPeriod : DefaultContinuousIntegrationQuietPeriod);
            this.BatchSize = (buildDefinitions.Any() ? buildDefinitions.First().BatchSize : DefaultBatchSize);
        }

        #endregion

        #region Update

        public void Update(ApplicationTask task, IBuildDefinition buildDefinition, ICollection<IBuildController> availableBuildControllers, ICollection<IProcessTemplate> availableProcessTemplates)
        {
            if (buildDefinition == null)
            {
                return;
            }

            // General Properties
            if (UpdateBuildControllerName)
            {
                var selectedBuildController = availableBuildControllers.FirstOrDefault(c => string.Equals(c.Name, this.BuildControllerName, StringComparison.OrdinalIgnoreCase));
                if (selectedBuildController == null)
                {
                    task.SetWarning(string.Format(CultureInfo.CurrentCulture, "The build controller could not be set to \"{0}\" because a build controller with this name is not registered.", this.BuildControllerName));
                }
                else
                {
                    buildDefinition.BuildController = selectedBuildController;
                }
            }
            if (UpdateDefaultDropLocation)
            {
                // To disable the drop location, do not set to null but always use an empty string (null only works for TFS 2010 and before, an empty string always works).
                buildDefinition.DefaultDropLocation = this.DefaultDropLocation ?? string.Empty;
            }
            if (UpdateTriggerType)
            {
                buildDefinition.TriggerType = this.TriggerType;
            }
            if (UpdateQueueStatus)
            {
                buildDefinition.QueueStatus = this.QueueStatus;
            }
            if (UpdateProcessTemplate)
            {
                var selectedProcessTemplate = availableProcessTemplates.FirstOrDefault(p => string.Equals(p.ServerPath, this.ProcessTemplate, StringComparison.OrdinalIgnoreCase));
                if (selectedProcessTemplate == null)
                {
                    task.SetWarning(string.Format(CultureInfo.CurrentCulture, "The process template could not be set to \"{0}\" because a process template with this server path is not registered.", this.ProcessTemplate));
                }
                else
                {
                    buildDefinition.Process = selectedProcessTemplate;
                }
            }
            if (UpdateContinuousIntegrationQuietPeriod)
            {
                buildDefinition.ContinuousIntegrationQuietPeriod = this.ContinuousIntegrationQuietPeriod;
            }
            if (UpdateBatchSize)
            {
                buildDefinition.BatchSize = this.BatchSize;
            }

            XmlNamespaceManager nsmgr;
            var processParameters = GetProcessParameters(buildDefinition, out nsmgr);
            var rootNode = processParameters.SelectSingleNode(ProcessParametersRootNodeXpath, nsmgr);

            // Process Template-Specific Basic Properties
            if (UpdateBuildNumberFormat)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "x:String", "BuildNumberFormat", this.BuildNumberFormat);
            }
            if (UpdateCleanWorkspace)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "mtbwa:CleanWorkspaceOption", "CleanWorkspace", this.CleanWorkspace.ToString());
            }
            if (UpdateVerbosity)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "mtbw:BuildVerbosity", "Verbosity", this.Verbosity.ToString());
            }
            if (UpdateRunCodeAnalysis)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "mtbwa:CodeAnalysisOption", "RunCodeAnalysis", this.RunCodeAnalysis.ToString());
            }
            if (UpdateSourceServerEnabled)
            {
                var sourceAndSymbolServerSettingsNode = GetOrCreateBuildProcessParameterNode(rootNode, nsmgr, "mtbwa:SourceAndSymbolServerSettings", "SourceAndSymbolServerSettings");
                SetAttribute(sourceAndSymbolServerSettingsNode, nsmgr, "IndexSources", this.SourceServerEnabled.ToString());
            }
            if (UpdateSymbolServerPath)
            {
                var sourceAndSymbolServerSettingsNode = GetOrCreateBuildProcessParameterNode(rootNode, nsmgr, "mtbwa:SourceAndSymbolServerSettings", "SourceAndSymbolServerSettings");
                SetAttribute(sourceAndSymbolServerSettingsNode, nsmgr, "SymbolStorePath", this.SymbolServerPath);
            }

            // Process Template-Specific Advanced Properties
            if (UpdateMSBuildArguments)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "x:String", "MSBuildArguments", this.MSBuildArguments);
            }
            if (UpdateMSBuildPlatform)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "mtbwa:ToolPlatform", "MSBuildPlatform", this.MSBuildPlatform.ToString());
            }
            if (UpdatePrivateDropLocation)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "x:String", "PrivateDropLocation", this.PrivateDropLocation);
            }
            if (UpdateSolutionSpecificBuildOutputs)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "x:Boolean", "SolutionSpecificBuildOutputs", this.SolutionSpecificBuildOutputs.ToString());
            }
            if (UpdateMSBuildMultiProc)
            {
                SetBuildProcessParameterNode(rootNode, nsmgr, "x:Boolean", "MSBuildMultiProc", this.MSBuildMultiProc.ToString());
            }
            buildDefinition.ProcessParameters = processParameters.OuterXml;
        }

        private void SetBuildProcessParameterNode(XmlNode rootNode, XmlNamespaceManager nsmgr, string elementName, string key, string value)
        {
            var node = GetOrCreateBuildProcessParameterNode(rootNode, nsmgr, elementName, key);
            node.InnerText = value;
        }

        private XmlNode GetOrCreateBuildProcessParameterNode(XmlNode rootNode, XmlNamespaceManager nsmgr, string elementName, string key)
        {
            var node = rootNode.SelectSingleNode(string.Format(CultureInfo.InvariantCulture, "{0}[@x:Key='{1}']", elementName, key), nsmgr);
            if (node == null)
            {
                var colonIndex = elementName.IndexOf(':');
                if (colonIndex >= 0)
                {
                    var elementNamespace = elementName.Substring(0, elementName.IndexOf(":"));
                    node = rootNode.AppendChild(rootNode.OwnerDocument.CreateElement(elementName, nsmgr.LookupNamespace(elementNamespace)));
                }
                else
                {
                    node = rootNode.AppendChild(rootNode.OwnerDocument.CreateElement(elementName));
                }
                node.Attributes.Append(rootNode.OwnerDocument.CreateAttribute("x:Key", nsmgr.LookupNamespace("x"))).Value = key;
            }
            return node;
        }

        private void SetAttribute(XmlNode node, XmlNamespaceManager nsmgr, string attributeName, string value)
        {
            var attribute = node.Attributes[attributeName];
            if (attribute == null)
            {
                attribute = node.OwnerDocument.CreateAttribute(attributeName);
            }
            attribute.Value = value;
        }

        #endregion
    }
}