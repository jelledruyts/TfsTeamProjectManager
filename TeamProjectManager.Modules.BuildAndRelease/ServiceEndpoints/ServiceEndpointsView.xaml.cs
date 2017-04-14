using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.BuildAndRelease.ServiceEndpoints
{
    [Export]
    public partial class ServiceEndpointsView : UserControl
    {
        public ServiceEndpointsView()
        {
            InitializeComponent();
        }

        [Import]
        public ServiceEndpointsViewModel ViewModel
        {
            get
            {
                return (ServiceEndpointsViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void serviceEndpointsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedServiceEndpoints = this.serviceEndpointsDataGrid.SelectedItems.Cast<ServiceEndpointInfo>().ToList();
        }

        private void genericServiceEndpointPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ViewModel.GenericServiceEndpoint.Password = this.genericServiceEndpointPasswordBox.Password;
        }
    }
}