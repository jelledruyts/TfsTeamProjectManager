using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
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

namespace TeamProjectManager.Modules.BuildAndRelease.ServiceEndpoints
{
    [Export]
    public class ServiceEndpointsViewModel : ViewModelBase
    {
        #region Properties

        public AsyncRelayCommand GetServiceEndpointsCommand { get; private set; }
        public AsyncRelayCommand DeleteSelectedServiceEndpointsCommand { get; private set; }
        public AsyncRelayCommand AddServiceEndpointCommand { get; private set; }
        public GenericServiceEndpoint GenericServiceEndpoint { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<ServiceEndpointInfo> ServiceEndpoints
        {
            get { return this.GetValue(ServiceEndpointsProperty); }
            set { this.SetValue(ServiceEndpointsProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<ServiceEndpointInfo>> ServiceEndpointsProperty = new ObservableProperty<ICollection<ServiceEndpointInfo>, ServiceEndpointsViewModel>(o => o.ServiceEndpoints);

        public ICollection<ServiceEndpointInfo> SelectedServiceEndpoints
        {
            get { return this.GetValue(SelectedServiceEndpointsProperty); }
            set { this.SetValue(SelectedServiceEndpointsProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<ServiceEndpointInfo>> SelectedServiceEndpointsProperty = new ObservableProperty<ICollection<ServiceEndpointInfo>, ServiceEndpointsViewModel>(o => o.SelectedServiceEndpoints);

        #endregion

        #region Constructors

        [ImportingConstructor]
        protected ServiceEndpointsViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Allows you to manage service endpoints for Team Projects.")
        {
            this.GetServiceEndpointsCommand = new AsyncRelayCommand(GetServiceEndpoints, CanGetServiceEndpoints);
            this.DeleteSelectedServiceEndpointsCommand = new AsyncRelayCommand(DeleteSelectedServiceEndpoints, CanDeleteSelectedServiceEndpoints);
            this.AddServiceEndpointCommand = new AsyncRelayCommand(AddServiceEndpoint, CanAddServiceEndpoint);
            this.GenericServiceEndpoint = new GenericServiceEndpoint();
        }

        #endregion

        #region GetServiceEndpoints Command

        private bool CanGetServiceEndpoints(object argument)
        {
            return this.IsAnyTeamProjectSelected();
        }

        private async Task GetServiceEndpoints(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving service endpoints", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var taskAgentClient = tfs.GetClient<TaskAgentHttpClient>();

                var step = 0;
                var serviceEndpoints = new List<ServiceEndpointInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var projectServiceEndpoints = await taskAgentClient.GetServiceEndpointsAsync(project: teamProject.Name);
                        serviceEndpoints.AddRange(projectServiceEndpoints.Select(p => new ServiceEndpointInfo(teamProject, p)));
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
                this.ServiceEndpoints = serviceEndpoints;
                task.SetComplete("Retrieved " + this.ServiceEndpoints.Count.ToCountString("service endpoint"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while retrieving service endpoints", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region DeleteSelectedServiceEndpoints Command

        private bool CanDeleteSelectedServiceEndpoints(object argument)
        {
            return this.SelectedServiceEndpoints != null && this.SelectedServiceEndpoints.Any();
        }

        private async Task DeleteSelectedServiceEndpoints(object argument)
        {
            var result = MessageBox.Show("This will delete the selected service endpoints. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var serviceEndpointsToDelete = this.SelectedServiceEndpoints;
            var task = new ApplicationTask("Deleting service endpoints", serviceEndpointsToDelete.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var taskAgentClient = tfs.GetClient<TaskAgentHttpClient>();

                var step = 0;
                var count = 0;
                foreach (var serviceEndpointToDelete in serviceEndpointsToDelete)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting service endpoint \"{0}\" in Team Project \"{1}\"", serviceEndpointToDelete.ServiceEndpoint.Name, serviceEndpointToDelete.TeamProject.Name));
                    try
                    {
                        // Delete the service endpoints one by one to avoid one failure preventing the other ones from being deleted.
                        await taskAgentClient.DeleteServiceEndpointAsync(serviceEndpointToDelete.TeamProject.Guid, serviceEndpointToDelete.ServiceEndpoint.Id);
                        count++;
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting service endpoints \"{0}\" in Team Project \"{1}\"", serviceEndpointToDelete.ServiceEndpoint.Name, serviceEndpointToDelete.TeamProject.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                task.SetComplete("Deleted " + count.ToCountString("service endpoint"));

                // Refresh the list.
                await GetServiceEndpoints(null);
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while deleting service endpoints", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion

        #region AddServiceEndpoint Command

        private bool CanAddServiceEndpoint(object argument)
        {
            return this.IsAnyTeamProjectSelected() && this.GenericServiceEndpoint.IsValid();
        }

        private async Task AddServiceEndpoint(object argument)
        {
            var result = MessageBox.Show("This will add the service endpoint to all selected Team Projects. Are you sure you want to continue?", "Confirm Service Endpoint", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Adding service endpoint", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            try
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var taskAgentClient = tfs.GetClient<TaskAgentHttpClient>();
                var serviceEndpoint = this.GenericServiceEndpoint.ToServiceEndpoint();

                var step = 0;
                var count = 0;
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        await taskAgentClient.CreateServiceEndpointAsync(teamProject.Guid, serviceEndpoint);
                        count++;
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
                task.SetComplete("Added service endpoint to " + count.ToCountString("Team Project"));
            }
            catch (Exception exc)
            {
                Logger.Log("An unexpected exception occurred while adding service endpoint", exc);
                task.SetError(exc);
                task.SetComplete("An unexpected exception occurred");
            }
        }

        #endregion
    }
}
