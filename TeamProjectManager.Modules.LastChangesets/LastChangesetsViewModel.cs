using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.LastChangesets
{
    [Export]
    public class LastChangesetsViewModel : ViewModelBase
    {
        #region Constants

        private const int MaxHistoryCount = 100;

        #endregion

        #region Fields

        private RegisteredProjectCollection selectedTeamProjectCollection;
        private ICollection<ProjectInfo> selectedTeamProjects;

        #endregion

        #region Properties

        public string HeaderInfo { get { return "Last Changesets"; } }
        public RelayCommand GetLastChangesetsCommand { get; private set; }
        public RelayCommand ViewChangesetDetailsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<ChangesetInfo> Changesets
        {
            get { return this.GetValue(ChangesetsProperty); }
            set { this.SetValue(ChangesetsProperty, value); }
        }

        public static ObservableProperty<ICollection<ChangesetInfo>> ChangesetsProperty = new ObservableProperty<ICollection<ChangesetInfo>, LastChangesetsViewModel>(o => o.Changesets);

        public ICollection<ChangesetInfo> SelectedChangesets
        {
            get { return this.GetValue(SelectedChangesetsProperty); }
            set { this.SetValue(SelectedChangesetsProperty, value); }
        }

        public static ObservableProperty<ICollection<ChangesetInfo>> SelectedChangesetsProperty = new ObservableProperty<ICollection<ChangesetInfo>, LastChangesetsViewModel>(o => o.SelectedChangesets);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public LastChangesetsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger)
        {
            this.GetLastChangesetsCommand = new RelayCommand(GetLastChangesets, CanGetLastChangesets);
            this.ViewChangesetDetailsCommand = new RelayCommand(ViewChangesetDetails, CanViewChangesetDetails);
            this.EventAggregator.GetEvent<TeamProjectSelectionChangedEvent>().Subscribe(e => { this.selectedTeamProjectCollection = e.SelectedTeamProjectCollection; this.selectedTeamProjects = e.SelectedTeamProjects; });
        }

        #endregion

        #region Commands

        private bool CanGetLastChangesets(object argument)
        {
            return (this.selectedTeamProjectCollection != null && this.selectedTeamProjects != null && this.selectedTeamProjects.Count > 0);
        }

        private void GetLastChangesets(object argument)
        {
            var projects = this.selectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving last changesets", projects.Count);
            this.PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var changesets = new List<ChangesetInfo>();
                var step = 0;
                using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.selectedTeamProjectCollection.Uri))
                {
                    var vcs = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));
                    foreach (var project in projects)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", project));
                        var history = vcs.QueryHistory("$/" + project, VersionSpec.Latest, 0, RecursionType.Full, null, null, null, MaxHistoryCount, false, false);
                        Changeset lastChangeset = null;
                        var historyFound = false;
                        foreach (Changeset historyItem in history)
                        {
                            historyFound = true;

                            // TODO: Make exclusions configurable.
                            if (!string.Equals(historyItem.Comment, "Auto-Build: Version Update", StringComparison.OrdinalIgnoreCase))
                            {
                                lastChangeset = historyItem;
                                break;
                            }
                        }

                        if (lastChangeset == null)
                        {
                            var comment = (historyFound ? "The latest changeset from an actual user was not found in the last " + MaxHistoryCount + " changesets" : "There are no changesets in this Team Project");
                            changesets.Add(new ChangesetInfo(project, null, null, null, comment));
                        }
                        else
                        {
                            changesets.Add(new ChangesetInfo(project, lastChangeset));
                        }
                    }
                }
                e.Result = changesets;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving last changesets", e.Error);
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
            return this.SelectedChangesets != null && this.SelectedChangesets.Count == 1 && this.SelectedChangesets.First().Changeset != null;
        }

        private void ViewChangesetDetails(object argument)
        {
            var changeset = this.SelectedChangesets.First().Changeset;
            var assembly = Assembly.GetAssembly(typeof(WorkItemPolicy));
            var args = new object[] { changeset.VersionControlServer, changeset, false };
            using (var dialog = (System.Windows.Forms.Form)assembly.CreateInstance("Microsoft.TeamFoundation.VersionControl.Controls.DialogChangesetDetails", false, BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, args, CultureInfo.CurrentCulture, null))
            {
                dialog.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
            }
        }

        #endregion
    }
}