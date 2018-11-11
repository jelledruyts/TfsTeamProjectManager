using Prism.Events;
using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.XamlBuild.BuildDefinitions
{
    [Export]
    public class BuildDefinitionsViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetBuildDefinitionsCommand { get; private set; }
        public RelayCommand UpdateSelectedBuildDefinitionsCommand { get; private set; }
        public RelayCommand DeleteSelectedBuildDefinitionsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<BuildDefinitionInfo> BuildDefinitions
        {
            get { return this.GetValue(BuildDefinitionsProperty); }
            set { this.SetValue(BuildDefinitionsProperty, value); }
        }

        public static ObservableProperty<ICollection<BuildDefinitionInfo>> BuildDefinitionsProperty = new ObservableProperty<ICollection<BuildDefinitionInfo>, BuildDefinitionsViewModel>(o => o.BuildDefinitions);

        public ICollection<BuildDefinitionInfo> SelectedBuildDefinitions
        {
            get { return this.GetValue(SelectedBuildDefinitionsProperty); }
            set { this.SetValue(SelectedBuildDefinitionsProperty, value); }
        }

        public static ObservableProperty<ICollection<BuildDefinitionInfo>> SelectedBuildDefinitionsProperty = new ObservableProperty<ICollection<BuildDefinitionInfo>, BuildDefinitionsViewModel>(o => o.SelectedBuildDefinitions);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public BuildDefinitionsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "XAML Build Definitions", "Allows you to manage the XAML build definitions for Team Projects.")
        {
            this.GetBuildDefinitionsCommand = new RelayCommand(GetBuildDefinitions, CanGetBuildDefinitions);
            this.UpdateSelectedBuildDefinitionsCommand = new RelayCommand(UpdateSelectedBuildDefinitions, CanUpdateSelectedBuildDefinitions);
            this.DeleteSelectedBuildDefinitionsCommand = new RelayCommand(DeleteSelectedBuildDefinitions, CanDeleteSelectedBuildDefinitions);
        }

        #endregion

        #region Commands

        private bool CanGetBuildDefinitions(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetBuildDefinitions(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving build definitions", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetService<IBuildServer>();

                var step = 0;
                var buildDefinitions = new List<BuildDefinitionInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        foreach (var buildDefinition in buildServer.QueryBuildDefinitions(teamProject.Name, QueryOptions.All))
                        {
                            buildDefinitions.Add(new BuildDefinitionInfo(teamProject.Name, buildDefinition));
                        }
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

                e.Result = buildDefinitions;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving build definitions", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.BuildDefinitions = (ICollection<BuildDefinitionInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.BuildDefinitions.Count.ToCountString("build definition"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanUpdateSelectedBuildDefinitions(object argument)
        {
            return this.SelectedBuildDefinitions != null && this.SelectedBuildDefinitions.Count > 0;
        }

        private void UpdateSelectedBuildDefinitions(object argument)
        {
            var tfs = GetSelectedTfsTeamProjectCollection();
            var buildServer = tfs.GetService<IBuildServer>();
            var buildDefinitionsToUpdate = this.SelectedBuildDefinitions;
            var availableBuildControllers = buildServer.QueryBuildControllers(false);
            var dialog = new BuildDefinitionUpdateDialog(buildDefinitionsToUpdate, availableBuildControllers.Select(c => c.Name).ToList());
            dialog.Owner = Application.Current.MainWindow;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                var buildDefinitionInfoUpdate = dialog.BuildDefinitionInfoUpdate;
                var task = new ApplicationTask("Updating build definitions", buildDefinitionsToUpdate.Count, true);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var step = 0;
                    var count = 0;
                    foreach (var buildDefinitionToUpdate in buildDefinitionsToUpdate)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Updating build definition \"{0}\" in Team Project \"{1}\"", buildDefinitionToUpdate.Name, buildDefinitionToUpdate.TeamProject));
                        try
                        {
                            var buildDefinition = buildServer.GetBuildDefinition(buildDefinitionToUpdate.Uri);
                            var availableProcessTemplates = buildServer.QueryProcessTemplates(buildDefinitionToUpdate.TeamProject);
                            buildDefinitionInfoUpdate.Update(task, buildDefinition, availableBuildControllers, availableProcessTemplates);
                            buildDefinition.Save();
                            count++;
                        }
                        catch (Exception exc)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while updating build definition \"{0}\" in Team Project \"{1}\"", buildDefinitionToUpdate.Name, buildDefinitionToUpdate.TeamProject), exc);
                        }
                        if (task.IsCanceled)
                        {
                            task.Status = "Canceled";
                            break;
                        }
                    }

                    e.Result = count;
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while updating build definitions", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        var count = (int)e.Result;
                        task.SetComplete("Updated " + count.ToCountString("build definition"));
                    }

                    // Refresh the list.
                    GetBuildDefinitions(null);
                };
                worker.RunWorkerAsync();
            }
        }

        private bool CanDeleteSelectedBuildDefinitions(object argument)
        {
            return this.SelectedBuildDefinitions != null && this.SelectedBuildDefinitions.Count > 0;
        }

        private void DeleteSelectedBuildDefinitions(object argument)
        {
            var dialog = new BuildDefinitionDeleteDialog();
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var buildDefinitionsToDelete = this.SelectedBuildDefinitions;
                var task = new ApplicationTask("Deleting build definitions", buildDefinitionsToDelete.Count, true);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var tfs = GetSelectedTfsTeamProjectCollection();
                    var buildServer = tfs.GetService<IBuildServer>();

                    var step = 0;
                    var count = 0;
                    foreach (var buildDefinitionToDelete in buildDefinitionsToDelete)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting build definition \"{0}\" in Team Project \"{1}\"", buildDefinitionToDelete.Name, buildDefinitionToDelete.TeamProject));
                        try
                        {
                            if (dialog.DeleteBuilds)
                            {
                                DeleteBuildsForDefinition(task, buildServer, step, buildDefinitionToDelete);
                            }

                            // Delete the build definitions one by one to avoid one failure preventing the other build definitions from being deleted.
                            buildServer.DeleteBuildDefinitions(new Uri[] { buildDefinitionToDelete.Uri });
                            count++;
                        }
                        catch (Exception exc)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting build definition \"{0}\" in Team Project \"{1}\"", buildDefinitionToDelete.Name, buildDefinitionToDelete.TeamProject), exc);
                        }
                        if (task.IsCanceled)
                        {
                            task.Status = "Canceled";
                            break;
                        }
                    }

                    e.Result = count;
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while deleting build definitions", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        var count = (int)e.Result;
                        task.SetComplete("Deleted " + count.ToCountString("build definition"));
                    }

                    // Refresh the list.
                    GetBuildDefinitions(null);
                };
                worker.RunWorkerAsync();
            }
        }

        private static void DeleteBuildsForDefinition(ApplicationTask task, IBuildServer buildServer, int step, BuildDefinitionInfo buildDefinition)
        {
            var buildDetailSpec = buildServer.CreateBuildDetailSpec(new[] { buildDefinition.Uri, });
            var buildsToDelete = buildServer.QueryBuilds(buildDetailSpec).Builds;
            if (buildsToDelete.Any())
            {
                task.SetProgress(step, string.Format(CultureInfo.CurrentCulture, "Deleting {0} builds for definition \"{1}\" in Team Project \"{2}\"", buildsToDelete.Count(), buildDefinition.Name, buildDefinition.TeamProject));
                foreach (var buildToDelete in buildsToDelete)
                {
                    try
                    {
                        buildToDelete.Delete(DeleteOptions.All);
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting build number \"{0}\" for definition \"{1}\" in Team Project \"{2}\"", buildToDelete.BuildNumber, buildDefinition.Name, buildDefinition.TeamProject), exc);
                    }
                }
            }
        }

        #endregion
    }
}