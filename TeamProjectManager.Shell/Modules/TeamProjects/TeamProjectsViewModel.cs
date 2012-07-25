using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Shell.Infrastructure;

namespace TeamProjectManager.Shell.Modules.TeamProjects
{
    [Export]
    public class TeamProjectsViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand AddTeamProjectCollectionCommand { get; private set; }
        public RelayCommand RefreshTeamProjectsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public IEnumerable<TeamProjectCollectionInfo> TeamProjectCollections
        {
            get { return this.GetValue(TeamProjectCollectionsProperty); }
            set { this.SetValue(TeamProjectCollectionsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<TeamProjectCollectionInfo>> TeamProjectCollectionsProperty = new ObservableProperty<IEnumerable<TeamProjectCollectionInfo>, TeamProjectsViewModel>(o => o.TeamProjectCollections);

        public Visibility TeamProjectsVisibility
        {
            get { return this.GetValue(TeamProjectsVisibilityProperty); }
            set { this.SetValue(TeamProjectsVisibilityProperty, value); }
        }

        public static ObservableProperty<Visibility> TeamProjectsVisibilityProperty = new ObservableProperty<Visibility, TeamProjectsViewModel>(o => o.TeamProjectsVisibility, Visibility.Collapsed);

        public string InfoMessage
        {
            get { return this.GetValue(InfoMessageProperty); }
            set { this.SetValue(InfoMessageProperty, value); }
        }

        public static ObservableProperty<string> InfoMessageProperty = new ObservableProperty<string, TeamProjectsViewModel>(o => o.InfoMessage, "Please select a project collection");

        public bool IsTeamProjectsLoadComplete
        {
            get { return this.GetValue(IsTeamProjectsLoadCompleteProperty); }
            set { this.SetValue(IsTeamProjectsLoadCompleteProperty, value); }
        }

        public static ObservableProperty<bool> IsTeamProjectsLoadCompleteProperty = new ObservableProperty<bool, TeamProjectsViewModel>(o => o.IsTeamProjectsLoadComplete, true);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public TeamProjectsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Team Projects")
        {
            this.AddTeamProjectCollectionCommand = new RelayCommand(AddTeamProjectCollection, CanAddTeamProjectCollection);
            this.RefreshTeamProjectsCommand = new RelayCommand(RefreshTeamProjects, CanRefreshTeamProjects);
            RefreshTeamProjectCollections(null);
        }

        #endregion

        #region Property Change Handlers

        protected override void OnSelectedTeamProjectCollectionChanged()
        {
            RefreshTeamProjects(false);
        }

        protected override void OnSelectedTeamProjectsChanged()
        {
            this.EventAggregator.GetEvent<TeamProjectSelectionChangedEvent>().Publish(new TeamProjectSelectionChangedEventArgs(this.SelectedTeamProjects));
        }

        #endregion

        #region Commands

        private bool CanAddTeamProjectCollection(object argument)
        {
            return Network.IsAvailable();
        }

        private void AddTeamProjectCollection(object argument)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.NoProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var projectCollection = dialog.SelectedTeamProjectCollection;
                    RegisteredTfsConnections.RegisterProjectCollection(projectCollection);
                    RefreshTeamProjectCollections(projectCollection.Name);
                }
            }
        }

        private bool CanRefreshTeamProjects(object argument)
        {
            return this.SelectedTeamProjectCollection != null;
        }

        private void RefreshTeamProjects(object argument)
        {
            RefreshTeamProjects(true);
        }

        #endregion

        #region Helper Methods

        private void RefreshTeamProjectCollections(string selectedTeamProjectCollectionName)
        {
            try
            {
                this.TeamProjectCollections = RegisteredTfsConnections.GetProjectCollections().Select(c => new TeamProjectCollectionInfo(c.Name, c.Uri));
                this.SelectedTeamProjectCollection = this.TeamProjectCollections.FirstOrDefault(t => t.Name == selectedTeamProjectCollectionName);
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving the Team Project Collections", exc);
            }
        }


