using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;
using TeamProjectManager.Common.UI;

namespace TeamProjectManager.Modules.BuildAndRelease.BuildTemplates
{
    [Export]
    public class BuildTemplatesViewModel : ViewModelBase
    {
        #region Properties

        public AsyncRelayCommand GetBuildTemplatesCommand { get; private set; }
        public AsyncRelayCommand DeleteSelectedBuildTemplatesCommand { get; private set; }
        public AsyncRelayCommand AddBuildTemplateFromExistingBuildTemplatesCommand { get; private set; }
        public AsyncRelayCommand AddBuildTemplateFromExistingBuildDefinitionsCommand { get; private set; }
        public RelayCommand RemoveBuildTemplateToImportCommand { get; private set; }
        public RelayCommand RemoveAllBuildTemplatesToImportCommand { get; private set; }
        public AsyncRelayCommand ImportCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<BuildDefinitionTemplate> BuildTemplates
        {
            get { return this.GetValue(BuildTemplatesProperty); }
            set { this.SetValue(BuildTemplatesProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<BuildDefinitionTemplate>> BuildTemplatesProperty = new ObservableProperty<ICollection<BuildDefinitionTemplate>, BuildTemplatesViewModel>(o => o.BuildTemplates);

        public ICollection<BuildDefinitionTemplate> SelectedBuildTemplates
        {
            get { return this.GetValue(SelectedBuildTemplatesProperty); }
            set { this.SetValue(SelectedBuildTemplatesProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<BuildDefinitionTemplate>> SelectedBuildTemplatesProperty = new ObservableProperty<ICollection<BuildDefinitionTemplate>, BuildTemplatesViewModel>(o => o.SelectedBuildTemplates);

        public ObservableCollection<BuildDefinitionTemplate> BuildTemplatesToImport
        {
            get { return this.GetValue(BuildTemplatesToImportProperty); }
            set { this.SetValue(BuildTemplatesToImportProperty, value); }
        }

        public static readonly ObservableProperty<ObservableCollection<BuildDefinitionTemplate>> BuildTemplatesToImportProperty = new ObservableProperty<ObservableCollection<BuildDefinitionTemplate>, BuildTemplatesViewModel>(o => o.BuildTemplatesToImport);

        public ICollection<BuildDefinitionTemplate> SelectedBuildTemplatesToImport
        {
            get { return this.GetValue(SelectedBuildTemplatesToImportProperty); }
            set { this.SetValue(SelectedBuildTemplatesToImportProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<BuildDefinitionTemplate>> SelectedBuildTemplatesToImportProperty = new ObservableProperty<ICollection<BuildDefinitionTemplate>, BuildTemplatesViewModel>(o => o.SelectedBuildTemplatesToImport);

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected BuildTemplatesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Allows you to manage build templates for Team Projects.")
        {
            this.BuildTemplatesToImport = new ObservableCollection<BuildDefinitionTemplate>();
            this.GetBuildTemplatesCommand = new AsyncRelayCommand(GetBuildTemplates, CanGetBuildTemplates);
            this.DeleteSelectedBuildTemplatesCommand = new AsyncRelayCommand(DeleteSelectedBuildTemplates, CanDeleteSelectedBuildTemplates);
            this.AddBuildTemplateFromExistingBuildTemplatesCommand = new AsyncRelayCommand(AddBuildTemplateFromExistingBuildTemplates, CanAddBuildTemplateFromExistingBuildTemplates);
            this.AddBuildTemplateFromExistingBuildDefinitionsCommand = new AsyncRelayCommand(AddBuildTemplateFromExistingBuildDefinitions, CanAddBuildTemplateFromExistingBuildDefinitions);
            this.RemoveBuildTemplateToImportCommand = new RelayCommand(RemoveBuildTemplateToImport, CanRemoveBuildTemplateToImport);
            this.RemoveAllBuildTemplatesToImportCommand = new RelayCommand(RemoveAllBuildTemplatesToImport, CanRemoveAllBuildTemplatesToImport);
            this.ImportCommand = new AsyncRelayCommand(Import, CanImport);
        }

        #endregion

        #region GetBuildTemplates Command

        private bool CanGetBuildTemplates(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private async Task GetBuildTemplates(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving build templates", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetClient<BuildHttpClient>();

                var step = 0;
                var buildTemplates = new List<BuildDefinitionTemplate>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var projectBuildTemplates = await buildServer.GetTemplatesAsync(project: teamProject.Name);
                        buildTemplates.AddRange(projectBuildTemplates.Where(t => t.Template != null && t.Template.Project != null));
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProject.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                this.BuildTemplates = buildTemplates.OrderBy(t => t.Template?.Project?.Name).ThenBy(t => t.Category).ThenBy(t => t.Id).ToList();
                task.SetComplete("Retrieved " + this.BuildTemplates.Count.ToCountString("build template"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving build templates", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region DeleteSelectedBuildTemplates Command

        private bool CanDeleteSelectedBuildTemplates(object argument)
        {
            return this.SelectedBuildTemplates != null && this.SelectedBuildTemplates.All(t => t.CanDelete);
        }

        private async Task DeleteSelectedBuildTemplates(object argument)
        {
            var result = MessageBox.Show("This will delete the selected build templates. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var buildTemplatesToDelete = this.SelectedBuildTemplates;
            var task = new ApplicationTask("Deleting build templates", buildTemplatesToDelete.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetClient<BuildHttpClient>();

                var step = 0;
                var count = 0;
                foreach (var buildTemplateToDelete in buildTemplatesToDelete)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting build template \"{0}\" in Team Project \"{1}\"", buildTemplateToDelete.Name, buildTemplateToDelete.Template.Project.Name));
                    try
                    {
                        // Delete the build templates one by one to avoid one failure preventing the other build templates from being deleted.
                        await buildServer.DeleteTemplateAsync(buildTemplateToDelete.Template.Project.Id, buildTemplateToDelete.Id);
                        count++;
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting build template \"{0}\" in Team Project \"{1}\"", buildTemplateToDelete.Name, buildTemplateToDelete.Template.Project.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                task.SetComplete("Deleted " + count.ToCountString("build template"));

                // Refresh the list.
                await GetBuildTemplates(null);
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while deleting build templates", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region AddBuildTemplateFromExistingBuildTemplates Command

        private bool CanAddBuildTemplateFromExistingBuildTemplates(object argument)
        {
            return true;
        }

        private async Task AddBuildTemplateFromExistingBuildTemplates(object argument)
        {
            var buildTemplates = await GetBuildTemplatesAsync(this.SelectedTeamProjectCollection.TeamFoundationServer.MajorVersion, false);
            foreach (var buildTemplate in buildTemplates)
            {
                this.BuildTemplatesToImport.Add(buildTemplate);
            }
        }

        #endregion

        #region AddBuildTemplateFromExistingBuildDefinitions Command

        private bool CanAddBuildTemplateFromExistingBuildDefinitions(object argument)
        {
            return true;
        }

        private async Task AddBuildTemplateFromExistingBuildDefinitions(object argument)
        {
            var buildTemplates = await GetBuildTemplatesAsync(this.SelectedTeamProjectCollection.TeamFoundationServer.MajorVersion, true);
            foreach (var buildTemplate in buildTemplates)
            {
                this.BuildTemplatesToImport.Add(buildTemplate);
            }
        }

        #endregion

        #region RemoveBuildTemplateToImport Command

        private bool CanRemoveBuildTemplateToImport(object argument)
        {
            return this.SelectedBuildTemplatesToImport != null && this.SelectedBuildTemplatesToImport.Any();
        }

        private void RemoveBuildTemplateToImport(object argument)
        {
            foreach (var selectedBuildTemplateToImport in this.SelectedBuildTemplatesToImport)
            {
                this.BuildTemplatesToImport.Remove(selectedBuildTemplateToImport);
            }
        }

        #endregion

        #region RemoveAllBuildTemplatesToImport Command

        private bool CanRemoveAllBuildTemplatesToImport(object argument)
        {
            return this.BuildTemplatesToImport != null && this.BuildTemplatesToImport.Any();
        }

        private void RemoveAllBuildTemplatesToImport(object argument)
        {
            this.BuildTemplatesToImport.Clear();
        }

        #endregion

        #region Import Command

        private bool CanImport(object argument)
        {
            return this.IsAnyTeamProjectSelected() && this.BuildTemplatesToImport != null && this.BuildTemplatesToImport.Any();
        }

        private async Task Import(object argument)
        {
            var result = MessageBox.Show("This will import the specified build templates. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            var teamProjects = this.SelectedTeamProjects.ToList();
            var buildTemplates = this.BuildTemplatesToImport.ToArray();
            var task = new ApplicationTask("Importing build templates", teamProjects.Count * buildTemplates.Length, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var buildServer = tfs.GetClient<BuildHttpClient>();

                var step = 0;
                foreach (var teamProject in teamProjects)
                {
                    task.Status = "Processing Team Project \"{0}\"".FormatCurrent(teamProject.Name);
                    foreach (var buildTemplate in buildTemplates)
                    {
                        try
                        {
                            task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Importing build template \"{0}\" into Team Project \"{1}\"", buildTemplate.Name, teamProject.Name));
                            buildTemplate.Template.Project = new TeamProjectReference { Name = teamProject.Name, Id = teamProject.Guid };
                            await buildServer.SaveTemplateAsync(buildTemplate, teamProject.Name, buildTemplate.Id);
                        }
                        catch (Exception exc)
                        {
                            task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while importing build template \"{0}\" into Team Project \"{1}\"", buildTemplate.Name, teamProject.Name), exc);
                        }
                        if (task.IsCanceled)
                        {
                            break;
                        }
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                task.SetComplete("Imported " + buildTemplates.Length.ToCountString("build template"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while importing build templates", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region Helper Methods

        private async Task<ICollection<BuildDefinitionTemplate>> GetBuildTemplatesAsync(TfsMajorVersion tfsVersion, bool fromBuildDefinitions)
        {
            var buildTemplates = new List<BuildDefinitionTemplate>();
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                {
                    var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                    var teamProject = dialog.SelectedProjects.First();
                    var buildServer = teamProjectCollection.GetClient<BuildHttpClient>();

                    if (fromBuildDefinitions)
                    {
                        var buildDefinitions = new List<BuildDefinition>();
                        if (tfsVersion == TfsMajorVersion.TeamServices)
                        {
                            // Use a single API call if available.
                            var projectBuildDefinitions = await buildServer.GetFullDefinitionsAsync(project: teamProject.Name);
                            buildDefinitions.AddRange(projectBuildDefinitions);
                        }
                        else
                        {
                            // Otherwise get the base info first and then individual details.
                            var baseBuildDefinitions = await buildServer.GetDefinitionsAsync(project: teamProject.Name);
                            foreach (var baseBuildDefinition in baseBuildDefinitions.Where(b => b.Type == DefinitionType.Build))
                            {
                                var buildDefinition = await buildServer.GetDefinitionAsync(project: teamProject.Name, definitionId: baseBuildDefinition.Id);
                                buildDefinitions.Add(buildDefinition);
                            }
                        }
                        buildDefinitions = buildDefinitions.Where(b => b.Type == DefinitionType.Build).OrderBy(b => b.Name).ToList();

                        if (!buildDefinitions.Any())
                        {
                            MessageBox.Show("The selected Team Project does not contain any build definitions.", "No Build Definitions", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            foreach (var buildDefinition in buildDefinitions)
                            {
                                var template = new BuildDefinitionTemplate
                                {
                                    Id = buildDefinition.Name.Replace(" ", ""),
                                    Name = buildDefinition.Name,
                                    Description = "Build template based on build definition \"{0}\" in Team Project \"{1}\"".FormatCurrent(buildDefinition.Name, teamProject.Name),
                                    Template = buildDefinition
                                };
                                buildTemplates.Add(template);
                            }
                        }
                    }
                    else
                    {
                        var buildTemplatesFromProject = await buildServer.GetTemplatesAsync(project: teamProject.Name);
                        buildTemplatesFromProject = buildTemplatesFromProject.Where(t => t.Template != null && t.Template.Project != null).OrderBy(t => t.Name).ToList();

                        if (!buildTemplatesFromProject.Any())
                        {
                            MessageBox.Show("The selected Team Project does not contain any build templates.", "No Build Templates", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            foreach (var buildTemplate in buildTemplatesFromProject)
                            {
                                buildTemplates.Add(buildTemplate);
                            }
                        }
                    }
                }
            }

            if (buildTemplates.Any())
            {
                var picker = new ItemsPickerDialog();
                picker.ItemDisplayMemberPath = nameof(BuildDefinitionTemplate.Name);
                picker.Owner = Application.Current.MainWindow;
                picker.Title = fromBuildDefinitions ? "Select the build definitions to import" : "Select the build templates to import";
                picker.SelectionMode = SelectionMode.Multiple;
                picker.AvailableItems = buildTemplates;
                if (picker.ShowDialog() == true)
                {
                    return picker.SelectedItems.Cast<BuildDefinitionTemplate>().ToArray();
                }
            }

            return new BuildDefinitionTemplate[0];
        }

        #endregion
    }
}