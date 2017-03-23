using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Queries
{
    [Export]
    public partial class WorkItemQueriesView : UserControl
    {
        public WorkItemQueriesView()
        {
            InitializeComponent();
        }

        [Import]
        public WorkItemQueriesViewModel ViewModel
        {
            get
            {
                return (WorkItemQueriesViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void workItemQueriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedWorkItemQueries = this.workItemQueriesDataGrid.SelectedItems.Cast<WorkItemQueryInfo>().ToList();
        }
    }
}