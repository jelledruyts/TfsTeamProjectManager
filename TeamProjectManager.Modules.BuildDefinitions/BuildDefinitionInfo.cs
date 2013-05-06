using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace TeamProjectManager.Modules.BuildDefinitions
{
    public class BuildDefinitionInfo
    {
        #region Constants

        // General Properties
        protected const string DefaultBuildControllerName = null;
        protected const string DefaultDefaultDropLocation = null;
        protected const DefinitionTriggerType DefaultTriggerType = DefinitionTriggerType.None;
        protected const DefinitionQueueStatus DefaultQueueStatus = DefinitionQueueStatus.Enabled;
        protected const string DefaultProcessTemplate = null;
        protected const int DefaultContinuousIntegrationQuietPeriod = 0;
        protected const int DefaultBatchSize = 1;

        // Process Template-Specific Basic Properties
        protected const string ProcessParametersRootNodeXpath = "/scg:Dictionary";
        protected const string BuildNumberFormatNodeXpath = ProcessParametersRootNodeXpath + "/x:String[@x:Key='BuildNumberFormat']";
        protected const string DefaultBuildNumberFormat = "$(BuildDefinitionName)_$(Date:yyyyMMdd)$(Rev:.r)";
        protected const string CleanWorkspaceNodeXpath = ProcessParametersRootNodeXpath + "/mtbwa:CleanWorkspaceOption[@x:Key='CleanWorkspace']";
        protected const CleanWorkspaceOption DefaultCleanWorkspace = CleanWorkspaceOption.All;
        protected const string VerbosityNodeXpath = ProcessParametersRootNodeXpath + "/mtbw:BuildVerbosity[@x:Key='Verbosity']";
        protected const BuildVerbosity DefaultVerbosity = BuildVerbosity.Normal;
        protected const string RunCodeAnalysisNodeXpath = ProcessParametersRootNodeXpath + "/mtbwa:CodeAnalysisOption[@x:Key='RunCodeAnalysis']";
        protected const CodeAnalysisOption DefaultRunCodeAnalysis = CodeAnalysisOption.AsConfigured;
        protected const string SourceAndSymbolServerSettingsNodeXpath = ProcessParametersRootNodeXpath + "/mtbwa:SourceAndSymbolServerSettings[@x:Key='SourceAndSymbolServerSettings']";
        protected const bool DefaultSourceServerEnabled = false;
        protected const string DefaultSymbolServerPath = null;

        // Process Template-Specific Advanced Properties
        protected const string MSBuildArgumentsNodeXpath = ProcessParametersRootNodeXpath + "/x:String[@x:Key='MSBuildArguments']";
        protected const string DefaultMSBuildArguments = null;
        protected const string MSBuildPlatformNodeXpath = ProcessParametersRootNodeXpath + "/mtbwa:ToolPlatform[@x:Key='MSBuildPlatform']";
        protected const ToolPlatform DefaultMSBuildPlatform = ToolPlatform.Auto;
        protected const string PrivateDropLocationNodeXpath = ProcessParametersRootNodeXpath + "/x:String[@x:Key='PrivateDropLocation']";
        protected const string DefaultPrivateDropLocation = null;
        protected const string SolutionSpecificBuildOutputsNodeXpath = ProcessParametersRootNodeXpath + "/x:Boolean[@x:Key='SolutionSpecificBuildOutputs']";
        protected const bool DefaultSolutionSpecificBuildOutputs = false;
        protected const string MSBuildMultiProcNodeXpath = ProcessParametersRootNodeXpath + "/x:Boolean[@x:Key='MSBuildMultiProc']";
        protected const bool DefaultMSBuildMultiProc = true;

        #endregion

        #region Properties

        public string TeamProject { get; private set; }
        public Uri Uri { get; private set; }
        public string Name { get; private set; }

        // General Properties
        public string BuildControllerName { get; set; }
        public string DefaultDropLocation { get; set; }
        public DefinitionTriggerType TriggerType { get; set; }
        public DefinitionQueueStatus QueueStatus { get; set; }
        public string ProcessTemplate { get; set; }
        public string ScheduleDescription { get; set; }
        public int ContinuousIntegrationQuietPeriod { get; set; }
        public int BatchSize { get; set; }
        public DateTime? LastBuildStartTime { get; set; }

        // Process Template-Specific Basic Properties
        public string BuildNumberFormat { get; set; }
        public CleanWorkspaceOption CleanWorkspace { get; set; }
        public BuildVerbosity Verbosity { get; set; }
        public CodeAnalysisOption RunCodeAnalysis { get; set; }
        public bool SourceServerEnabled { get; set; }
        public string SymbolServerPath { get; set; }
        public bool SolutionSpecificBuildOutputs { get; set; }
        public bool MSBuildMultiProc { get; set; }

        // Process Template-Specific Advanced Properties
        public string MSBuildArguments { get; set; }
        public ToolPlatform MSBuildPlatform { get; set; }
        public string PrivateDropLocation { get; set; }

        #endregion

        #region Constructors

        protected BuildDefinitionInfo()
        {
        }

        public BuildDefinitionInfo(string teamProject, IBuildDefinition buildDefinition)
            : this()
        {
            this.TeamProject = teamProject;
            this.Uri = buildDefinition.Uri;
            this.Name = buildDefinition.Name;
            this.BuildControllerName = (buildDefinition.BuildController == null ? null : buildDefinition.BuildController.Name);
            this.DefaultDropLocation = buildDefinition.DefaultDropLocation;
            this.TriggerType = buildDefinition.TriggerType;
            this.QueueStatus = buildDefinition.QueueStatus;
            this.ProcessTemplate = buildDefinition.Process == null ? null : buildDefinition.Process.ServerPath;
            this.ContinuousIntegrationQuietPeriod = buildDefinition.ContinuousIntegrationQuietPeriod;
            this.BatchSize = buildDefinition.BatchSize;

            var buildSpec = buildDefinition.BuildServer.CreateBuildDetailSpec(buildDefinition);
            buildSpec.InformationTypes = null;
            buildSpec.MaxBuildsPerDefinition = 1;
            buildSpec.QueryOrder = BuildQueryOrder.StartTimeDescending;
            var builds = buildDefinition.BuildServer.QueryBuilds(buildSpec).Builds;
            if (builds.Any())
            {
                this.LastBuildStartTime = builds.First().StartTime;
            }

            var scheduleDescription = new StringBuilder();
            foreach (var schedule in buildDefinition.Schedules)
            {
                if (scheduleDescription.Length > 0)
                {
                    scheduleDescription.Append("; ");
                }
                var time = TimeSpan.FromSeconds(schedule.StartTime);
                scheduleDescription.AppendFormat(CultureInfo.CurrentCulture, "{0} at {1}", schedule.DaysToBuild.ToString(), time.ToString("g"));
            }
            this.ScheduleDescription = scheduleDescription.ToString();

            if (!string.IsNullOrEmpty(buildDefinition.ProcessParameters))
            {
                try
                {
                    XmlNamespaceManager nsmgr;
                    var processParameters = GetProcessParameters(buildDefinition, out nsmgr);

                    this.BuildNumberFormat = GetBuildProcessParameterAsString(processParameters, nsmgr, BuildNumberFormatNodeXpath, DefaultBuildNumberFormat);
                    this.CleanWorkspace = GetBuildProcessParameterAsEnum<CleanWorkspaceOption>(processParameters, nsmgr, CleanWorkspaceNodeXpath, DefaultCleanWorkspace);
                    this.Verbosity = GetBuildProcessParameterAsEnum<BuildVerbosity>(processParameters, nsmgr, VerbosityNodeXpath, DefaultVerbosity);
                    this.RunCodeAnalysis = GetBuildProcessParameterAsEnum<CodeAnalysisOption>(processParameters, nsmgr, RunCodeAnalysisNodeXpath, DefaultRunCodeAnalysis);
                    this.SourceServerEnabled = GetBuildProcessParameterAsBoolean(processParameters, nsmgr, SourceAndSymbolServerSettingsNodeXpath + "/@IndexSources", DefaultSourceServerEnabled);
                    this.SymbolServerPath = GetBuildProcessParameterAsString(processParameters, nsmgr, SourceAndSymbolServerSettingsNodeXpath + "/@SymbolStorePath", DefaultSymbolServerPath);
                    this.MSBuildArguments = GetBuildProcessParameterAsString(processParameters, nsmgr, MSBuildArgumentsNodeXpath, DefaultMSBuildArguments);
                    this.MSBuildPlatform = GetBuildProcessParameterAsEnum<ToolPlatform>(processParameters, nsmgr, MSBuildPlatformNodeXpath, DefaultMSBuildPlatform);
                    this.PrivateDropLocation = GetBuildProcessParameterAsString(processParameters, nsmgr, PrivateDropLocationNodeXpath, DefaultPrivateDropLocation);
                    this.SolutionSpecificBuildOutputs = GetBuildProcessParameterAsBoolean(processParameters, nsmgr, SolutionSpecificBuildOutputsNodeXpath, DefaultSolutionSpecificBuildOutputs);
                    this.MSBuildMultiProc = GetBuildProcessParameterAsBoolean(processParameters, nsmgr, MSBuildMultiProcNodeXpath, DefaultMSBuildMultiProc);
                }
                catch (Exception)
                {
                    // Ignore any exception while parsing the Build Process Parameters.
                }
            }
        }

        #endregion

        #region Helper Methods

        protected static XmlDocument GetProcessParameters(IBuildDefinition buildDefinition, out XmlNamespaceManager nsmgr)
        {
            var processParameters = new XmlDocument();
            if (!string.IsNullOrEmpty(buildDefinition.ProcessParameters))
            {
                processParameters.LoadXml(buildDefinition.ProcessParameters);
            }
            nsmgr = new XmlNamespaceManager(processParameters.NameTable);
            nsmgr.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            nsmgr.AddNamespace("mtbw", "clr-namespace:Microsoft.TeamFoundation.Build.Workflow;assembly=Microsoft.TeamFoundation.Build.Workflow");
            nsmgr.AddNamespace("mtbwa", "clr-namespace:Microsoft.TeamFoundation.Build.Workflow.Activities;assembly=Microsoft.TeamFoundation.Build.Workflow");
            nsmgr.AddNamespace("scg", "clr-namespace:System.Collections.Generic;assembly=mscorlib");
            return processParameters;
        }

        private static bool GetBuildProcessParameterAsBoolean(XmlDocument processParameters, XmlNamespaceManager nsmgr, string xpath, bool defaultValue)
        {
            var value = GetBuildProcessParameterValue(processParameters, nsmgr, xpath);
            return (value == null ? defaultValue : bool.Parse(value));
        }

        private static string GetBuildProcessParameterAsString(XmlDocument processParameters, XmlNamespaceManager nsmgr, string xpath, string defaultValue)
        {
            var value = GetBuildProcessParameterValue(processParameters, nsmgr, xpath);
            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                if (string.Equals("{x:Null}", value, StringComparison.OrdinalIgnoreCase))
                {
                    value = null;
                }
                return value;
            }
        }

        private static TEnum GetBuildProcessParameterAsEnum<TEnum>(XmlDocument processParameters, XmlNamespaceManager nsmgr, string xpath, TEnum defaultValue) where TEnum : struct
        {
            var value = GetBuildProcessParameterValue(processParameters, nsmgr, xpath);
            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                return (TEnum)Enum.Parse(typeof(TEnum), value);
            }
        }

        private static string GetBuildProcessParameterValue(XmlDocument processParameters, XmlNamespaceManager nsmgr, string xpath)
        {
            var node = GetBuildProcessParameterNode(processParameters, nsmgr, xpath);
            return node == null ? null : node.InnerXml;
        }

        protected static XmlNode GetBuildProcessParameterNode(XmlDocument processParameters, XmlNamespaceManager nsmgr, string xpath)
        {
            return processParameters.SelectSingleNode(xpath, nsmgr);
        }

        #endregion
    }
}
