using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

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

        #endregion

        #region Properties

        public string TeamProject { get; private set; }
        public Uri Uri { get; private set; }
        public string Name { get; private set; }
        public string BuildControllerName { get; set; }
        public string DefaultDropLocation { get; set; }
        public DefinitionTriggerType TriggerType { get; set; }
        public DefinitionQueueStatus QueueStatus { get; set; }
        public string ProcessTemplate { get; set; }
        public string ScheduleDescription { get; set; }
        public int ContinuousIntegrationQuietPeriod { get; set; }
        public int BatchSize { get; set; }
        public DateTime? LastBuildStartTime { get; set; }
        public string MaxBuildsToKeepByRetentionPolicy { get; set; }

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
            if (buildDefinition.RetentionPolicyList.Any())
            {
                var maxNumberToKeep = buildDefinition.RetentionPolicyList.Max(r => r.NumberToKeep);
                this.MaxBuildsToKeepByRetentionPolicy = "Keep " + (maxNumberToKeep == 0 ? "None" : (maxNumberToKeep == int.MaxValue ? "All" : (maxNumberToKeep == 1 ? "Latest Only" : maxNumberToKeep + " Latest")));
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
        }

        #endregion
    }
}
