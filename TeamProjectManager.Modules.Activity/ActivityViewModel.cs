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
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.Activity
{
    // TODO: Allow latest x activities per team project to be viewed in a dialog
    [Export]
    public class ActivityViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetActivityCommand { get; private set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public ActivityViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Activity", "Allows you to see recent activity for Team Projects.")
        {
            this.GetActivityCommand = new RelayCommand(GetActivity, CanGetActivity);
        }

        #endregion

        #region Observable Properties

        public ICollection<TeamProjectActivityInfo> Activity
        {
            get { return this.GetValue(ActivityProperty); }
            set { this.SetValue(ActivityProperty, value); }
        }

        public static ObservableProperty<ICollection<TeamProjectActivityInfo>> ActivityProperty = new ObservableProperty<ICollection<TeamProjectActivityInfo>, ActivityViewModel>(o => o.Activity);

        #endregion

        #region GetActivity Command

        private bool CanGetActivity(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetActivity(object argument)
        {
            var numberOfActivities = 5;
            // TODO
            //var exclusions = (string.IsNullOrEmpty(this.Exclusions) ? new string[0] : this.Exclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            var exclusions = new string[0];

            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving activity", teamProjects.Count);
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
                        while (foundChangesetsForProject < numberOfActivities)
                        {
                            const int pageCount = 10;
                            var history = vcs.QueryHistory("$/" + teamProject.Name, VersionSpec.Latest, 0, RecursionType.Full, null, null, versionTo, pageCount, false, false, false).Cast<Changeset>().ToList();
                            foreach (Changeset changeset in history)
                            {
                                if (string.IsNullOrEmpty(changeset.Comment) || !exclusions.Any(x => changeset.Comment.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    foundChangesetsForProject++;
                                    var commentSuffix = string.IsNullOrEmpty(changeset.Comment) ? null : ": " + changeset.Comment;
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
                        // TODO
                        var parameters = new Dictionary<string, object>() {
                                { "TeamProject", teamProject.Name}
                            };
                        var workItemsQuery = wit.Query("SELECT [System.Id], [System.ChangedDate], [System.Title] FROM WorkItems WHERE [System.TeamProject] = @TeamProject ORDER BY [System.ChangedDate]", parameters);
                        var workItems = workItemsQuery.Cast<WorkItem>().Take(numberOfActivities).ToList();
                        var workItemActivities = workItems.Select(w => new ComponentActivityInfo("Work Item Tracking", w.ChangedDate, string.Format(CultureInfo.CurrentCulture, "{0} {1}: \"{2}\"", w.Type.Name, w.Id, w.Title))).ToList();

                        // TODO: Check if this also returns new (unchanged) work items.

                        // Get the latest build.
                        var spec = buildServer.CreateBuildDetailSpec(teamProject.Name);
                        spec.QueryOptions = QueryOptions.Definitions;
                        spec.QueryOrder = BuildQueryOrder.StartTimeDescending;
                        spec.MaxBuildsPerDefinition = numberOfActivities;
                        spec.QueryDeletedOption = QueryDeletedOption.IncludeDeleted;
                        // TODO: Retrieve *all* changed builds (numberOfActivities)
                        var builds = buildServer.QueryBuilds(spec).Builds;
                        var teamBuildActivities = builds.Select(b => new ComponentActivityInfo("Team Build", b.StartTime, string.Format(CultureInfo.CurrentCulture, "Build Number \"{0}\" from Build Definition \"{1}\"", b.BuildDefinition.Name, b.BuildNumber)));

                        activity.Add(new TeamProjectActivityInfo(teamProject.Name, sourceControlActivities, workItemActivities, teamBuildActivities));
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProject.Name), exc);
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
                    this.Activity = (ICollection<TeamProjectActivityInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.Activity.Count.ToCountString("activity result"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion
    }
}