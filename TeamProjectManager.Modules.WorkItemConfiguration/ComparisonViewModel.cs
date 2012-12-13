using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public class ComparisonViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand AddComparisonSourceCommand { get; private set; }
        public RelayCommand EditSelectedComparisonSourceCommand { get; private set; }
        public RelayCommand RemoveSelectedComparisonSourceCommand { get; private set; }
        public RelayCommand MoveSelectedComparisonSourceUpCommand { get; private set; }
        public RelayCommand MoveSelectedComparisonSourceDownCommand { get; private set; }
        public RelayCommand LoadComparisonSourcesCommand { get; private set; }
        public RelayCommand SaveComparisonSourcesCommand { get; private set; }
        public RelayCommand CompareCommand { get; private set; }
        public RelayCommand ViewSelectedComparisonDetailsCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ObservableCollection<WorkItemConfiguration> ComparisonSources
        {
            get { return this.GetValue(ComparisonSourcesProperty); }
            set { this.SetValue(ComparisonSourcesProperty, value); }
        }

        public static ObservableProperty<ObservableCollection<WorkItemConfiguration>> ComparisonSourcesProperty = new ObservableProperty<ObservableCollection<WorkItemConfiguration>, ComparisonViewModel>(o => o.ComparisonSources, new ObservableCollection<WorkItemConfiguration>());

        public WorkItemConfiguration SelectedComparisonSource
        {
            get { return this.GetValue(SelectedComparisonSourceProperty); }
            set { this.SetValue(SelectedComparisonSourceProperty, value); }
        }

        public static ObservableProperty<WorkItemConfiguration> SelectedComparisonSourceProperty = new ObservableProperty<WorkItemConfiguration, ComparisonViewModel>(o => o.SelectedComparisonSource);

        public ICollection<TeamProjectComparisonResult> ComparisonResults
        {
            get { return this.GetValue(ComparisonResultsProperty); }
            set { this.SetValue(ComparisonResultsProperty, value); }
        }

        public static ObservableProperty<ICollection<TeamProjectComparisonResult>> ComparisonResultsProperty = new ObservableProperty<ICollection<TeamProjectComparisonResult>, ComparisonViewModel>(o => o.ComparisonResults);

        public TeamProjectComparisonResult SelectedComparisonResult
        {
            get { return this.GetValue(SelectedComparisonResultProperty); }
            set { this.SetValue(SelectedComparisonResultProperty, value); }
        }

        public static ObservableProperty<TeamProjectComparisonResult> SelectedComparisonResultProperty = new ObservableProperty<TeamProjectComparisonResult, ComparisonViewModel>(o => o.SelectedComparisonResult);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public ComparisonViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Configuration", "Allows you to compare work item type configurations.")
        {
            this.AddComparisonSourceCommand = new RelayCommand(AddComparisonSource, CanAddComparisonSource);
            this.EditSelectedComparisonSourceCommand = new RelayCommand(EditSelectedComparisonSource, CanEditSelectedComparisonSource);
            this.RemoveSelectedComparisonSourceCommand = new RelayCommand(RemoveSelectedComparisonSource, CanRemoveSelectedComparisonSource);
            this.MoveSelectedComparisonSourceUpCommand = new RelayCommand(MoveSelectedComparisonSourceUp, CanMoveSelectedComparisonSourceUp);
            this.MoveSelectedComparisonSourceDownCommand = new RelayCommand(MoveSelectedComparisonSourceDown, CanMoveSelectedComparisonSourceDown);
            this.LoadComparisonSourcesCommand = new RelayCommand(LoadComparisonSources, CanLoadComparisonSources);
            this.SaveComparisonSourcesCommand = new RelayCommand(SaveComparisonSources, CanSaveComparisonSources);
            this.CompareCommand = new RelayCommand(Compare, CanCompare);
            this.ViewSelectedComparisonDetailsCommand = new RelayCommand(ViewSelectedComparisonDetails, CanViewSelectedComparisonDetails);
        }

        #endregion

        #region Commands

        private bool CanAddComparisonSource(object argument)
        {
            return true;
        }

        private void AddComparisonSource(object argument)
        {
            var dialog = new WorkItemConfigurationEditorDialog();
            dialog.Owner = Application.Current.MainWindow;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                this.ComparisonSources.Add(dialog.Configuration);
            }
        }

        private bool CanEditSelectedComparisonSource(object argument)
        {
            return this.SelectedComparisonSource != null;
        }

        private void EditSelectedComparisonSource(object argument)
        {
            var dialog = new WorkItemConfigurationEditorDialog(this.SelectedComparisonSource);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        private bool CanRemoveSelectedComparisonSource(object argument)
        {
            return this.SelectedComparisonSource != null;
        }

        private void RemoveSelectedComparisonSource(object argument)
        {
            this.ComparisonSources.Remove(this.SelectedComparisonSource);
        }

        private bool CanMoveSelectedComparisonSourceUp(object argument)
        {
            return this.SelectedComparisonSource != null && this.ComparisonSources.IndexOf(this.SelectedComparisonSource) > 0;
        }

        private void MoveSelectedComparisonSourceUp(object argument)
        {
            var currentIndex = this.ComparisonSources.IndexOf(this.SelectedComparisonSource);
            this.ComparisonSources.Move(currentIndex, currentIndex - 1);
        }

        private bool CanMoveSelectedComparisonSourceDown(object argument)
        {
            return this.SelectedComparisonSource != null && this.ComparisonSources.IndexOf(this.SelectedComparisonSource) < this.ComparisonSources.Count - 1;
        }

        private void MoveSelectedComparisonSourceDown(object argument)
        {
            var currentIndex = this.ComparisonSources.IndexOf(this.SelectedComparisonSource);
            this.ComparisonSources.Move(currentIndex, currentIndex + 1);
        }

        private bool CanLoadComparisonSources(object argument)
        {
            return true;
        }

        private void LoadComparisonSources(object argument)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the comparison source list (*.xml) to load.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                try
                {
                    var persistedSources = SerializationProvider.Read<WorkItemConfigurationPersistenceData[]>(dialog.FileName);
                    this.ComparisonSources.Clear();
                    foreach (var persistedSource in persistedSources)
                    {
                        var items = new List<WorkItemConfigurationItem>();
                        foreach (var itemXml in persistedSource.WorkItemConfigurationItems)
                        {
                            try
                            {
                                items.Add(WorkItemConfigurationItem.FromXml(itemXml));
                            }
                            catch (ArgumentException)
                            {
                            }
                        }
                        this.ComparisonSources.Add(new WorkItemConfiguration(persistedSource.Name, items));
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.Log(string.Format(CultureInfo.CurrentCulture, "An error occurred while loading the work item configuration list from \"{0}\"", dialog.FileName), exc);
                    MessageBox.Show("An error occurred while loading the work item configuration list. See the log file for details", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool CanSaveComparisonSources(object argument)
        {
            return true;
        }

        private void SaveComparisonSources(object argument)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Please select the comparison source list (*.xml) to save.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                try
                {
                    SerializationProvider.Write<WorkItemConfigurationPersistenceData[]>(this.ComparisonSources.Select(c => new WorkItemConfigurationPersistenceData(c)).ToArray(), dialog.FileName);
                }
                catch (Exception exc)
                {
                    this.Logger.Log(string.Format(CultureInfo.CurrentCulture, "An error occurred while saving the work item configuration list to \"{0}\"", dialog.FileName), exc);
                    MessageBox.Show("An error occurred while saving the work item configuration list. See the log file for details", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool CanCompare(object argument)
        {
            return IsAnyTeamProjectSelected() && this.ComparisonSources.Count > 0;
        }

        private void Compare(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var sources = this.ComparisonSources.ToList();
            var task = new ApplicationTask("Comparing work item configurations", teamProjectNames.Count);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var results = new List<TeamProjectComparisonResult>();
                foreach (var teamProjectName in teamProjectNames)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                    try
                    {
                        var project = store.Projects[teamProjectName];
                        var shouldIncludeAgileAndCommonConfiguration = sources.Any(s => s.Items.Any(i => i.Type == WorkItemConfigurationItemType.AgileConfiguration || i.Type == WorkItemConfigurationItemType.CommonConfiguration));
                        var target = WorkItemConfiguration.FromTeamProject(tfs, project, shouldIncludeAgileAndCommonConfiguration);

                        var sourceComparisonResults = new List<WorkItemConfigurationComparisonResult>();
                        foreach (var source in sources)
                        {
                            sourceComparisonResults.Add(WorkItemConfigurationComparer.Compare(this.SelectedTeamProjectCollection.TeamFoundationServer.MajorVersion, source, target));
                        }
                        results.Add(new TeamProjectComparisonResult(teamProjectName, sourceComparisonResults));
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProjectName), exc);
                    }
                }
                e.Result = results;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while comparing work item configurations", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.ComparisonResults = (ICollection<TeamProjectComparisonResult>)e.Result;
                    task.SetComplete("Done");
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanViewSelectedComparisonDetails(object argument)
        {
            return this.SelectedComparisonResult != null;
        }

        private void ViewSelectedComparisonDetails(object argument)
        {
            var dialog = new ComparisonResultViewerDialog(this.SelectedComparisonResult);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        #endregion
    }
}