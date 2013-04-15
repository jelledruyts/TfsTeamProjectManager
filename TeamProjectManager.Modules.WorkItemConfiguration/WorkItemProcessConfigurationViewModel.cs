using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public class WorkItemProcessConfigurationViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetProcessConfigurationsCommand { get; private set; }
        public RelayCommand ExportSelectedProcessConfigurationsCommand { get; private set; }
        public RelayCommand EditSelectedProcessConfigurationsCommand { get; private set; }
        public RelayCommand BrowseCommonConfigurationFilePathCommand { get; private set; }
        public RelayCommand BrowseAgileConfigurationFilePathCommand { get; private set; }
        public RelayCommand ImportProcessConfigurationsCommand { get; private set; }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets or sets the process configurations.
        /// </summary>
        public ICollection<WorkItemConfigurationItemExport> ProcessConfigurations
        {
            get { return this.GetValue(ProcessConfigurationsProperty); }
            set { this.SetValue(ProcessConfigurationsProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="ProcessConfigurations"/> observable property.
        /// </summary>
        public static readonly ObservableProperty<ICollection<WorkItemConfigurationItemExport>> ProcessConfigurationsProperty = new ObservableProperty<ICollection<WorkItemConfigurationItemExport>, WorkItemProcessConfigurationViewModel>(o => o.ProcessConfigurations);

        /// <summary>
        /// Gets or sets the selected process configurations.
        /// </summary>
        public ICollection<WorkItemConfigurationItemExport> SelectedProcessConfigurations
        {
            get { return this.GetValue(SelectedProcessConfigurationsProperty); }
            set { this.SetValue(SelectedProcessConfigurationsProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="SelectedProcessConfigurations"/> observable property.
        /// </summary>
        public static readonly ObservableProperty<ICollection<WorkItemConfigurationItemExport>> SelectedProcessConfigurationsProperty = new ObservableProperty<ICollection<WorkItemConfigurationItemExport>, WorkItemProcessConfigurationViewModel>(o => o.SelectedProcessConfigurations);

        /// <summary>
        /// Gets or sets the path to the common process configuration file.
        /// </summary>
        public string CommonConfigurationFilePath
        {
            get { return this.GetValue(CommonConfigurationFilePathProperty); }
            set { this.SetValue(CommonConfigurationFilePathProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="CommonConfigurationFilePath"/> observable property.
        /// </summary>
        public static readonly ObservableProperty<string> CommonConfigurationFilePathProperty = new ObservableProperty<string, WorkItemProcessConfigurationViewModel>(o => o.CommonConfigurationFilePath);

        /// <summary>
        /// Gets or sets the path to the agile process configuration file.
        /// </summary>
        public string AgileConfigurationFilePath
        {
            get { return this.GetValue(AgileConfigurationFilePathProperty); }
            set { this.SetValue(AgileConfigurationFilePathProperty, value); }
        }

        /// <summary>
        /// The definition for the <see cref="AgileConfigurationFilePath"/> observable property.
        /// </summary>
        public static readonly ObservableProperty<string> AgileConfigurationFilePathProperty = new ObservableProperty<string, WorkItemProcessConfigurationViewModel>(o => o.AgileConfigurationFilePath);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemProcessConfigurationViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Process Configuration", "Allows you to manage work item process configurations.")
        {
            this.GetProcessConfigurationsCommand = new RelayCommand(GetProcessConfigurations, CanGetProcessConfigurations);
            this.ExportSelectedProcessConfigurationsCommand = new RelayCommand(ExportSelectedProcessConfigurations, CanExportSelectedProcessConfigurations);
            this.EditSelectedProcessConfigurationsCommand = new RelayCommand(EditSelectedProcessConfigurations, CanEditSelectedProcessConfigurations);
            this.BrowseCommonConfigurationFilePathCommand = new RelayCommand(BrowseCommonConfigurationFilePath, CanBrowseCommonConfigurationFilePath);
            this.BrowseAgileConfigurationFilePathCommand = new RelayCommand(BrowseAgileConfigurationFilePath, CanBrowseAgileConfigurationFilePath);
            this.ImportProcessConfigurationsCommand = new RelayCommand(ImportProcessConfigurations, CanImportProcessConfigurations);
        }

        #endregion

        #region GetProcessConfigurations Command

        private bool CanGetProcessConfigurations(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetProcessConfigurations(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving process configurations", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var results = new List<WorkItemConfigurationItemExport>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var project = store.Projects[teamProject.Name];
                        var commonConfiguration = WorkItemConfigurationItemImportExport.GetCommonConfiguration(tfs, project);
                        if (commonConfiguration != null)
                        {
                            results.Add(new WorkItemConfigurationItemExport(teamProject, commonConfiguration));
                        }
                        var agileConfiguration = WorkItemConfigurationItemImportExport.GetAgileConfiguration(tfs, project);
                        if (agileConfiguration != null)
                        {
                            results.Add(new WorkItemConfigurationItemExport(teamProject, agileConfiguration));
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
                e.Result = results;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving process configurations", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.ProcessConfigurations = (ICollection<WorkItemConfigurationItemExport>)e.Result;
                    task.SetComplete("Retrieved " + this.ProcessConfigurations.Count.ToCountString("process configuration"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion

        #region ExportSelectedProcessConfigurations Command

        private bool CanExportSelectedProcessConfigurations(object argument)
        {
            return (this.SelectedProcessConfigurations != null && this.SelectedProcessConfigurations.Count > 0);
        }

        private void ExportSelectedProcessConfigurations(object argument)
        {
            var processConfigurationsToExport = new List<WorkItemConfigurationItemExport>();
            var processConfigurationExports = this.SelectedProcessConfigurations.ToList();
            if (processConfigurationExports.Count == 1)
            {
                // Export to single file.
                var processConfigurationExport = processConfigurationExports.Single();
                var dialog = new SaveFileDialog();
                dialog.FileName = processConfigurationExport.Item.Type.ToString() + ".xml";
                dialog.Filter = "XML Files (*.xml)|*.xml";
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    processConfigurationsToExport.Add(new WorkItemConfigurationItemExport(processConfigurationExport.TeamProject, processConfigurationExport.Item, dialog.FileName));
                }
            }
            else
            {
                // Export to a directory structure.
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select the path where to export the Process Configuration files (*.xml). They will be stored in a folder per Team Project.";
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var rootFolder = dialog.SelectedPath;
                    foreach (var processConfigurationExport in processConfigurationExports)
                    {
                        var fileName = Path.Combine(rootFolder, processConfigurationExport.TeamProject.Name, processConfigurationExport.Item.Type.ToString() + ".xml");
                        processConfigurationsToExport.Add(new WorkItemConfigurationItemExport(processConfigurationExport.TeamProject, processConfigurationExport.Item, fileName));
                    }
                }
            }

            var task = new ApplicationTask("Exporting process configurations", processConfigurationExports.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                WorkItemConfigurationItemImportExport.ExportWorkItemConfigurationItems(task, "process configuration", processConfigurationsToExport);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while exporting process configurations", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete("Exported " + processConfigurationsToExport.Count.ToCountString("process configuration"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion

        #region EditSelectedProcessConfigurations Command

        private bool CanEditSelectedProcessConfigurations(object argument)
        {
            return IsAnyTeamProjectSelected() && this.SelectedProcessConfigurations != null && this.SelectedProcessConfigurations.Count > 0;
        }

        private void EditSelectedProcessConfigurations(object argument)
        {
            var processConfigurationsToEdit = this.SelectedProcessConfigurations.ToList();
            var dialog = new WorkItemConfigurationItemEditorDialog(processConfigurationsToEdit, "Process Configuration");
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var result = MessageBox.Show("This will import the edited process configurations. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var teamProjectsWithProcessConfigurations = processConfigurationsToEdit.GroupBy(w => w.TeamProject).ToDictionary(g => g.Key, g => g.Select(w => w.Item).ToList());
                    PerformImport(teamProjectsWithProcessConfigurations);
                }
            }
        }

        #endregion

        #region BrowseCommonConfigurationFilePath Command

        private bool CanBrowseCommonConfigurationFilePath(object argument)
        {
            return true;
        }

        private void BrowseCommonConfigurationFilePath(object argument)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the Common Process Configuration XML file to import.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                this.CommonConfigurationFilePath = dialog.FileName;
            }
        }

        #endregion

        #region BrowseAgileConfigurationFilePath Command

        private bool CanBrowseAgileConfigurationFilePath(object argument)
        {
            return true;
        }

        private void BrowseAgileConfigurationFilePath(object argument)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the Agile Process Configuration XML file to import.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                this.AgileConfigurationFilePath = dialog.FileName;
            }
        }

        #endregion

        #region ImportProcessConfigurations Command

        private bool CanImportProcessConfigurations(object argument)
        {
            return IsAnyTeamProjectSelected() && !(string.IsNullOrEmpty(this.CommonConfigurationFilePath) && string.IsNullOrEmpty(this.AgileConfigurationFilePath));
        }

        private void ImportProcessConfigurations(object argument)
        {
            var result = MessageBox.Show("This will import the specified process configurations. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var processConfigurations = new List<WorkItemConfigurationItem>();
                if (File.Exists(this.CommonConfigurationFilePath))
                {
                    processConfigurations.Add(WorkItemConfigurationItem.FromFile(this.CommonConfigurationFilePath));
                }
                if (File.Exists(this.AgileConfigurationFilePath))
                {
                    processConfigurations.Add(WorkItemConfigurationItem.FromFile(this.AgileConfigurationFilePath));
                }
                var teamProjectsWithProcessConfigurations = this.SelectedTeamProjects.ToDictionary(p => p, p => processConfigurations);
                PerformImport(teamProjectsWithProcessConfigurations);
            }
        }

        #endregion

        #region Helper Methods

        private void PerformImport(Dictionary<TeamProjectInfo, List<WorkItemConfigurationItem>> teamProjectsWithProcessConfigurations)
        {
            var numberOfSteps = teamProjectsWithProcessConfigurations.Aggregate(0, (a, p) => a += p.Value.Count);
            var task = new ApplicationTask("Importing process configurations", numberOfSteps, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                WorkItemConfigurationItemImportExport.ImportProcessConfigurations(task, tfs, store, teamProjectsWithProcessConfigurations);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while importing process configurations", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete(task.IsError ? "Failed" : (task.IsWarning ? "Succeeded with warnings" : "Succeeded"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion
    }
}