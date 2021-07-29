using Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Common.UI;

namespace TeamProjectManager.Modules.BuildAndRelease.TaskGroups
{
    [Export]
    public class TaskGroupsViewModel : ViewModelBase
    {
        #region Properties

        public AsyncRelayCommand GetTaskGroupsCommand { get; private set; }
        public AsyncRelayCommand DeleteSelectedTaskGroupsCommand { get; private set; }
        public AsyncRelayCommand AddTaskGroupFromTeamProjectCommand { get; private set; }
        public RelayCommand RemoveSelectedTaskGroupsToImportCommand { get; private set; }
        public RelayCommand RemoveAllTaskGroupsToImportCommand { get; private set; }
        public AsyncRelayCommand ImportTaskGroupsCommand { get; private set; }

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

        public ObservableCollection<TaskGroupCreateParameter> TaskGroupsToImport
        {
            get { return this.GetValue(TaskGroupsToImportProperty); }
            set { this.SetValue(TaskGroupsToImportProperty, value); }
        }

        public static readonly ObservableProperty<ObservableCollection<TaskGroupCreateParameter>> TaskGroupsToImportProperty = new ObservableProperty<ObservableCollection<TaskGroupCreateParameter>, TaskGroupsViewModel>(o => o.TaskGroupsToImport);

        public ICollection<TaskGroupCreateParameter> SelectedTaskGroupsToImport
        {
            get { return this.GetValue(SelectedTaskGroupsToImportProperty); }
            set { this.SetValue(SelectedTaskGroupsToImportProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<TaskGroupCreateParameter>> SelectedTaskGroupsToImportProperty = new ObservableProperty<ICollection<TaskGroupCreateParameter>, TaskGroupsViewModel>(o => o.SelectedTaskGroupsToImport);

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected TaskGroupsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Allows you to manage task groups for Team Projects.")
        {
            this.TaskGroupsToImport = new ObservableCollection<TaskGroupCreateParameter>();
            this.GetTaskGroupsCommand = new AsyncRelayCommand(GetTaskGroups, CanGetTaskGroups);
            this.DeleteSelectedTaskGroupsCommand = new AsyncRelayCommand(DeleteSelectedTaskGroups, CanDeleteSelectedTaskGroups);
            this.AddTaskGroupFromTeamProjectCommand = new AsyncRelayCommand(AddTaskGroupFromTeamProject, CanAddTaskGroupFromTeamProject);
            this.RemoveSelectedTaskGroupsToImportCommand = new RelayCommand(RemoveSelectedTaskGroupsToImport, CanRemoveSelectedTaskGroupsToImport);
            this.RemoveAllTaskGroupsToImportCommand = new RelayCommand(RemoveAllTaskGroupsToImport, CanRemoveAllTaskGroupsToImport);
            this.ImportTaskGroupsCommand = new AsyncRelayCommand(ImportTaskGroups, CanImportTaskGroups);
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
            var result = MessageBox.Show("This will delete the selected task groups and thereby break all build definitions that refer to them. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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

        #region AddTaskGroupFromTeamProject Command

        private bool CanAddTaskGroupFromTeamProject(object argument)
        {
            return true;
        }

        private async Task AddTaskGroupFromTeamProject(object argument)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                {
                    var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                    var teamProject = dialog.SelectedProjects.First();
                    var taskAgentClient = teamProjectCollection.GetClient<TaskAgentHttpClient>();
                    var taskGroups = await taskAgentClient.GetTaskGroupsAsync(teamProject.Name);

                    if (!taskGroups.Any())
                    {
                        MessageBox.Show("The selected Team Project does not contain any task groups.", "No Task Groups", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {


                        var picker = new ItemsPickerDialog();
                        picker.ItemDisplayMemberPath = nameof(TaskGroup.Name);
                        picker.Owner = Application.Current.MainWindow;
                        picker.Title = "Select the task groups to add";
                        picker.SelectionMode = SelectionMode.Multiple;
                        picker.AvailableItems = taskGroups.Select(grp => grp.ConvertToTaskGroupCreateParameter());
                        if (picker.ShowDialog() == true)
                        {
                            foreach (var taskGroupToImport in picker.SelectedItems.Cast<TaskGroupCreateParameter>().ToArray())
                            {
                                this.TaskGroupsToImport.Add(taskGroupToImport);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region RemoveSelectedTaskGroupsToImport Command

        private bool CanRemoveSelectedTaskGroupsToImport(object argument)
        {
            return this.SelectedTaskGroupsToImport != null && this.SelectedTaskGroupsToImport.Any();
        }

        private void RemoveSelectedTaskGroupsToImport(object argument)
        {
            foreach (var selectedTaskGroupToImport in this.SelectedTaskGroupsToImport)
            {
                this.TaskGroupsToImport.Remove(selectedTaskGroupToImport);
            }
        }

        #endregion

        #region RemoveAllTaskGroupsToImport Command

        private bool CanRemoveAllTaskGroupsToImport(object argument)
        {
            return this.TaskGroupsToImport != null && this.TaskGroupsToImport.Any();
        }

        private void RemoveAllTaskGroupsToImport(object argument)
        {
            this.TaskGroupsToImport.Clear();
        }

        #endregion

        #region ImportTaskGroups Command

        private bool CanImportTaskGroups(object argument)
        {
            return this.IsAnyTeamProjectSelected() && this.TaskGroupsToImport.Any();
        }

        private async Task ImportTaskGroups(object argument)
        {
            var result = MessageBox.Show("This will import the selected task groups. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            var teamProjects = this.SelectedTeamProjects.ToList();
            var taskGroups = this.TaskGroupsToImport.ToArray();
            var task = new ApplicationTask("Importing task groups", teamProjects.Count * taskGroups.Length, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var taskAgentClient = tfs.GetClient<TaskAgentHttpClient>();

                var step = 0;
                foreach (var teamProject in teamProjects)
                {
                    task.Status = "Processing Team Project \"{0}\"".FormatCurrent(teamProject.Name);
                    foreach (var taskGroup in taskGroups)
                    {
                        try
                        {
                            task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Importing task group \"{0}\" into Team Project \"{1}\"", taskGroup.Name, teamProject.Name));
                            await taskAgentClient.AddTaskGroupAsync(teamProject.Guid, taskGroup);
                        }
                        catch (MetaTaskDefinitionExistsException)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "The task group \"{0}\" already exists in Team Project \"{1}\"", taskGroup.Name, teamProject.Name));
                        }
                        catch (Exception exc)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while importing task group \"{0}\" into Team Project \"{1}\"", taskGroup.Name, teamProject.Name), exc);
                        }
                        if (task.IsCanceled)
                        {
                            break;
                        }
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                task.SetComplete("Imported " + taskGroups.Length.ToCountString("task group"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while importing task groups", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion
    }
}