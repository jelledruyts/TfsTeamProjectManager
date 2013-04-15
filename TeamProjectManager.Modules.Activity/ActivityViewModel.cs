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

        public string SourceControlExclusions
        {
            get { return this.GetValue(SourceControlExclusionsProperty); }
            set { this.SetValue(SourceControlExclusionsProperty, value); }
        }

        public static ObservableProperty<string> SourceControlExclusionsProperty = new ObservableProperty<string, ActivityViewModel>(o => o.SourceControlExclusions, Constants.DefaultSourceControlHistoryExclusions);

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

        private bool CanGetActivity(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetActivity(object argument)
        {
            var numberOfActivities = this.NumberOfActivities;
            var exclusions = (string.IsNullOrEmpty(this.SourceControlExclusions) ? new string[0] : this.SourceControlExclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

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
                        // Get the latest source control activity.
                        var sourceControlActivities = new List<ComponentActivityInfo>();
                        var foundChangesetsForProject = 0;
                        VersionSpec versionTo = null;
                        while (foundChangesetsForProject < numberOfActivities && !task.IsCanceled)
                        {
                            const int pageCount = 10;
                            var history = vcs.QueryHistory("$/" + teamProject.Name, VersionSpec.Latest, 0, RecursionType.Full, null, null, versionTo, pageCount, false, false, false).Cast<Changeset>().ToList();
                            foreach (Changeset changeset in history)
                            {
                                if (string.IsNullOrEmpty(changeset.Comment) || !exclusions.Any(x => changeset.Comment.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    foundChangesetsForProject++;
                                    var commentSuffix = string.IsNullOrEmpty(changeset.Comment) ? null : ": " + changeset.Comment.Trim();
                                    sourceControlActivities.Add(new ComponentActivityInfo("Source Control", changeset.CreationDate, string.Format(CultureInfo.CurrentCulture, "Changeset {0} by \"{1}\"{2}", changeset.ChangesetId, changeset.Committer, commentSuffix)));
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

                        // Get the latest work item tracking activity.
                        IEnumerable<ComponentActivityInfo> workItemActivities = null;
                        if (!task.IsCanceled)
                        {
                            var parameters = new Dictionary<string, object>() {
                                { "TeamProject", teamProject.Name}
                            };
                            var workItemsQuery = wit.Query("SELECT [System.Id], [System.ChangedDate], [System.Title] FROM WorkItems WHERE [System.TeamProject] = @TeamProject ORDER BY [System.ChangedDate] DESC", parameters);
                            var workItems = workItemsQuery.Cast<WorkItem>().Take(numberOfActivities).ToList();
                            workItemActivities = workItems.Select(w => new ComponentActivityInfo("Work Item Tracking", w.ChangedDate, string.Format(CultureInfo.CurrentCulture, "{0} {1}: \"{2}\"", w.Type.Name, w.Id, w.Title))).ToList();
                        }

                        // Get the latest builds.
                        IEnumerable<ComponentActivityInfo> teamBuildActivities = null;
                        if (!task.IsCanceled)
                        {
                            var spec = buildServer.CreateBuildDetailSpec(teamProject.Name);
                            spec.QueryOptions = QueryOptions.Definitions;
                            spec.QueryOrder = BuildQueryOrder.StartTimeDescending;
                            spec.MaxBuildsPerDefinition = numberOfActivities;
                            spec.QueryDeletedOption = QueryDeletedOption.IncludeDeleted;
                            var builds = buildServer.QueryBuilds(spec).Builds;
                            teamBuildActivities = builds.OrderByDescending(b => b.StartTime).Take(numberOfActivities).Select(b => new ComponentActivityInfo("Team Build", b.StartTime, string.Format(CultureInfo.CurrentCulture, "Build Number \"{0}\" from Build Definition \"{1}\"", b.BuildNumber, b.BuildDefinition.Name)));
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