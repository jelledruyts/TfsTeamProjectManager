using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common;
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

        public IEnumerable<BuildProcessHierarchyNode> BuildProcessHierarchy
        {
            get { return this.GetValue(BuildProcessHierarchyProperty); }
            set { this.SetValue(BuildProcessHierarchyProperty, value); }
        }

        public static readonly ObservableProperty<IEnumerable<BuildProcessHierarchyNode>> BuildProcessHierarchyProperty = new ObservableProperty<IEnumerable<BuildProcessHierarchyNode>, BuildProcessTemplatesViewModel>(o => o.BuildProcessHierarchy);

        public bool BuildProcessHierarchyHidesUnused
        {
            get { return this.GetValue(BuildProcessHierarchyHidesUnusedProperty); }
            set { this.SetValue(BuildProcessHierarchyHidesUnusedProperty, value); }
        }

        public static readonly ObservableProperty<bool> BuildProcessHierarchyHidesUnusedProperty = new ObservableProperty<bool, BuildProcessTemplatesViewModel>(o => o.BuildProcessHierarchyHidesUnused, OnBuildProcessHierarchyHidesUnused);

        private static void OnBuildProcessHierarchyHidesUnused(object sender, ObservablePropertyChangedEventArgs e)
        {
            ((BuildProcessTemplatesViewModel)sender).RefreshBuildProcessHierarchy();
        }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public BuildProcessTemplatesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Build Process Templates", "Allows you to manage the registered build process templates for Team Projects.")
        {
            this.RegisterBuildProcessTemplateCommand = new RelayCommand(RegisterBuildProcessTemplate, CanRegisterBuildProcessTemplate);
            this.GetBuildProcessTemplatesCommand = new RelayCommand(GetBuildProcessTemplates, CanGetBuildProcessTemplates);
            this.DeleteSelectedBuildProcessTemplatesCommand = new RelayCommand(DeleteSelectedBuildProcessTemplates, CanDeleteSelectedBuildProcessTemplates);
        }

        #endregion

        #region Commands

        private bool CanGetBuildProcessTemplates(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetBuildProcessTemplates(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving build process templates", teamProjectNames.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetService<IBuildServer>();
                e.Result = Tasks.GetBuildProcessTemplates(task, buildServer, teamProjectNames);
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
                    var buildProcessTemplates = (IList<BuildProcessTemplateInfo>)e.Result;
                    this.BuildProcessTemplates = buildProcessTemplates;
                    RefreshBuildProcessHierarchy();
                    task.SetComplete("Retrieved " + buildProcessTemplates.Count().ToCountString("build process template"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanRegisterBuildProcessTemplate(object argument)
        {
            return IsAnyTeamProjectSelected() && !string.IsNullOrEmpty(this.TemplateServerPath);
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
                var task = new ApplicationTask((this.Simulate ? "Simulating registering build process template" : "Registering build process template"), teamProjectNames.Count, true);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    var tfs = GetSelectedTfsTeamProjectCollection();
                    var buildServer = tfs.GetService<IBuildServer>();
                    Tasks.RegisterBuildProcessTemplate(task, buildServer, teamProjectNames, this.TemplateServerPath, this.TemplateType, true, this.UnregisterAllOtherTemplates, this.UnregisterAllOtherTemplatesIncludesUpgradeTemplate, this.Simulate);
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
            var result = MessageBox.Show("This will unregister the selected build process templates. Are you sure you want to continue?" + Environment.NewLine + Environment.NewLine + "Note that the XAML files will not be deleted from Source Control, they will only be unregistered as build process templates.", "Confirm Unregister", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var buildProcessTemplates = this.SelectedBuildProcessTemplates.Select(p => p.ProcessTemplate).ToList();
                var task = new ApplicationTask("Unregistering build process templates", buildProcessTemplates.Count, true);
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
                        Logger.Log("An unexpected exception occurred while unregistering build process template", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        task.SetComplete("Unregistered " + buildProcessTemplates.Count.ToCountString("build process template"));
                    }

                    // Refresh the list.
                    GetBuildProcessTemplates(null);
                };
                worker.RunWorkerAsync();
            }
        }

        #endregion

        #region Overrides

        protected override bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return server.MajorVersion >= TfsMajorVersion.V10;
        }

        #endregion

        #region Helper Methods

        private void RefreshBuildProcessHierarchy()
        {
            var serverPathNodes = new List<BuildProcessHierarchyNode>();
            foreach (var buildProcessTemplatesByServerPath in this.BuildProcessTemplates.GroupBy(b => b.ProcessTemplate.ServerPath).OrderBy(g => g.Key))
            {
                var buildProcessTemplatesForServerPath = buildProcessTemplatesByServerPath.ToList();
                var teamProjectNodes = new List<BuildProcessHierarchyNode>();
                foreach (var buildProcessTemplatesByTeamProject in buildProcessTemplatesForServerPath.GroupBy(b => b.ProcessTemplate.TeamProject).OrderBy(g => g.Key))
                {
                    var buildProcessTemplatesForTeamProject = buildProcessTemplatesByTeamProject.ToList();
                    var buildDefinitionNodes = buildProcessTemplatesForTeamProject.SelectMany(b => b.BuildDefinitions).OrderBy(d => d.Name).Select(d => new BuildProcessHierarchyNode(BuildProcessHierarchyNodeType.BuildDefinition, d.Name, null, null)).ToList();
                    buildDefinitionNodes = buildDefinitionNodes.Where(n => !(this.BuildProcessHierarchyHidesUnused && !n.HasBuildDefinitions)).ToList();
                    var teamProjectNode = new BuildProcessHierarchyNode(BuildProcessHierarchyNodeType.TeamProject, buildProcessTemplatesByTeamProject.Key, buildProcessTemplatesForTeamProject, buildDefinitionNodes);
                    if (!(this.BuildProcessHierarchyHidesUnused && !teamProjectNode.HasBuildDefinitions))
                    {
                        teamProjectNodes.Add(teamProjectNode);
                    }
                }
                var serverPathNode = new BuildProcessHierarchyNode(BuildProcessHierarchyNodeType.BuildProcessTemplateServerPath, buildProcessTemplatesByServerPath.Key, buildProcessTemplatesForServerPath, teamProjectNodes);
                if (!(this.BuildProcessHierarchyHidesUnused && !serverPathNode.HasBuildDefinitions))
                {
                    serverPathNodes.Add(serverPathNode);
                }
            }
            this.BuildProcessHierarchy = serverPathNodes;
        }

        #endregion
    }
}