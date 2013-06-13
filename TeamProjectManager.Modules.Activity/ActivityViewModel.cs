using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.Activity
{
    [Export]
    public class ActivityViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        /// The default substrings of changeset comments to exclude from source control activity.
        /// </summary>
        public const string DefaultSourceControlCommentExclusions = "Auto-Build: Version Update;Checked in by server upgrade";

        #endregion

        #region Properties

        public RelayCommand GetActivityCommand { get; private set; }
        public RelayCommand ViewActivityDetailsCommand { get; private set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public ActivityViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Activity", "Allows you to see the most recent activity for Team Projects.")
        {
            this.GetActivityCommand = new RelayCommand(GetActivity, CanGetActivity);
            this.ViewActivityDetailsCommand = new RelayCommand(ViewActivityDetails, CanViewActivityDetails);
        }

        #endregion

        #region Observable Properties

        public int NumberOfActivities
        {
            get { return this.GetValue(NumberOfActivitiesProperty); }
            set { this.SetValue(NumberOfActivitiesProperty, value); }
        }

        public static ObservableProperty<int> NumberOfActivitiesProperty = new ObservableProperty<int, ActivityViewModel>(o => o.NumberOfActivities, 10);

        public bool SourceControlComponentEnabled
        {
            get { return this.GetValue(SourceControlComponentEnabledProperty); }
            set { this.SetValue(SourceControlComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> SourceControlComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.SourceControlComponentEnabled, true);

        public bool WorkItemTrackingComponentEnabled
        {
            get { return this.GetValue(WorkItemTrackingComponentEnabledProperty); }
            set { this.SetValue(WorkItemTrackingComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> WorkItemTrackingComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.WorkItemTrackingComponentEnabled, true);

        public bool TeamBuildComponentEnabled
        {
            get { return this.GetValue(TeamBuildComponentEnabledProperty); }
            set { this.SetValue(TeamBuildComponentEnabledProperty, value); }
        }

        public static readonly ObservableProperty<bool> TeamBuildComponentEnabledProperty = new ObservableProperty<bool, ActivityViewModel>(o => o.TeamBuildComponentEnabled, true);

        public string SourceControlCommentExclusions
        {
            get { return this.GetValue(SourceControlCommentExclusionsProperty); }
            set { this.SetValue(SourceControlCommentExclusionsProperty, value); }
        }

        public static ObservableProperty<string> SourceControlCommentExclusionsProperty = new ObservableProperty<string, ActivityViewModel>(o => o.SourceControlCommentExclusions, DefaultSourceControlCommentExclusions);

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

        #region GetActivity Command

        private ActivityComponentTypes GetComponentTypes()
        {
            var componentTypes = ActivityComponentTypes.None;
            if (this.SourceControlComponentEnabled)
            {
                componentTypes |= ActivityComponentTypes.SourceControl;
            }
            if (this.WorkItemTrackingComponentEnabled)
            {
                componentTypes |= ActivityComponentTypes.WorkItemTracking;
            }
            if (this.TeamBuildComponentEnabled)
            {
                componentTypes |= ActivityComponentTypes.TeamBuild;
            }
            return componentTypes;
        }

        private bool CanGetActivity(object argument)
        {
            return IsAnyTeamProjectSelected() && this.NumberOfActivities > 0 && GetComponentTypes() != ActivityComponentTypes.None;
        }

        private void GetActivity(object argument)
        {
            var componentTypes = GetComponentTypes();
            var numberOfActivities = this.NumberOfActivities;
            var commentExclusions = (string.IsNullOrEmpty(this.SourceControlCommentExclusions) ? new string[0] : this.SourceControlCommentExclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            var userExclusions = (string.IsNullOrEmpty(this.UserExclusions) ? new string[0] : this.UserExclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving activity", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetService<IBuildServer>();
                var wit = tfs.GetService<WorkItemStore>();
                var vcs = tfs.GetService<VersionControlServer>();

                var step = 0;
                var activity = new List<TeamProjectActivityInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var sourceControlActivities = new List<ComponentActivityInfo>();
                        if (!task.IsCanceled && componentTypes.HasFlag(ActivityComponentTypes.SourceControl))
                        {
                            // Get the latest source control activity.
                            var foundChangesetsForProject = 0;
                            VersionSpec versionTo = null;
                            while (foundChangesetsForProject < numberOfActivities && !task.IsCanceled)
                            {
                                const int pageCount = 10;
                                var history = vcs.QueryHistory("$/" + teamProject.Name, VersionSpec.Latest, 0, RecursionType.Full, null, null, versionTo, pageCount, false, false, false).Cast<Changeset>().ToList();
                                foreach (Changeset changeset in history)
                                {
                                    if ((string.IsNullOrEmpty(changeset.Comment) || !commentExclusions.Any(x => changeset.Comment.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)) && !userExclusions.Any(x => changeset.Owner.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0 || changeset.OwnerDisplayName.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        foundChangesetsForProject++;
                                        var commentSuffix = string.IsNullOrEmpty(changeset.Comment) ? null : ": " + changeset.Comment.Trim();
                                        sourceControlActivities.Add(new ComponentActivityInfo("Source Control", changeset.CreationDate, changeset.CommitterDisplayName, string.Format(CultureInfo.CurrentCulture, "Changeset {0}{1}", changeset.ChangesetId, commentSuffix)));
                                        if (foundChangesetsForProject == numberOfActivities)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (history.Count == pageCount)
                                {
                                    versionTo = new ChangesetVersionSpec(history.Last().ChangesetId - 1);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        // Get the latest work item tracking activity.
                        IEnumerable<ComponentActivityInfo> workItemActivities = null;
                        if (!task.IsCanceled && componentTypes.HasFlag(ActivityComponentTypes.WorkItemTracking))
                        {
                            var parameters = new Dictionary<string, object>() {
                                { "TeamProject", teamProject.Name}
                            };
                            var queryBuilder = new StringBuilder();
                            queryBuilder.Append("SELECT [System.Id], [System.ChangedDate], [System.Title] FROM WorkItems WHERE [System.TeamProject] = @TeamProject");
                            for (var userExclusionIndex = 0; userExclusionIndex < userExclusions.Length; userExclusionIndex++)
                            {
                                var userParameterName = "User" + userExclusionIndex;
                                queryBuilder.AppendFormat(" AND [System.ChangedBy] NOT CONTAINS @{0}", userParameterName);
                                parameters.Add(userParameterName, userExclusions[userExclusionIndex]);
                            }
                            queryBuilder.Append(" ORDER BY [System.ChangedDate] DESC");
                            var workItemsQuery = wit.Query(queryBuilder.ToString(), parameters);
                            var workItems = workItemsQuery.Cast<WorkItem>().Take(numberOfActivities).ToList();
                            workItemActivities = workItems.Select(w => new ComponentActivityInfo("Work Item Tracking", w.ChangedDate, w.ChangedBy, string.Format(CultureInfo.CurrentCulture, "{0} {1}: {2}", w.Type.Name, w.Id, w.Title))).ToList();
                        }

                        // Get the latest builds.
                        var teamBuildActivities = new List<ComponentActivityInfo>();
                        if (!task.IsCanceled && componentTypes.HasFlag(ActivityComponentTypes.TeamBuild))
                        {
                            // Retrieve at least 2 builds to get the "manual paging" system working.
                            var pageSize = Math.Max(2, numberOfActivities);
                            IBuildDetail lastRetrievedBuild = null;
                            while (true)
                            {
                                var spec = buildServer.CreateBuildDetailSpec(teamProject.Name);
                                spec.QueryOptions = QueryOptions.Definitions | QueryOptions.BatchedRequests;
                                spec.QueryOrder = BuildQueryOrder.StartTimeDescending;
                                spec.MaxBuildsPerDefinition = pageSize;
                                spec.QueryDeletedOption = QueryDeletedOption.IncludeDeleted;
                                if (lastRetrievedBuild != null)
                                {
                                    // There is no paging API for build queries, so we page the results manually by
                                    // limiting on the max finish time as being the finish time of the last returned result.
                                    // This will yield an overlap of 1 build per page which we can filter out.
                                    spec.MaxFinishTime = lastRetrievedBuild.FinishTime;
                                }
                                var pagedBuilds = buildServer.QueryBuilds(spec).Builds.Where(b => lastRetrievedBuild == null || b.Uri != lastRetrievedBuild.Uri).ToArray();
                                lastRetrievedBuild = pagedBuilds.LastOrDefault();
                                teamBuildActivities.AddRange(pagedBuilds.Where(b => string.IsNullOrEmpty(b.RequestedFor) || !userExclusions.Any(x => b.RequestedFor.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)).OrderByDescending(b => b.StartTime).Take(numberOfActivities).Select(b => new ComponentActivityInfo("Team Build", b.StartTime, b.RequestedFor, string.Format(CultureInfo.CurrentCulture, "Build Number \"{0}\" from Build Definition \"{1}\"", b.BuildNumber, b.BuildDefinition.Name))));
                                if (!pagedBuilds.Any() || teamBuildActivities.Count >= numberOfActivities)
                                {
                                    break;
                                }
                            }
                        }

                        // Assemble the complete activity details.
                        activity.Add(new TeamProjectActivityInfo(teamProject.Name, numberOfActivities, sourceControlActivities, workItemActivities, teamBuildActivities));
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProject.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }

                e.Result = activity;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving activity", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.Activities = (ICollection<TeamProjectActivityInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.Activities.Count.ToCountString("activity result"));
                }
            };
            worker.RunWorkerAsync();
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
    }
}