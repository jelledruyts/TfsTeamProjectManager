using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.SourceControl
{
    [Export]
    public class SourceControlViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetLatestChangesetsCommand { get; private set; }
        public RelayCommand ViewChangesetDetailsCommand { get; private set; }
        public RelayCommand GetSourceControlSettingsCommand { get; private set; }
        public RelayCommand LoadSourceControlSettingsCommand { get; private set; }
        public RelayCommand UpdateSourceControlSettingsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<ChangesetInfo> Changesets
        {
            get { return this.GetValue(ChangesetsProperty); }
            set { this.SetValue(ChangesetsProperty, value); }
        }

        public static ObservableProperty<ICollection<ChangesetInfo>> ChangesetsProperty = new ObservableProperty<ICollection<ChangesetInfo>, SourceControlViewModel>(o => o.Changesets);

        public ICollection<ChangesetInfo> SelectedChangesets
        {
            get { return this.GetValue(SelectedChangesetsProperty); }
            set { this.SetValue(SelectedChangesetsProperty, value); }
        }

        public static ObservableProperty<ICollection<ChangesetInfo>> SelectedChangesetsProperty = new ObservableProperty<ICollection<ChangesetInfo>, SourceControlViewModel>(o => o.SelectedChangesets);

        public int NumberOfChangesets
        {
            get { return this.GetValue(NumberOfChangesetsProperty); }
            set { this.SetValue(NumberOfChangesetsProperty, value); }
        }

        public static ObservableProperty<int> NumberOfChangesetsProperty = new ObservableProperty<int, SourceControlViewModel>(o => o.NumberOfChangesets, 1);

        public string Exclusions
        {
            get { return this.GetValue(ExclusionsProperty); }
            set { this.SetValue(ExclusionsProperty, value); }
        }

        public static ObservableProperty<string> ExclusionsProperty = new ObservableProperty<string, SourceControlViewModel>(o => o.Exclusions, Constants.DefaultSourceControlHistoryExclusions);

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

        #endregion

        #region Constructors

        [ImportingConstructor]
        public SourceControlViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Source Control", "Allows you to manage Source Control settings for Team Projects.")
        {
            this.GetLatestChangesetsCommand = new RelayCommand(GetLatestChangesets, CanGetLatestChangesets);
            this.ViewChangesetDetailsCommand = new RelayCommand(ViewChangesetDetails, CanViewChangesetDetails);
            this.GetSourceControlSettingsCommand = new RelayCommand(GetSourceControlSettings, CanGetSourceControlSettings);
            this.LoadSourceControlSettingsCommand = new RelayCommand(LoadSourceControlSettings, CanLoadSourceControlSettings);
            this.UpdateSourceControlSettingsCommand = new RelayCommand(UpdateSourceControlSettings, CanUpdateSourceControlSettings);
        }

        #endregion

        #region Commands

        private bool CanGetLatestChangesets(object argument)
        {
            return IsAnyTeamProjectSelected() && this.NumberOfChangesets > 0;
        }

        private void GetLatestChangesets(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var numberOfChangesetsForProject = this.NumberOfChangesets;
            var exclusions = (string.IsNullOrEmpty(this.Exclusions) ? new string[0] : this.Exclusions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            var task = new ApplicationTask("Retrieving latest changesets", teamProjectNames.Count, true);
            this.PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var changesets = new List<ChangesetInfo>();
                var step = 0;
                var tfs = GetSelectedTfsTeamProjectCollection();
                var vcs = tfs.GetService<VersionControlServer>();
                foreach (var teamProjectName in teamProjectNames)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                    try
                    {
                        var foundChangesetsForProject = 0;
                        VersionSpec versionTo = null;
                        while (foundChangesetsForProject < numberOfChangesetsForProject && !task.IsCanceled)
                        {
                            const int pageCount = 10;
                            var history = vcs.QueryHistory("$/" + teamProjectName, VersionSpec.Latest, 0, RecursionType.Full, null, null, versionTo, pageCount, false, false, false).Cast<Changeset>().ToList();
                            foreach (Changeset changeset in history)
                            {
                                if (string.IsNullOrEmpty(changeset.Comment) || !exclusions.Any(x => changeset.Comment.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    foundChangesetsForProject++;
                                    task.SetProgressForCurrentStep((double)foundChangesetsForProject / (double)numberOfChangesetsForProject);
                                    changesets.Add(new ChangesetInfo(teamProjectName, changeset.ChangesetId, changeset.Committer, changeset.CreationDate, changeset.Comment));
                                    if (foundChangesetsForProject == numberOfChangesetsForProject)
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
                e.Result = changesets;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving latest changesets", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.Changesets = (ICollection<ChangesetInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.Changesets.Count.ToCountString("changeset"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanViewChangesetDetails(object argument)
        {
            return this.SelectedChangesets != null && this.SelectedChangesets.Count == 1;
        }

        private void ViewChangesetDetails(object argument)
        {
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var vcs = tfs.GetService<VersionControlServer>();
                var changesetId = this.SelectedChangesets.First().Id;
                var assembly = Assembly.GetAssembly(typeof(WorkItemPolicy));
                var args = new object[] { vcs, vcs.GetChangeset(changesetId), false };
                using (var dialog = (System.Windows.Forms.Form)assembly.CreateInstance("Microsoft.TeamFoundation.VersionControl.Controls.DialogChangesetDetails", false, BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, args, CultureInfo.CurrentCulture, null))
                {
                    dialog.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                    dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                }
            }
            catch (Exception exc)
            {
                var message = "There was a problem showing the internal TFS changeset details dialog.";
                Logger.Log(message, exc, TraceEventType.Warning);
                MessageBox.Show(message + " See the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

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

        #endregion
    }
}