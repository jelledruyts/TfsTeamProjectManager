using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.BuildAndRelease.TaskGroups
{
    [Export]
    public class TaskGroupsViewModel : ViewModelBase
    {
        #region Properties

        public AsyncRelayCommand GetTaskGroupsCommand { get; private set; }
        public AsyncRelayCommand DeleteSelectedTaskGroupsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<TaskGroupInfo> TaskGroups
        {
            get { return this.GetValue(TaskGroupsProperty); }
            set { this.SetValue(TaskGroupsProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<TaskGroupInfo>> TaskGroupsProperty = new ObservableProperty<ICollection<TaskGroupInfo>, TaskGroupsViewModel>(o => o.TaskGroups);

        public ICollection<TaskGroupInfo> SelectedTaskGroups
        {
            get { return this.GetValue(SelectedTaskGroupsProperty); }
            set { this.SetValue(SelectedTaskGroupsProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<TaskGroupInfo>> SelectedTaskGroupsProperty = new ObservableProperty<ICollection<TaskGroupInfo>, TaskGroupsViewModel>(o => o.SelectedTaskGroups);

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected TaskGroupsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Allows you to manage task groups for Team Projects.")
        {
            this.GetTaskGroupsCommand = new AsyncRelayCommand(GetTaskGroups, CanGetTaskGroups);
            this.DeleteSelectedTaskGroupsCommand = new AsyncRelayCommand(DeleteSelectedTaskGroups, CanDeleteSelectedTaskGroups);
        }

        #endregion

        #region GetTaskGroups Command

        private bool CanGetTaskGroups(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private async Task GetTaskGroups(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving task groups", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var taskAgentClient = tfs.GetClient<TaskAgentHttpClient>();

                var step = 0;
                var taskGroups = new List<TaskGroupInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var projectTaskGroups = await taskAgentClient.GetTaskGroupsAsync(project: teamProject.Name);
                        taskGroups.AddRange(projectTaskGroups.Select(t => new TaskGroupInfo(teamProject, t)));
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
                this.TaskGroups = taskGroups;
                task.SetComplete("Retrieved " + this.TaskGroups.Count.ToCountString("task group"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving task groups", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region DeleteSelectedTaskGroups Command

        private bool CanDeleteSelectedTaskGroups(object argument)
        {
            return this.SelectedTaskGroups != null && this.SelectedTaskGroups.Any();
        }

        private async Task DeleteSelectedTaskGroups(object argument)
        {
            var result = MessageBox.Show("This will delete the selected task groups. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var taskGroupsToDelete = this.SelectedTaskGroups;
            var task = new ApplicationTask("Deleting task groups", taskGroupsToDelete.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var taskAgentClient = tfs.GetClient<TaskAgentHttpClient>();

                var step = 0;
                var count = 0;
                foreach (var taskGroupToDelete in taskGroupsToDelete)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting task group \"{0}\" in Team Project \"{1}\"", taskGroupToDelete.TaskGroup.Name, taskGroupToDelete.TeamProject.Name));
                    try
                    {
                        // Delete the task groups one by one to avoid one failure preventing the other ones from being deleted.
                        await taskAgentClient.DeleteTaskGroupAsync(taskGroupToDelete.TeamProject.Guid, taskGroupToDelete.TaskGroup.Id);
                        count++;
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting task group \"{0}\" in Team Project \"{1}\"", taskGroupToDelete.TaskGroup.Name, taskGroupToDelete.TeamProject.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                task.SetComplete("Deleted " + count.ToCountString("task group"));

                // Refresh the list.
                await GetTaskGroups(null);
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while deleting task groups", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion
    }
}