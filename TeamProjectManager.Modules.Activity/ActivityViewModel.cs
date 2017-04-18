using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.Activity
{
    [Export]
    public class ActivityViewModel : ViewModelBase
    {
        #region Properties

        public AsyncRelayCommand GetActivityCommand { get; private set; }
        public RelayCommand ViewActivityDetailsCommand { get; private set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public ActivityViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Activity", "Allows you to see the most recent activity for Team Projects.")
        {
            this.GetActivityCommand = new AsyncRelayCommand(GetActivity, CanGetActivity);
            this.ViewActivityDetailsCommand = new RelayCommand(ViewActivityDetails, CanViewActivityDetails);
            this.SourceControlCommentExclusions = ConfigurationManager.AppSettings["SourceControlCommentExclusions"];
        }

        #endregion

        #region Observable Properties

        public int NumberOfActivities
        {
            get { return this.GetValue(NumberOfActivitiesProperty); }
            set { this.SetValue(NumberOfActivitiesProperty, value); }
        }

        public static ObservableProperty<int> NumberOfActivitiesProperty = new ObservableProperty<int, ActivityViewModel>(o => o.NumberOfActivities, 10);

        public bool TfvcComponentEnabled
        {
            get { return this.GetValue(TfvcComponentEnabledProperty); }
            set { this.SetValue(TfvcComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> TfvcComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.TfvcComponentEnabled, true);

        public bool GitComponentEnabled
        {
            get { return this.GetValue(GitComponentEnabledProperty); }
            set { this.SetValue(GitComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> GitComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.GitComponentEnabled, true);

        public bool WorkItemTrackingComponentEnabled
        {
            get { return this.GetValue(WorkItemTrackingComponentEnabledProperty); }
            set { this.SetValue(WorkItemTrackingComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> WorkItemTrackingComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.WorkItemTrackingComponentEnabled, true);

        public bool XamlBuildComponentEnabled
        {
            get { return this.GetValue(XamlBuildComponentEnabledProperty); }
            set { this.SetValue(XamlBuildComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> XamlBuildComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.XamlBuildComponentEnabled, true);

        public bool BuildComponentEnabled
        {
            get { return this.GetValue(BuildComponentEnabledProperty); }
            set { this.SetValue(BuildComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> BuildComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.BuildComponentEnabled, true);

        public string SourceControlCommentExclusions
        {
            get { return this.GetValue(SourceControlCommentExclusionsProperty); }
            set { this.SetValue(SourceControlCommentExclusionsProperty, value); }
        }

        public static ObservableProperty<string> SourceControlCommentExclusionsProperty = new ObservableProperty<string, ActivityViewModel>(o => o.SourceControlCommentExclusions);

        public string UserExclusions
        {
            get { return this.GetValue(UserExclusionsProperty); }
            set { this.SetValue(UserExclusionsProperty, value); }
        }

        public static readonly ObservableProperty<string> UserExclusionsProperty = new ObservableProperty<string, ActivityViewModel>(o => o.UserExclusions);

        public ICollection<TeamProjectActivityInfo> Activities
        {
            get { return this.GetValue(ActivitiesProperty); }
            set { this.SetValue(ActivitiesProperty, value); }
        }

        public static ObservableProperty<ICollection<TeamProjectActivityInfo>> ActivitiesProperty = new ObservableProperty<ICollection<TeamProjectActivityInfo>, ActivityViewModel>(o => o.Activities);

        public TeamProjectActivityInfo SelectedActivity
        {
            get { return this.GetValue(SelectedActivityProperty); }
            set { this.SetValue(SelectedActivityProperty, value); }
        }

        public static ObservableProperty<TeamProjectActivityInfo> SelectedActivityProperty = new ObservableProperty<TeamProjectActivityInfo, ActivityViewModel>(o => o.SelectedActivity);

        #endregion

        #region Overrides

        protected override bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return server.MajorVersion >= TfsMajorVersion.V14;
        }

        #endregion

        #region GetActivity Command

        private bool CanGetActivity(object argument)
        {
            return IsAnyTeamProjectSelected() && this.NumberOfActivities > 0 && GetComponentTypes().Count > 0;
        }

        private async Task GetActivity(object argument)
        {
            var componentTypes = GetComponentTypes();
            var numberOfActivities = this.NumberOfActivities;
            var commentExclusions = (string.IsNullOrEmpty(this.SourceControlCommentExclusions) ? new string[0] : this.SourceControlCommentExclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            var userExclusions = (string.IsNullOrEmpty(this.UserExclusions) ? new string[0] : this.UserExclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            var teamProjects = this.SelectedTeamProjects.ToList();
            var steps = teamProjects.Count * componentTypes.Count;
            var task = new ApplicationTask("Retrieving activity", steps, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var step = 0;
                var activities = new List<TeamProjectActivityInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.Status = string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name);
                    var componentActivities = new List<ComponentActivityInfo>();
                    try
                    {
                        if (!task.IsCanceled && componentTypes.Any(c => c == ActivityComponentType.Tfvc))
                        {
                            task.SetProgress(step++, "Retrieving TFVC activity");
                            componentActivities.AddRange(await GetTfvcActivityAsync(task, tfs, teamProject, numberOfActivities, userExclusions, commentExclusions));
                        }
                        if (!task.IsCanceled && componentTypes.Any(c => c == ActivityComponentType.Git))
                        {
                            task.SetProgress(step++, "Retrieving Git activity");
                            componentActivities.AddRange(await GetGitActivityAsync(task, tfs, teamProject, numberOfActivities, userExclusions, commentExclusions));
                        }
                        if (!task.IsCanceled && componentTypes.Any(c => c == ActivityComponentType.WorkItemTracking))
                        {
                            task.SetProgress(step++, "Retrieving Work Item Tracking activity");
                            componentActivities.AddRange(await GetWorkItemTrackingActivityAsync(task, tfs, teamProject, numberOfActivities, userExclusions));
                        }
                        if (!task.IsCanceled && componentTypes.Any(c => c == ActivityComponentType.Build))
                        {
                            task.SetProgress(step++, "Retrieving Build activity");
                            componentActivities.AddRange(await GetBuildActivityAsync(task, tfs, teamProject, numberOfActivities, userExclusions));
                        }
                        if (!task.IsCanceled && componentTypes.Any(c => c == ActivityComponentType.XamlBuild))
                        {
                            task.SetProgress(step++, "Retrieving XAML Build activity");
                            componentActivities.AddRange(await GetXamlBuildActivityAsync(task, tfs, teamProject, numberOfActivities, userExclusions));
                        }
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProject.Name), exc);
                    }
                    activities.Add(new TeamProjectActivityInfo(teamProject.Name, componentActivities));
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                this.Activities = activities;
                task.SetComplete("Retrieved " + this.Activities.Count.ToCountString("activity result"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving build definitions", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region ViewActivityDetails Command

        private bool CanViewActivityDetails(object argument)
        {
            return this.SelectedActivity != null;
        }

        private void ViewActivityDetails(object argument)
        {
            var dialog = new ActivityViewerDialog(this.SelectedActivity);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        #endregion

        #region Helper Methods

        private IList<ActivityComponentType> GetComponentTypes()
        {
            var componentTypes = new List<ActivityComponentType>();
            if (this.TfvcComponentEnabled)
            {
                componentTypes.Add(ActivityComponentType.Tfvc);
            }
            if (this.GitComponentEnabled)
            {
                componentTypes.Add(ActivityComponentType.Git);
            }
            if (this.WorkItemTrackingComponentEnabled)
            {
                componentTypes.Add(ActivityComponentType.WorkItemTracking);
            }
            if (this.BuildComponentEnabled)
            {
                componentTypes.Add(ActivityComponentType.Build);
            }
            if (this.XamlBuildComponentEnabled)
            {
                componentTypes.Add(ActivityComponentType.XamlBuild);
            }
            return componentTypes;
        }

        private async Task<IList<ComponentActivityInfo>> GetTfvcActivityAsync(ApplicationTask task, TfsTeamProjectCollection tfs, TeamProjectInfo teamProject, int numberOfActivities, IList<string> userExclusions, IList<string> commentExclusions)
        {
            var activities = new List<ComponentActivityInfo>();
            try
            {
                var client = tfs.GetClient<TfvcHttpClient>();
                var skip = 0;
                var top = numberOfActivities;
                while (activities.Count < numberOfActivities && !task.IsCanceled)
                {
                    var changesets = await client.GetChangesetsAsync(project: teamProject.Name, skip: skip, top: top);
                    foreach (var changeset in changesets)
                    {
                        if ((string.IsNullOrEmpty(changeset.Comment) || !commentExclusions.Any(x => changeset.Comment.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)) && !userExclusions.Any(x => changeset.Author.DisplayName.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0 || changeset.CheckedInBy.DisplayName.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            var commentSuffix = string.IsNullOrEmpty(changeset.Comment) ? null : ": " + changeset.Comment.Trim();
                            var time = changeset.CreatedDate;
                            var user = changeset.Author.DisplayName;
                            var description = string.Format(CultureInfo.CurrentCulture, "Changeset {0}{1}", changeset.ChangesetId, commentSuffix);
                            activities.Add(new ComponentActivityInfo("TFVC", time, user, description));
                        }
                    }
                    if (changesets.Count < top)
                    {
                        // There are no more pages.
                        break;
                    }
                    else
                    {
                        skip += top;
                    }
                }
            }
            catch (VssServiceException exc)
            {
                // This is thrown when the Team Project doesn't contain a TFVC repository, ignore it.
                Logger.Log("Error retrieving TFVC activity from Team Project " + teamProject.Name, exc, TraceEventType.Verbose);
            }
            return activities;
        }

        private async Task<IList<ComponentActivityInfo>> GetGitActivityAsync(ApplicationTask task, TfsTeamProjectCollection tfs, TeamProjectInfo teamProject, int numberOfActivities, IList<string> userExclusions, IList<string> commentExclusions)
        {
            var activities = new List<ComponentActivityInfo>();
            var client = tfs.GetClient<GitHttpClient>();
            var top = numberOfActivities;

            // Find all repos.
            var repos = await client.GetRepositoriesAsync(project: teamProject.Name);
            var criteria = new GitQueryCommitsCriteria { Top = top, IncludeLinks = false };
            foreach (var repo in repos)
            {
                var skip = 0;
                while (activities.Count < numberOfActivities && !task.IsCanceled)
                {
                    var commits = await client.GetCommitsAsync(repo.Id, criteria, skip: skip, top: top);
                    foreach (var commit in commits)
                    {
                        if ((string.IsNullOrEmpty(commit.Comment) || !commentExclusions.Any(x => commit.Comment.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)) && !userExclusions.Any(x => commit.Author.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0 || commit.Committer.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            var commentSuffix = string.IsNullOrEmpty(commit.Comment) ? null : ": " + commit.Comment.Trim();
                            var time = commit.Author.Date;
                            var user = commit.Author.Name;
                            var description = string.Format(CultureInfo.CurrentCulture, "Commit {0}{1}", commit.CommitId.Substring(0, 7), commentSuffix);
                            activities.Add(new ComponentActivityInfo("Git", time, user, description));
                        }
                    }
                    if (commits.Count < top)
                    {
                        // There are no more pages.
                        break;
                    }
                    else
                    {
                        skip += top;
                    }
                }
            }
            return activities;
        }

        private async Task<IList<ComponentActivityInfo>> GetWorkItemTrackingActivityAsync(ApplicationTask task, TfsTeamProjectCollection tfs, TeamProjectInfo teamProject, int numberOfActivities, IList<string> userExclusions)
        {
            var activities = new List<ComponentActivityInfo>();
            var client = tfs.GetClient<WorkItemTrackingHttpClient>();

            // First execute the query to get the work item id's.
            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project");
            foreach (var userExclusion in userExclusions)
            {
                queryBuilder.AppendFormat(" AND [System.ChangedBy] NOT CONTAINS '{0}'", userExclusion.Replace("'", "''"));
            }
            queryBuilder.Append(" ORDER BY [System.ChangedDate] DESC");
            var queryResult = await client.QueryByWiqlAsync(new Wiql { Query = queryBuilder.ToString() }, teamProject.Name, top: numberOfActivities);
            var ids = queryResult.WorkItems.Select(w => w.Id).ToArray();

            // Then get the details for the returned work item id's.
            if (ids.Any() && !task.IsCanceled)
            {
                var workItems = await client.GetWorkItemsAsync(ids, new[] { "System.Id", "System.Title", "System.ChangedDate", "System.ChangedBy", "System.WorkItemType" }, queryResult.AsOf);
                foreach (var workItem in ids.Select(id => workItems.Single(w => w.Id == id)))
                {
                    var time = (DateTime)workItem.Fields["System.ChangedDate"];
                    var user = (string)workItem.Fields["System.ChangedBy"];
                    var description = string.Format(CultureInfo.CurrentCulture, "{0} {1}: {2}", workItem.Fields["System.WorkItemType"], workItem.Id, workItem.Fields["System.Title"]);
                    activities.Add(new ComponentActivityInfo("Work Item Tracking", time, user, description));
                }
            }
            return activities;
        }

        private async Task<IList<ComponentActivityInfo>> GetBuildActivityAsync(ApplicationTask task, TfsTeamProjectCollection tfs, TeamProjectInfo teamProject, int numberOfActivities, IList<string> userExclusions)
        {
            var activities = new List<ComponentActivityInfo>();
            var client = tfs.GetClient<BuildHttpClient>();

            // There is no paging API for build queries, so we page the results manually by
            // limiting on the max finish time as being the finish time of the last returned result.
            // This will yield an overlap of 1 build per page which we can filter out.
            // Retrieve at least 2 builds to get the "manual paging" system working.
            var pageSize = Math.Max(2, numberOfActivities);
            var lastRetrievedBuild = default(Build);
            while (activities.Count < numberOfActivities && !task.IsCanceled)
            {
                var maxFinishTime = lastRetrievedBuild == null ? null : lastRetrievedBuild.FinishTime;
                var builds = await client.GetBuildsAsync(teamProject.Name, top: pageSize, maxFinishTime: maxFinishTime, queryOrder: Microsoft.TeamFoundation.Build.WebApi.BuildQueryOrder.FinishTimeDescending);
                var unseenBuilds = builds.Where(b => lastRetrievedBuild == null || b.Uri != lastRetrievedBuild.Uri);
                foreach (var build in unseenBuilds)
                {
                    lastRetrievedBuild = build;
                    if (build.RequestedFor == null || !userExclusions.Any(x => build.RequestedFor.DisplayName.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        var time = build.FinishTime ?? build.StartTime ?? build.QueueTime ?? DateTime.MinValue;
                        var user = build.RequestedFor.DisplayName;
                        var description = string.Format(CultureInfo.CurrentCulture, "Build Number \"{0}\" from Build Definition \"{1}\"", build.BuildNumber, build.Definition.Name);
                        activities.Add(new ComponentActivityInfo("Build", time, user, description));
                    }
                }
                if (!unseenBuilds.Any())
                {
                    break;
                }
            }

            return activities;
        }

        private async Task<IList<ComponentActivityInfo>> GetXamlBuildActivityAsync(ApplicationTask task, TfsTeamProjectCollection tfs, TeamProjectInfo teamProject, int numberOfActivities, IList<string> userExclusions)
        {
            var activities = new List<ComponentActivityInfo>();
            var client = tfs.GetClient<XamlBuildHttpClient>();

            // There is no paging API for build queries, so we page the results manually by
            // limiting on the max finish time as being the finish time of the last returned result.
            // This will yield an overlap of 1 build per page which we can filter out.
            // Retrieve at least 2 builds to get the "manual paging" system working.
            var pageSize = Math.Max(2, numberOfActivities);
            var lastRetrievedBuild = default(Build);
            while (activities.Count < numberOfActivities && !task.IsCanceled)
            {
                var maxFinishTime = lastRetrievedBuild == null ? null : lastRetrievedBuild.FinishTime;
                var builds = await client.GetBuildsAsync(teamProject.Name, top: pageSize, maxFinishTime: maxFinishTime, queryOrder: Microsoft.TeamFoundation.Build.WebApi.BuildQueryOrder.FinishTimeDescending);
                var unseenBuilds = builds.Where(b => lastRetrievedBuild == null || b.Uri != lastRetrievedBuild.Uri);
                foreach (var build in unseenBuilds)
                {
                    lastRetrievedBuild = build;
                    if (build.RequestedFor == null || !userExclusions.Any(x => build.RequestedFor.DisplayName.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        var time = build.FinishTime ?? build.StartTime ?? build.QueueTime ?? DateTime.MinValue;
                        var user = build.RequestedFor.DisplayName;
                        var description = string.Format(CultureInfo.CurrentCulture, "Build Number \"{0}\" from Build Definition \"{1}\"", build.BuildNumber, build.Definition.Name);
                        activities.Add(new ComponentActivityInfo("XAML Build", time, user, description));
                    }
                }
                if (!unseenBuilds.Any())
                {
                    break;
                }
            }

            return activities;
        }

        #endregion
    }
}