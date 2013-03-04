using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.Client;
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

        public ICollection<ActivityInfo> Activity
        {
            get { return this.GetValue(ActivityProperty); }
            set { this.SetValue(ActivityProperty, value); }
        }

        public static ObservableProperty<ICollection<ActivityInfo>> ActivityProperty = new ObservableProperty<ICollection<ActivityInfo>, ActivityViewModel>(o => o.Activity);

        #endregion

        #region GetActivity Command

        private bool CanGetActivity(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetActivity(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving activity", teamProjects.Count);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetService<IBuildServer>();

                var step = 0;
                var activity = new List<ActivityInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        //foreach (var buildDefinition in buildServer.QueryBuildDefinitions(teamProject.Name, QueryOptions.All))
                        //{
                        //}
                        activity.Add(new ActivityInfo(teamProject.Name, DateTimeOffset.Now, null, DateTimeOffset.Now, null, DateTimeOffset.Now, null));
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
                    this.Activity = (ICollection<ActivityInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.Activity.Count.ToCountString("activity result"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion
    }
}