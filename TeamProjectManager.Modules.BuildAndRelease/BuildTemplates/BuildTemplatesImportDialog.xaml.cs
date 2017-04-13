using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.UI;

namespace TeamProjectManager.Modules.BuildAndRelease.BuildTemplates
{
    public partial class BuildTemplatesImportDialog : Window
    {
        public ObservableCollection<BuildDefinitionTemplate> BuildTemplates { get; private set; }

        public BuildTemplatesImportDialog()
        {
            InitializeComponent();
            this.BuildTemplates = new ObservableCollection<BuildDefinitionTemplate>();
            this.DataContext = this.BuildTemplates;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private async void importFromBuildTemplatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var buildTemplates = await GetBuildTemplatesAsync(false);
                foreach (var buildTemplate in buildTemplates)
                {
                    this.BuildTemplates.Add(buildTemplate);
                }
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private async void importFromBuildDefinitionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var buildTemplates = await GetBuildTemplatesAsync(true);
                foreach (var buildTemplate in buildTemplates)
                {
                    this.BuildTemplates.Add(buildTemplate);
                }
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        private void ShowException(Exception exc)
        {
            MessageBox.Show("An error occurred: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private async Task<ICollection<BuildDefinitionTemplate>> GetBuildTemplatesAsync(bool fromBuildDefinitions)
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
                        var buildDefinitions = await buildServer.GetFullDefinitionsAsync(project: teamProject.Name);
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
                picker.Owner = this;
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
    }
}