        private void RefreshTeamProjects(bool forceRefresh)
        {
            if (forceRefresh || (this.SelectedTeamProjectCollection != null && this.SelectedTeamProjectCollection.TeamFoundationServer == null && this.SelectedTeamProjectCollection.TeamProjects == null))
            {
                var teamProjectCollectionToRefresh = this.SelectedTeamProjectCollection;
                this.SelectedTeamProjectCollection = null;
                var task = new ApplicationTask(string.Format(CultureInfo.CurrentCulture, "Retrieving team projects for \"{0}\"", teamProjectCollectionToRefresh.Name));
                this.InfoMessage = "Loading...";
                this.PublishStatus(new StatusEventArgs(task));
                this.IsTeamProjectsLoadComplete = false;
                this.TeamProjectsVisibility = Visibility.Collapsed;
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var tfs = GetTfsTeamProjectCollection(teamProjectCollectionToRefresh.Uri);
                    var tfsInfo = new TeamFoundationServerInfo(GetTfsMajorVersion(tfs, this.Logger));
                    var store = tfs.GetService<ICommonStructureService>();
                    var teamProjects = store.ListAllProjects().Where(p => p.Status == Microsoft.TeamFoundation.Common.ProjectState.WellFormed).Select(p => new TeamProjectInfo(teamProjectCollectionToRefresh, p.Name, new Uri(p.Uri))).OrderBy(p => p.Name).ToList();
                    e.Result = new Tuple<TeamFoundationServerInfo, ICollection<TeamProjectInfo>>(tfsInfo, teamProjects);
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        this.Logger.Log("An unexpected exception occurred while retrieving team projects", e.Error);
                        this.InfoMessage = "Could not connect";
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        var result = (Tuple<TeamFoundationServerInfo, ICollection<TeamProjectInfo>>)e.Result;
                        teamProjectCollectionToRefresh.TeamFoundationServer = result.Item1;
                        teamProjectCollectionToRefresh.TeamProjects = result.Item2;
                        this.SelectedTeamProjectCollection = teamProjectCollectionToRefresh;
                        task.SetComplete("Retrieved " + teamProjectCollectionToRefresh.TeamProjects.Count.ToCountString("team project"));
                    }
                    this.IsTeamProjectsLoadComplete = true;
                    CommandManager.InvalidateRequerySuggested();
                };
                worker.RunWorkerAsync();
            }
            else
            {
                if (this.SelectedTeamProjectCollection == null)
                {
                    this.InfoMessage = InfoMessageProperty.DefaultValue;
                    this.TeamProjectsVisibility = Visibility.Collapsed;
                }
                else
                {
                    this.InfoMessage = "Connected to " + this.SelectedTeamProjectCollection.TeamFoundationServer.ShortDisplayVersion;
                    this.TeamProjectsVisibility = Visibility.Visible;
                }
                this.EventAggregator.GetEvent<TeamProjectCollectionSelectionChangedEvent>().Publish(new TeamProjectCollectionSelectionChangedEventArgs(this.SelectedTeamProjectCollection));
            }
        }

        private static TfsMajorVersion GetTfsMajorVersion(TfsTeamProjectCollection tfs, ILogger logger)
        {
            try
            {
                var registrationService = (IRegistration)tfs.GetService(typeof(IRegistration));
                var frameworkEntries = registrationService.GetRegistrationEntries("Framework");
                if (frameworkEntries.Length > 0)
                {
                    // We are talking to at least TFS 2010.
                    if (frameworkEntries.Any(e => e.ServiceInterfaces != null && e.ServiceInterfaces.Any(i => i.Url.IndexOf("v4.0", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        return TfsMajorVersion.V11;
                    }
                    else
                    {
                        return TfsMajorVersion.V10;
                    }
                }
                else
                {
                    var vstfsEntries = registrationService.GetRegistrationEntries("vstfs");
                    if (vstfsEntries.Length != 1)
                    {
                        // We must be talking to an unknown version of TFS.
                        return TfsMajorVersion.Unknown;
                    }
                    else
                    {
                        var groupSecurity2Found = false;
                        foreach (ServiceInterface serviceInterface in vstfsEntries[0].ServiceInterfaces)
                        {
                            if (serviceInterface.Name.Equals("GroupSecurity2", StringComparison.OrdinalIgnoreCase))
                            {
                                groupSecurity2Found = true;
                            }
                        }

                        if (groupSecurity2Found)
                        {
                            // We are talking to TFS 2008, which is v9.
                            return TfsMajorVersion.V9;
                        }
                        else
                        {
                            // We are talking to TFS 2005, which is v8.
                            return TfsMajorVersion.V8;
                        }
                    }
                }
            }
            catch (TeamFoundationServerException exc)
            {
                logger.Log("An exception occurred while determining the TFS major version", exc);
                return TfsMajorVersion.Unknown;
            }
        }

        #endregion
    }
}