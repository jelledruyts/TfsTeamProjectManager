using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.BuildAndRelease.BuildTemplates
{
    [Export]
    public class BuildTemplatesViewModel : ViewModelBase
    {
        #region Properties

        public AsyncRelayCommand GetBuildTemplatesCommand { get; private set; }
        public AsyncRelayCommand DeleteSelectedBuildTemplatesCommand { get; private set; }

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

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected BuildTemplatesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Allows you to manage build templates for Team Projects.")
        {
            this.GetBuildTemplatesCommand = new AsyncRelayCommand(GetBuildTemplates, CanGetBuildTemplates);
            this.DeleteSelectedBuildTemplatesCommand = new AsyncRelayCommand(DeleteSelectedBuildTemplates, CanDeleteSelectedBuildTemplates);
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
    }
}