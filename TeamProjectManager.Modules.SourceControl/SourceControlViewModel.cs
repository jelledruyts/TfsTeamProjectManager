using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.SourceControl
{
    [Export]
    public class SourceControlViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetSourceControlSettingsCommand { get; private set; }
        public RelayCommand LoadSourceControlSettingsCommand { get; private set; }
        public RelayCommand UpdateSourceControlSettingsCommand { get; private set; }
        public RelayCommand ViewBranchHierarchiesCommand { get; private set; }
        public RelayCommand ExportBranchHierarchiesCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ObservableCollection<SourceControlSettings> SourceControlSettings
        {
            get { return this.GetValue(SourceControlSettingsProperty); }
            set { this.SetValue(SourceControlSettingsProperty, value); }
        }

        public static ObservableProperty<ObservableCollection<SourceControlSettings>> SourceControlSettingsProperty = new ObservableProperty<ObservableCollection<SourceControlSettings>, SourceControlViewModel>(o => o.SourceControlSettings);

        public SourceControlSettings SelectedSourceControlSettings
        {
            get { return this.GetValue(SelectedSourceControlSettingsProperty); }
            set { this.SetValue(SelectedSourceControlSettingsProperty, value); }
        }

        public static ObservableProperty<SourceControlSettings> SelectedSourceControlSettingsProperty = new ObservableProperty<SourceControlSettings, SourceControlViewModel>(o => o.SelectedSourceControlSettings, new SourceControlSettings());

        public string BranchHierarchiesInfoMessage
        {
            get { return this.GetValue(BranchHierarchiesInfoMessageProperty); }
            set { this.SetValue(BranchHierarchiesInfoMessageProperty, value); }
        }

        public static readonly ObservableProperty<string> BranchHierarchiesInfoMessageProperty = new ObservableProperty<string, SourceControlViewModel>(o => o.BranchHierarchiesInfoMessage);

        public IList<BranchInfo> BranchHierarchies
        {
            get { return this.GetValue(BranchHierarchiesProperty); }
            set { this.SetValue(BranchHierarchiesProperty, value); }
        }

        public static readonly ObservableProperty<IList<BranchInfo>> BranchHierarchiesProperty = new ObservableProperty<IList<BranchInfo>, SourceControlViewModel>(o => o.BranchHierarchies);

        public BranchHierarchyExportFormat ExportFormat
        {
            get { return this.GetValue(ExportFormatProperty); }
            set { this.SetValue(ExportFormatProperty, value); }
        }

        public static readonly ObservableProperty<BranchHierarchyExportFormat> ExportFormatProperty = new ObservableProperty<BranchHierarchyExportFormat, SourceControlViewModel>(o => o.ExportFormat);

        public bool ExportBranchHierarchiesPerTeamProject
        {
            get { return this.GetValue(ExportBranchHierarchiesPerTeamProjectProperty); }
            set { this.SetValue(ExportBranchHierarchiesPerTeamProjectProperty, value); }
        }

        public static readonly ObservableProperty<bool> ExportBranchHierarchiesPerTeamProjectProperty = new ObservableProperty<bool, SourceControlViewModel>(o => o.ExportBranchHierarchiesPerTeamProject);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public SourceControlViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "TFVC", "Allows you to manage Team Foundation Version Control settings for Team Projects and see branch hierarchies.")
        {
            this.GetSourceControlSettingsCommand = new RelayCommand(GetSourceControlSettings, CanGetSourceControlSettings);
            this.LoadSourceControlSettingsCommand = new RelayCommand(LoadSourceControlSettings, CanLoadSourceControlSettings);
            this.UpdateSourceControlSettingsCommand = new RelayCommand(UpdateSourceControlSettings, CanUpdateSourceControlSettings);
            this.ViewBranchHierarchiesCommand = new RelayCommand(ViewBranchHierarchies, CanViewBranchHierarchies);
            this.ExportBranchHierarchiesCommand = new RelayCommand(ExportBranchHierarchies, CanExportBranchHierarchies);
        }

        #endregion

        #region Commands

        private bool CanGetSourceControlSettings(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetSourceControlSettings(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving source control settings", teamProjectNames.Count, true);
            this.PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var settings = new ObservableCollection<SourceControlSettings>();
                var step = 0;
                var tfs = GetSelectedTfsTeamProjectCollection();
                var vcs = tfs.GetService<VersionControlServer>();
                foreach (var teamProjectName in teamProjectNames)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                    try
                    {
                        var projectSettings = GetSourceControlSettings(vcs, teamProjectName);
                        settings.Add(projectSettings);
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProjectName), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                e.Result = settings;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving source control settings", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.SourceControlSettings = (ObservableCollection<SourceControlSettings>)e.Result;
                    task.SetComplete("Done");
                }
            };
            worker.RunWorkerAsync();
        }

        private static SourceControlSettings GetSourceControlSettings(VersionControlServer vcs, string teamProjectName)
        {
            var teamProject = vcs.GetTeamProject(teamProjectName);
            var checkinNoteFields = teamProject.GetCheckinNoteFields().Select(f => new CheckinNoteField(f.DisplayOrder, f.Name, f.Required));
            return new SourceControlSettings(teamProject.Name, !teamProject.ExclusiveCheckout, teamProject.GetLatestOnCheckout, checkinNoteFields);
        }

        private bool CanLoadSourceControlSettings(object argument)
        {
            return true;
        }

        private void LoadSourceControlSettings(object argument)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                {
                    var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                    var teamProject = dialog.SelectedProjects.First();

                    var task = new ApplicationTask("Loading source control settings");
                    PublishStatus(new StatusEventArgs(task));
                    var worker = new BackgroundWorker();
                    worker.DoWork += (sender, e) =>
                    {
                        var tfs = GetSelectedTfsTeamProjectCollection();
                        var vcs = tfs.GetService<VersionControlServer>();
                        var projectSettings = GetSourceControlSettings(vcs, teamProject.Name);
                        e.Result = projectSettings;
                    };
                    worker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (e.Error != null)
                        {
                            Logger.Log("An unexpected exception occurred while loading source control settings", e.Error);
                            task.SetError(e.Error);
                            task.SetComplete("An unexpected exception occurred");
                        }
                        else
                        {
                            this.SelectedSourceControlSettings = (SourceControlSettings)e.Result;
                            task.SetComplete("Done");
                        }
                    };
                    worker.RunWorkerAsync();
                }
            }
        }

        private bool CanUpdateSourceControlSettings(object argument)
        {
            return IsAnyTeamProjectSelected() && this.SelectedSourceControlSettings != null;
        }

        private void UpdateSourceControlSettings(object argument)
        {
            var result = MessageBox.Show("This will update the source control settings in all selected Team Projects. Are you sure you want to continue?", "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
                var settings = this.SelectedSourceControlSettings;
                var task = new ApplicationTask("Updating source control settings", teamProjectNames.Count, true);
                this.PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var step = 0;
                    var tfs = GetSelectedTfsTeamProjectCollection();
                    var vcs = tfs.GetService<VersionControlServer>();
                    foreach (var teamProjectName in teamProjectNames)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                        try
                        {
                            var project = vcs.GetTeamProject(teamProjectName);
                            project.ExclusiveCheckout = !settings.EnableMultipleCheckout;
                            project.GetLatestOnCheckout = settings.EnableGetLatestOnCheckout;
                            var checkinNoteFields = settings.CheckinNoteFields.Select(f => new CheckinNoteFieldDefinition(f.Name, f.Required, f.DisplayOrder)).ToArray();
                            project.SetCheckinNoteFields(checkinNoteFields);
                        }
                        catch (Exception exc)
                        {
                            task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProjectName), exc);
                        }
                        if (task.IsCanceled)
                        {
                            task.Status = "Canceled";
                            break;
                        }
                    }
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while updating source control settings", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        task.SetComplete("Done");
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        private bool CanViewBranchHierarchies(object argument)
        {
            return this.IsAnyTeamProjectSelected();
        }

        private void ViewBranchHierarchies(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving branch hierarchies", null, true);
            this.PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var vcs = tfs.GetService<VersionControlServer>();
                var teamProjectRootPaths = teamProjectNames.Select(t => string.Concat("$/", t, "/")).ToArray();
                var branchHierarchies = new List<BranchInfo>();
                foreach (var rootBranch in vcs.QueryRootBranchObjects(RecursionType.OneLevel).Where(b => !teamProjectRootPaths.Any() || teamProjectRootPaths.Any(t => (b.Properties.RootItem.Item + "/").StartsWith(t, StringComparison.OrdinalIgnoreCase))).OrderBy(b => b.Properties.RootItem.Item))
                {
                    branchHierarchies.Add(GetBranchInfo(rootBranch, null, vcs, task));
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                e.Result = branchHierarchies;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving branch hierarchies", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                    this.BranchHierarchiesInfoMessage = null;
                }
                else
                {
                    this.BranchHierarchies = (IList<BranchInfo>)e.Result;
                    var totalBranchCount = this.BranchHierarchies.Count + this.BranchHierarchies.Sum(b => b.RecursiveChildCount);
                    var maxTreeDepth = this.BranchHierarchies.Any() ? this.BranchHierarchies.Max(b => b.MaxTreeDepth) : 0;
                    var infoMessage = "Retrieved {0} with a total of {1} and a maximum depth of {2}".FormatCurrent(this.BranchHierarchies.Count.ToCountString("branch hierarchy"), totalBranchCount.ToCountString("branch"), maxTreeDepth);
                    task.SetComplete(infoMessage);
                    this.BranchHierarchiesInfoMessage = infoMessage;
                }
            };
            worker.RunWorkerAsync();
        }

        private static BranchInfo GetBranchInfo(BranchObject branch, BranchInfo parent, VersionControlServer vcs, ApplicationTask task)
        {
            var branchPath = branch.Properties.RootItem.Item;
            task.Status = "Processing " + branchPath;
            var current = new BranchInfo(parent, branchPath, branch.Properties.Description, branch.DateCreated, branch.Properties.Owner);
            var children = new List<BranchInfo>();
            foreach (var childBranch in vcs.QueryBranchObjects(branch.Properties.RootItem, RecursionType.OneLevel).Where(c => c.Properties.RootItem.Item != branchPath).OrderBy(c => c.Properties.RootItem.Item))
            {
                if (task.IsCanceled)
                {
                    task.Status = "Canceled";
                    break;
                }
                children.Add(GetBranchInfo(childBranch, current, vcs, task));
            }
            current.Children = children.ToArray();
            return current;
        }

        private bool CanExportBranchHierarchies(object argument)
        {
            return this.BranchHierarchies != null && this.BranchHierarchies.Any();
        }

        private void ExportBranchHierarchies(object argument)
        {
            if (!this.ExportBranchHierarchiesPerTeamProject)
            {
                var dialog = new SaveFileDialog();
                dialog.Title = "Please select the branch hierarchy {0} file to save.".FormatCurrent(this.ExportFormat.ToString().ToUpperInvariant());
                dialog.Filter = (this.ExportFormat == BranchHierarchyExportFormat.Dgml ? "DGML Files (*.dgml)|*.dgml" : "XML Files (*.xml)|*.xml");
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    try
                    {
                        BranchHierarchyExporter.Export(this.BranchHierarchies, dialog.FileName, this.ExportFormat);
                    }
                    catch (Exception exc)
                    {
                        this.Logger.Log(string.Format(CultureInfo.CurrentCulture, "An error occurred while saving the branch hierarchies to \"{0}\"", dialog.FileName), exc);
                        MessageBox.Show("An error occurred while saving the branch hierarchies. See the log file for details", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select the path where to save the branch hierarchy {0} files. They will be stored in a file per Team Project.".FormatCurrent(this.ExportFormat.ToString().ToUpperInvariant());
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var rootFolder = dialog.SelectedPath;
                    foreach (var teamProjectWithBranchHierarchies in this.BranchHierarchies.GroupBy(h => h.TeamProjectName))
                    {
                        var fileName = Path.Combine(rootFolder, teamProjectWithBranchHierarchies.Key + "." + this.ExportFormat.ToString().ToLower());
                        BranchHierarchyExporter.Export(teamProjectWithBranchHierarchies, fileName, this.ExportFormat);
                    }
                }
            }
        }

        #endregion
    }
}