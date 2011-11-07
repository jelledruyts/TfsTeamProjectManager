using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
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

        #endregion

        #region Observable Properties

        public IEnumerable<RegisteredProjectCollection> TeamProjectCollections
        {
            get { return this.GetValue(TeamProjectCollectionsProperty); }
            set { this.SetValue(TeamProjectCollectionsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<RegisteredProjectCollection>> TeamProjectCollectionsProperty = new ObservableProperty<IEnumerable<RegisteredProjectCollection>, TeamProjectsViewModel>(o => o.TeamProjectCollections);

        [Export]
        public RegisteredProjectCollection SelectedTeamProjectCollection
        {
            get { return this.GetValue(SelectedTeamProjectCollectionProperty); }
            set { this.SetValue(SelectedTeamProjectCollectionProperty, value); }
        }

        public static ObservableProperty<RegisteredProjectCollection> SelectedTeamProjectCollectionProperty = new ObservableProperty<RegisteredProjectCollection, TeamProjectsViewModel>(o => o.SelectedTeamProjectCollection, null, OnSelectedTeamProjectCollectionChanged);

        [Export]
        public IEnumerable<ProjectInfo> TeamProjects
        {
            get { return this.GetValue(TeamProjectsProperty); }
            set { this.SetValue(TeamProjectsProperty, value); }
        }

        public static ObservableProperty<IEnumerable<ProjectInfo>> TeamProjectsProperty = new ObservableProperty<IEnumerable<ProjectInfo>, TeamProjectsViewModel>(o => o.TeamProjects, OnTeamProjectsChanged);

        public ICollection<ProjectInfo> SelectedTeamProjects
        {
            get { return this.GetValue(SelectedTeamProjectsProperty); }
            set { this.SetValue(SelectedTeamProjectsProperty, value); }
        }

        public static ObservableProperty<ICollection<ProjectInfo>> SelectedTeamProjectsProperty = new ObservableProperty<ICollection<ProjectInfo>, TeamProjectsViewModel>(o => o.SelectedTeamProjects, null, OnSelectedTeamProjectsChanged);

        public Visibility TeamProjectsVisibility
        {
            get { return this.GetValue(TeamProjectsVisibilityProperty); }
            set { this.SetValue(TeamProjectsVisibilityProperty, value); }
        }

        public static ObservableProperty<Visibility> TeamProjectsVisibilityProperty = new ObservableProperty<Visibility, TeamProjectsViewModel>(o => o.TeamProjectsVisibility);

        public Visibility InfoMessageVisibility
        {
            get { return this.GetValue(InfoMessageVisibilityProperty); }
            set { this.SetValue(InfoMessageVisibilityProperty, value); }
        }

        public static ObservableProperty<Visibility> InfoMessageVisibilityProperty = new ObservableProperty<Visibility, TeamProjectsViewModel>(o => o.InfoMessageVisibility);

        public string InfoMessage
        {
            get { return this.GetValue(InfoMessageProperty); }
            set { this.SetValue(InfoMessageProperty, value); }
        }

        public static ObservableProperty<string> InfoMessageProperty = new ObservableProperty<string, TeamProjectsViewModel>(o => o.InfoMessage);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public TeamProjectsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger)
        {
            this.AddTeamProjectCollectionCommand = new RelayCommand(AddTeamProjectCollection);
            RefreshTeamProjectCollections(null);
        }

        #endregion

        #region Property Change Handlers

        private static void OnTeamProjectsChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<IEnumerable<ProjectInfo>> e)
        {
            var viewModel = (TeamProjectsViewModel)sender;
            viewModel.SelectedTeamProjects = null;
        }

        private static void OnSelectedTeamProjectCollectionChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<RegisteredProjectCollection> e)
        {
            var viewModel = (TeamProjectsViewModel)sender;
            if (viewModel.SelectedTeamProjectCollection != null)
            {
                var task = new ApplicationTask(string.Format(CultureInfo.CurrentCulture, "Retrieving team projects for \"{0}\"", viewModel.SelectedTeamProjectCollection.Name));
                viewModel.SetInfoMessage("Loading...");
                viewModel.PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (bsender, be) =>
                {
                    using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(viewModel.SelectedTeamProjectCollection.Uri))
                    {
                        var store = tfs.GetService<ICommonStructureService>();
                        be.Result = store.ListAllProjects().OrderBy(p => p.Name);
                    }
                };
                worker.RunWorkerCompleted += (bsender, be) =>
                {
                    if (be.Error != null)
                    {
                        viewModel.Logger.Log("An unexpected exception occurred while retrieving team projects", be.Error);
                        task.SetError(be.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        viewModel.TeamProjects = (IEnumerable<ProjectInfo>)be.Result;
                        task.SetComplete("Retrieved " + viewModel.TeamProjects.Count().ToCountString("team project"));
                    }
                    viewModel.ClearInfoMessage();
                };
                worker.RunWorkerAsync();
            }
            else
            {
                viewModel.TeamProjects = null;
            }
        }

        private static void OnSelectedTeamProjectsChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<ICollection<ProjectInfo>> e)
        {
            var viewModel = (TeamProjectsViewModel)sender;
            viewModel.EventAggregator.GetEvent<TeamProjectSelectionChangedEvent>().Publish(new TeamProjectSelectionChangedEventArgs(viewModel.SelectedTeamProjectCollection, viewModel.SelectedTeamProjects));
        }

        #endregion

        #region Commands

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

        #endregion

        #region Helper Methods

        private void RefreshTeamProjectCollections(string selectedTeamProjectCollectionName)
        {
            try
            {
                SetInfoMessage("Loading...");
                this.TeamProjectCollections = RegisteredTfsConnections.GetProjectCollections();
                this.SelectedTeamProjectCollection = this.TeamProjectCollections.FirstOrDefault(t => t.Name == selectedTeamProjectCollectionName);
                SetInfoMessage("Please select a project collection");
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving the Team Project Collections", exc);
            }
        }

        private void SetInfoMessage(string infoMessage)
        {
            this.InfoMessage = infoMessage;
            this.InfoMessageVisibility = string.IsNullOrWhiteSpace(infoMessage) ? Visibility.Hidden : Visibility.Visible;
            this.TeamProjectsVisibility = string.IsNullOrWhiteSpace(infoMessage) ? Visibility.Visible : Visibility.Hidden;
        }

        private void ClearInfoMessage()
        {
            SetInfoMessage(null);
        }

        #endregion
    }
}