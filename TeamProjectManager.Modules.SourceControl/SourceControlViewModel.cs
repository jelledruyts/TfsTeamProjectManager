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
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls;
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

        public static ObservableProperty<string> ExclusionsProperty = new ObservableProperty<string, SourceControlViewModel>(o => o.Exclusions, "***NO_CI***;Auto-Build: Version Update");

        #endregion

        #region Constructors

        [ImportingConstructor]
        public SourceControlViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Source Control", "Allows you to manage Source Control settings for Team Projects.")
        {
            this.GetLatestChangesetsCommand = new RelayCommand(GetLatestChangesets, CanGetLatestChangesets);
            this.ViewChangesetDetailsCommand = new RelayCommand(ViewChangesetDetails, CanViewChangesetDetails);
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
            var task = new ApplicationTask("Retrieving latest changesets", teamProjectNames.Count);
            this.PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var changesets = new List<ChangesetInfo>();
                var step = 0;
                using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
                {
                    var vcs = tfs.GetService<VersionControlServer>();
                    foreach (var teamProjectName in teamProjectNames)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                        var foundChangesetsForProject = 0;
                        VersionSpec versionTo = null;
                        while (foundChangesetsForProject < numberOfChangesetsForProject)
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
                using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
                {
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
            }
            catch (Exception exc)
            {
                var message = "There was a problem showing the internal TFS changeset details dialog.";
                Logger.Log(message, exc, TraceEventType.Warning);
                MessageBox.Show(message + " See the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}