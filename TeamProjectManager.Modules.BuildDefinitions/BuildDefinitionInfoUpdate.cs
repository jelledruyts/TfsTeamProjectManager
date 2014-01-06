using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        }

        #endregion
    }
}