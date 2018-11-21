﻿using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Shell.Modules.TeamProjects
{
    [Export]
    public class TeamProjectsViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand AddTeamProjectCollectionCommand { get; private set; }
        public RelayCommand AddExternalEditorCommand { get; private set; }
        public RelayCommand RefreshTeamProjectsCommand { get; private set; }
        public RelayCommand ShowAllTeamProjectsCommand { get; private set; }
        public RelayCommand ShowOnlySelectedTeamProjectsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public IEnumerable<TeamProjectCollectionInfo> TeamProjectCollections
        {
            get { return this.GetValue(TeamProjectCollectionsProperty); }
            set { this.SetValue(TeamProjectCollectionsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<TeamProjectCollectionInfo>> TeamProjectCollectionsProperty = new ObservableProperty<IEnumerable<TeamProjectCollectionInfo>, TeamProjectsViewModel>(o => o.TeamProjectCollections);

        public IEnumerable<string> ExternalEditors
        {
            get { return this.GetValue(ExternalEditorsProperty); }
            set { this.SetValue(ExternalEditorsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<string>> ExternalEditorsProperty = new ObservableProperty<IEnumerable<string>, TeamProjectsViewModel>(o => o.ExternalEditors);

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

        public static ObservableProperty<string> InfoMessageProperty = new ObservableProperty<string, TeamProjectsViewModel>(o => o.InfoMessage, "Please select a Team Project Collection");

        public string InfoMessageToolTip
        {
            get { return this.GetValue(InfoMessageToolTipProperty); }
            set { this.SetValue(InfoMessageToolTipProperty, value); }
        }

        public static readonly ObservableProperty<string> InfoMessageToolTipProperty = new ObservableProperty<string, TeamProjectsViewModel>(o => o.InfoMessageToolTip);

        public bool IsTeamProjectsLoadComplete
        {
            get { return this.GetValue(IsTeamProjectsLoadCompleteProperty); }
            set { this.SetValue(IsTeamProjectsLoadCompleteProperty, value); }
        }

        public static ObservableProperty<bool> IsTeamProjectsLoadCompleteProperty = new ObservableProperty<bool, TeamProjectsViewModel>(o => o.IsTeamProjectsLoadComplete, true);

        public ICollection<TeamProjectInfo> AvailableTeamProjects
        {
            get { return this.GetValue(AvailableTeamProjectsProperty); }
            set { this.SetValue(AvailableTeamProjectsProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<TeamProjectInfo>> AvailableTeamProjectsProperty = new ObservableProperty<ICollection<TeamProjectInfo>, TeamProjectsViewModel>(o => o.AvailableTeamProjects);

        public string TeamProjectsFilter
        {
            get { return this.GetValue(TeamProjectsFilterProperty); }
            set { this.SetValue(TeamProjectsFilterProperty, value); }
        }

        public static readonly ObservableProperty<string> TeamProjectsFilterProperty = new ObservableProperty<string, TeamProjectsViewModel>(o => o.TeamProjectsFilter, OnTeamProjectsFilterChanged);

        private static void OnTeamProjectsFilterChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<string> e)
        {
            ((TeamProjectsViewModel)sender).ApplyTeamProjectsFilter();
        }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public TeamProjectsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Team Projects")
        {
            this.AddTeamProjectCollectionCommand = new RelayCommand(AddTeamProjectCollection, CanAddTeamProjectCollection);
            this.AddExternalEditorCommand = new RelayCommand(AddExternalEditor);
            this.RefreshTeamProjectsCommand = new RelayCommand(RefreshTeamProjects, CanRefreshTeamProjects);
            this.ShowAllTeamProjectsCommand = new RelayCommand(ShowAllTeamProjects, CanShowAllTeamProjects);
            this.ShowOnlySelectedTeamProjectsCommand = new RelayCommand(ShowOnlySelectedTeamProjects, CanShowOnlySelectedTeamProjects);
            RefreshTeamProjectCollections(null);
            this.ExternalEditors = new List<string>();
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

        protected override void OnSelectedExternalEditorChanged()
        {
            this.EventAggregator.GetEvent<ExternalEditorSelectionChangedEvent>().Publish(new ExternalEditorSelectionChangedEventArgs(this.SelectedExternalEditor));
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

        private void AddExternalEditor(object argument)
        {
            var dialog = new OpenFileDialog {Multiselect = false};
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                var filename = dialog.FileName;
                RefreshExternalEditors(filename);
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

        private bool CanShowAllTeamProjects(object argument)
        {
            return this.SelectedTeamProjectCollection != null && this.SelectedTeamProjectCollection.TeamProjects != this.AvailableTeamProjects;
        }

        private void ShowAllTeamProjects(object argument)
        {
            this.AvailableTeamProjects = this.SelectedTeamProjectCollection == null ? null : this.SelectedTeamProjectCollection.TeamProjects;
        }

        private bool CanShowOnlySelectedTeamProjects(object argument)
        {
            return this.AvailableTeamProjects != this.SelectedTeamProjects;
        }

        private void ShowOnlySelectedTeamProjects(object argument)
        {
            this.AvailableTeamProjects = this.SelectedTeamProjects;
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
        private void RefreshExternalEditors(string selectedExternalEditor)
        {
            try
            {
                var list = this.ExternalEditors.ToList();
                if (!list.Contains(selectedExternalEditor))
                {
                    list.Add(selectedExternalEditor);
                }

                this.ExternalEditors = list;
                this.SelectedExternalEditor = this.ExternalEditors.FirstOrDefault(t => t == selectedExternalEditor);
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving the external editors", exc);
            }
        }

        private void RefreshTeamProjects(bool forceRefresh)
        {
            if (forceRefresh || (this.SelectedTeamProjectCollection != null && this.SelectedTeamProjectCollection.TeamFoundationServer == null && this.SelectedTeamProjectCollection.TeamProjects == null))
            {
                var teamProjectCollectionToRefresh = this.SelectedTeamProjectCollection;
                this.SelectedTeamProjectCollection = null;
                var task = new ApplicationTask(string.Format(CultureInfo.CurrentCulture, "Retrieving team projects for \"{0}\"", teamProjectCollectionToRefresh.Name));
                SetInfoMessage("Loading...", null);
                this.PublishStatus(new StatusEventArgs(task));
                this.IsTeamProjectsLoadComplete = false;
                this.TeamProjectsVisibility = Visibility.Collapsed;
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var tfs = GetTfsTeamProjectCollection(teamProjectCollectionToRefresh.Uri);
                    var tfsInfo = GetTeamFoundationServerInfo(tfs, this.Logger);
                    var store = tfs.GetService<ICommonStructureService>();
                    var teamProjects = store.ListAllProjects().Where(p => p.Status == Microsoft.TeamFoundation.Core.WebApi.ProjectState.WellFormed).Select(p => new TeamProjectInfo(teamProjectCollectionToRefresh, p.Name, new Uri(p.Uri), this.Logger)).OrderBy(p => p.Name).ToList();
                    e.Result = new Tuple<TeamFoundationServerInfo, ICollection<TeamProjectInfo>>(tfsInfo, teamProjects);
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        this.Logger.Log("An unexpected exception occurred while retrieving team projects", e.Error);
                        SetInfoMessage("Could not connect", e.Error.Message);
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
                    SetInfoMessage(InfoMessageProperty.DefaultValue, null);
                    this.TeamProjectsVisibility = Visibility.Collapsed;
                }
                else
                {
                    var infoMessage = "Connected to {0}".FormatCurrent(this.SelectedTeamProjectCollection.TeamFoundationServer.ShortDisplayVersion);
                    var toolTip = "Connected to {0}".FormatCurrent(this.SelectedTeamProjectCollection.TeamFoundationServer.DisplayVersion);
                    SetInfoMessage(infoMessage, toolTip);
                    this.TeamProjectsVisibility = Visibility.Visible;
                }
                this.EventAggregator.GetEvent<TeamProjectCollectionSelectionChangedEvent>().Publish(new TeamProjectCollectionSelectionChangedEventArgs(this.SelectedTeamProjectCollection));
            }
            ShowAllTeamProjects(null);
        }

        private static TeamFoundationServerInfo GetTeamFoundationServerInfo(TfsTeamProjectCollection tfs, ILogger logger)
        {
            if (tfs.IsHostedServer)
            {
                return new TeamFoundationServerInfo(tfs.DisplayName, tfs.ConfigurationServer.Uri, TfsMajorVersion.TeamServices, "Azure DevOps", "Azure DevOps");
            }
            try
            {
                // Determine the version of TFS based on the service interfaces that are available.
                var registrationService = (IRegistration)tfs.GetService(typeof(IRegistration));
                var serviceInterfaces = from e in registrationService.GetRegistrationEntries(string.Empty)
                                        from si in e.ServiceInterfaces
                                        select new { RegistrationEntryType = e.Type, Name = si.Name, Url = si.Url };

                if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "framework", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "wiki", StringComparison.OrdinalIgnoreCase)))
                {
                    // wiki functionality available (integrated) since TFS 2018
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V16, "Team Foundation Server 2018", "TFS 2018");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "vstfs", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "Authorization7", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V15, "Team Foundation Server 2017", "TFS 2017");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "WorkItemTracking", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "WorkitemService8", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V14, "Team Foundation Server 2015", "TFS 2015");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "WorkItemTracking", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "WorkitemService6", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V12, "Team Foundation Server 2013", "TFS 2013");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "TestManagement", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "TestManagementWebService3", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V11Update2, "Team Foundation Server 2012 Update 2 or higher", "TFS 2012.2+");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "TestManagement", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "TestManagementWebService2", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V11Update1, "Team Foundation Server 2012 Update 1", "TFS 2012.1");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "TestManagement", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "TestManagementWebService", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V11, "Team Foundation Server 2012", "TFS 2012.0");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "WorkItemTracking", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "WorkitemService4", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V10SP1, "Team Foundation Server 2010 Service Pack 1", "TFS 2010 SP1");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "Framework", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "LocationService", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V10, "Team Foundation Server 2010", "TFS 2010 RTM");
                }
                else if (serviceInterfaces.Any(e => string.Equals(e.RegistrationEntryType, "vstfs", StringComparison.OrdinalIgnoreCase) && string.Equals(e.Name, "GroupSecurity2", StringComparison.OrdinalIgnoreCase)))
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V9, "Team Foundation Server 2008", "TFS 2008");
                }
                else
                {
                    return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.V8, "Team Foundation Server 2005", "TFS 2005");
                }
            }
            catch (TeamFoundationServerException exc)
            {
                logger.Log("An exception occurred while determining the TFS major version", exc);
            }

            // We must be talking to an unknown version of TFS.
            return new TeamFoundationServerInfo(tfs.ConfigurationServer.Name, tfs.ConfigurationServer.Uri, TfsMajorVersion.Unknown, "Unknown version of Team Foundation Server", "Unknown TFS Version");
        }

        private void SetInfoMessage(string infoMessage, string toolTip)
        {
            this.InfoMessage = infoMessage;
            this.InfoMessageToolTip = toolTip;
        }

        private void ApplyTeamProjectsFilter()
        {
            if (string.IsNullOrEmpty(this.TeamProjectsFilter))
            {
                this.AvailableTeamProjects = this.SelectedTeamProjectCollection.TeamProjects;
            }
            else
            {
                this.AvailableTeamProjects = this.SelectedTeamProjectCollection.TeamProjects.Where(p => p.Name.IndexOf(this.TeamProjectsFilter, StringComparison.CurrentCultureIgnoreCase) >= 0).ToArray();
            }
        }

        #endregion
    }
}