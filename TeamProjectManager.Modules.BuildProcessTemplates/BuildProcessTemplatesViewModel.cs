using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.BuildProcessTemplates
{
    [Export]
    public class BuildProcessTemplatesViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand RegisterBuildProcessTemplateCommand { get; private set; }
        public RelayCommand GetBuildProcessTemplatesCommand { get; private set; }
        public RelayCommand DeleteSelectedBuildProcessTemplatesCommand { get; private set; }
        public RelayCommand BrowseTemplateServerPathCommand { get; private set; }

        #endregion

        #region Observable Properties

        public string TemplateServerPath
        {
            get { return this.GetValue(TemplateServerPathProperty); }
            set { this.SetValue(TemplateServerPathProperty, value); }
        }

        public static ObservableProperty<string> TemplateServerPathProperty = new ObservableProperty<string, BuildProcessTemplatesViewModel>(o => o.TemplateServerPath);

        public ProcessTemplateType TemplateType
        {
            get { return this.GetValue(TemplateTypeProperty); }
            set { this.SetValue(TemplateTypeProperty, value); }
        }

        public static ObservableProperty<ProcessTemplateType> TemplateTypeProperty = new ObservableProperty<ProcessTemplateType, BuildProcessTemplatesViewModel>(o => o.TemplateType);

        public bool UnregisterAllOtherTemplates
        {
            get { return this.GetValue(UnregisterAllOtherTemplatesProperty); }
            set { this.SetValue(UnregisterAllOtherTemplatesProperty, value); }
        }

        public static ObservableProperty<bool> UnregisterAllOtherTemplatesProperty = new ObservableProperty<bool, BuildProcessTemplatesViewModel>(o => o.UnregisterAllOtherTemplates);

        public bool UnregisterAllOtherTemplatesIncludesUpgradeTemplate
        {
            get { return this.GetValue(UnregisterAllOtherTemplatesIncludesUpgradeTemplateProperty); }
            set { this.SetValue(UnregisterAllOtherTemplatesIncludesUpgradeTemplateProperty, value); }
        }

        public static ObservableProperty<bool> UnregisterAllOtherTemplatesIncludesUpgradeTemplateProperty = new ObservableProperty<bool, BuildProcessTemplatesViewModel>(o => o.UnregisterAllOtherTemplatesIncludesUpgradeTemplate);

        public bool Simulate
        {
            get { return this.GetValue(SimulateProperty); }
            set { this.SetValue(SimulateProperty, value); }
        }

        public static ObservableProperty<bool> SimulateProperty = new ObservableProperty<bool, BuildProcessTemplatesViewModel>(o => o.Simulate);

        public IEnumerable<BuildProcessTemplateInfo> BuildProcessTemplates
        {
            get { return this.GetValue(BuildProcessTemplatesProperty); }
            set { this.SetValue(BuildProcessTemplatesProperty, value); }
        }

        public static ObservableProperty<IEnumerable<BuildProcessTemplateInfo>> BuildProcessTemplatesProperty = new ObservableProperty<IEnumerable<BuildProcessTemplateInfo>, BuildProcessTemplatesViewModel>(o => o.BuildProcessTemplates);

        public ICollection<BuildProcessTemplateInfo> SelectedBuildProcessTemplates
        {
            get { return this.GetValue(SelectedBuildProcessTemplatesProperty); }
            set { this.SetValue(SelectedBuildProcessTemplatesProperty, value); }
        }

        public static ObservableProperty<ICollection<BuildProcessTemplateInfo>> SelectedBuildProcessTemplatesProperty = new ObservableProperty<ICollection<BuildProcessTemplateInfo>, BuildProcessTemplatesViewModel>(o => o.SelectedBuildProcessTemplates);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public BuildProcessTemplatesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base("Build Process Templates", eventAggregator, logger)
        {
            this.RegisterBuildProcessTemplateCommand = new RelayCommand(RegisterBuildProcessTemplate, CanRegisterBuildProcessTemplate);
            this.GetBuildProcessTemplatesCommand = new RelayCommand(GetBuildProcessTemplates, CanGetBuildProcessTemplates);
            this.DeleteSelectedBuildProcessTemplatesCommand = new RelayCommand(DeleteSelectedBuildProcessTemplates, CanDeleteSelectedBuildProcessTemplates);
            this.BrowseTemplateServerPathCommand = new RelayCommand(BrowseTemplateServerPath, CanBrowseTemplateServerPath);
        }

        #endregion

        #region Commands

        private bool CanBrowseTemplateServerPath(object argument)
        {
            return this.SelectedTeamProjectCollection != null;
        }

        private void BrowseTemplateServerPath(object argument)
        {
            using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
            {
                var vcs = tfs.GetService<VersionControlServer>();

                try
                {
                    var assembly = Assembly.GetAssembly(typeof(WorkItemPolicy));
                    var args = new object[] { vcs };
                    using (var dialog = (System.Windows.Forms.Form)assembly.CreateInstance("Microsoft.TeamFoundation.VersionControl.Controls.DialogChooseItem", false, BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, args, CultureInfo.CurrentCulture, null))
                    {
                        dialog.GetType().GetProperty("AllowFileOnly").SetValue(dialog, true, null);
                        dialog.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                        var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            var item = (Item)dialog.GetType().GetProperty("SelectedItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dialog, null);
                            if (item != null)
                            {
                                this.TemplateServerPath = item.ServerItem;
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    var message = "There was a problem showing the internal TFS version control file browser dialog.";
                    Logger.Log(message, exc, TraceEventType.Warning);
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool CanGetBuildProcessTemplates(object argument)
        {
            return (this.SelectedTeamProjectCollection != null && this.SelectedTeamProjects != null && this.SelectedTeamProjects.Count > 0);
        }

        private void GetBuildProcessTemplates(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving build process templates", teamProjectNames.Count);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                e.Result = Tasks.GetBuildProcessTemplates(task, this.SelectedTeamProjectCollection.Uri, teamProjectNames);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving build process templates", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.BuildProcessTemplates = (IEnumerable<BuildProcessTemplateInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.BuildProcessTemplates.Count().ToCountString("build process template"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanRegisterBuildProcessTemplate(object argument)
        {
            return (this.SelectedTeamProjectCollection != null && this.SelectedTeamProjects != null && this.SelectedTeamProjects.Count > 0 && !string.IsNullOrEmpty(this.TemplateServerPath));
        }

        private void RegisterBuildProcessTemplate(object argument)
        {
            var result = MessageBoxResult.Yes;
            if (!this.Simulate)
            {
                result = MessageBox.Show("This will update all selected Team Projects. Are you sure you want to continue?", "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            }
            if (result == MessageBoxResult.Yes)
            {
                var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
                var task = new ApplicationTask((this.Simulate ? "Simulating registering build process template" : "Registering build process template"), teamProjectNames.Count);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    Tasks.RegisterBuildProcessTemplate(task, this.SelectedTeamProjectCollection.Uri, teamProjectNames, this.TemplateServerPath, this.TemplateType, true, this.UnregisterAllOtherTemplates, this.UnregisterAllOtherTemplatesIncludesUpgradeTemplate, this.Simulate);
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while registering build process template", e.Error);
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

        private bool CanDeleteSelectedBuildProcessTemplates(object argument)
        {
            return (this.SelectedBuildProcessTemplates != null && this.SelectedBuildProcessTemplates.Count > 0);
        }

        private void DeleteSelectedBuildProcessTemplates(object argument)
        {
            var result = MessageBox.Show("This will delete the selected build process templates. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var buildProcessTemplates = this.SelectedBuildProcessTemplates.Select(p => p.ProcessTemplate).ToList();
                var task = new ApplicationTask("Deleting build process templates", buildProcessTemplates.Count);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    Tasks.UnregisterBuildProcessTemplates(task, buildProcessTemplates);
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while deleting build process template", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        task.SetComplete("Deleted " + buildProcessTemplates.Count.ToCountString("build process template"));
                    }
                };
                worker.RunWorkerAsync();

                // Refresh the list.
                GetBuildProcessTemplates(null);
            }
        }

        #endregion
    }
}