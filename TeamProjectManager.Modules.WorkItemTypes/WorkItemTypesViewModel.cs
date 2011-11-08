using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Practices.Prism.Events;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [Export]
    public class WorkItemTypesViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand BrowseWorkItemTypesFilePathCommand { get; private set; }
        public RelayCommand ValidateCommand { get; private set; }
        public RelayCommand ValidateAndImportCommand { get; private set; }
        public RelayCommand ImportCommand { get; private set; }

        #endregion

        #region Observable Properties

        public string WorkItemTypesFilePath
        {
            get { return this.GetValue(WorkItemTypesFilePathProperty); }
            set { this.SetValue(WorkItemTypesFilePathProperty, value); }
        }

        public static ObservableProperty<string> WorkItemTypesFilePathProperty = new ObservableProperty<string, WorkItemTypesViewModel>(o => o.WorkItemTypesFilePath, OnWorkItemTypesFilePathChanged);

        public ICollection<WorkItemTypeFile> WorkItemTypeFiles
        {
            get { return this.GetValue(WorkItemTypeFilesProperty); }
            set { this.SetValue(WorkItemTypeFilesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeFile>> WorkItemTypeFilesProperty = new ObservableProperty<ICollection<WorkItemTypeFile>, WorkItemTypesViewModel>(o => o.WorkItemTypeFiles);

        public ICollection<WorkItemTypeFile> SelectedWorkItemTypeFiles
        {
            get { return this.GetValue(SelectedWorkItemTypeFilesProperty); }
            set { this.SetValue(SelectedWorkItemTypeFilesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeFile>> SelectedWorkItemTypeFilesProperty = new ObservableProperty<ICollection<WorkItemTypeFile>, WorkItemTypesViewModel>(o => o.SelectedWorkItemTypeFiles);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemTypesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base("Work Item Types", eventAggregator, logger)
        {
            this.BrowseWorkItemTypesFilePathCommand = new RelayCommand(BrowseWorkItemTypesFilePath, CanBrowseWorkItemTypesFilePath);
            this.ValidateCommand = new RelayCommand(Validate, CanValidate);
            this.ValidateAndImportCommand = new RelayCommand(ValidateAndImport, CanValidateAndImport);
            this.ImportCommand = new RelayCommand(Import, CanImport);
        }

        #endregion

        #region Events

        private static void OnWorkItemTypesFilePathChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<string> args)
        {
            var viewModel = (WorkItemTypesViewModel)sender;
            var path = viewModel.WorkItemTypesFilePath;
            if (Directory.Exists(path))
            {
                viewModel.WorkItemTypeFiles = Directory.GetFiles(path, "*.xml").Select(f => new WorkItemTypeFile(f)).ToList();
            }
            else
            {
                viewModel.WorkItemTypeFiles = null;
            }
        }

        #endregion

        #region Commands

        private bool CanBrowseWorkItemTypesFilePath(object arguments)
        {
            return true;
        }

        private void BrowseWorkItemTypesFilePath(object argument)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Please select the path where the Work Item Type Definition files (*.xml) are stored.";
            dialog.SelectedPath = this.WorkItemTypesFilePath;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.WorkItemTypesFilePath = dialog.SelectedPath;
            }
        }

        private bool CanValidate(object argument)
        {
            return this.SelectedTeamProjects != null && this.SelectedTeamProjects.Count > 0 && this.SelectedWorkItemTypeFiles != null && this.SelectedWorkItemTypeFiles.Count > 0;
        }

        private void Validate(object argument)
        {
            PerformImport("Validating work item types", ImportOptions.Validate);
        }

        private bool CanValidateAndImport(object argument)
        {
            return CanValidate(argument);
        }

        private void ValidateAndImport(object argument)
        {
            PerformImport("Validating and importing work item types", ImportOptions.Validate | ImportOptions.Import);
        }

        private bool CanImport(object argument)
        {
            return CanValidate(argument);
        }

        private void Import(object argument)
        {
            PerformImport("Importing work item types", ImportOptions.Import);
        }

        #endregion

        #region Helper Methods

        private void PerformImport(string description, ImportOptions options)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var numberOfSteps = GetTotalNumberOfSteps(options, teamProjectNames, this.SelectedWorkItemTypeFiles);
            var task = new ApplicationTask(description, numberOfSteps);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                new WorkItemTypeImporter().ImportWorkItemTypes(task, options, this.SelectedTeamProjectCollection.Uri, teamProjectNames, this.SelectedWorkItemTypeFiles);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while " + description.ToLower(CultureInfo.CurrentCulture), e.Error);
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

        private static int GetTotalNumberOfSteps(ImportOptions options, ICollection<string> teamProjectNames, ICollection<WorkItemTypeFile> workItemTypeFiles)
        {
            var numberOfSteps = 0;
            var numberOfImports = teamProjectNames.Count * workItemTypeFiles.Count;
            if (options.HasFlag(ImportOptions.Validate))
            {
                numberOfSteps += numberOfImports;
            }
            if (options.HasFlag(ImportOptions.Import))
            {
                numberOfSteps += numberOfImports;
            }
            return numberOfSteps;
        }

        #endregion
    }
}