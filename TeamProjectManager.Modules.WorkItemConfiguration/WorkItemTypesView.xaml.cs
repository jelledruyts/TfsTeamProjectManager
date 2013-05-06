using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public partial class WorkItemTypesView : UserControl
    {
        public WorkItemTypesView()
        {
            InitializeComponent();
        }

        [Import]
        public WorkItemTypesViewModel ViewModel
        {
            get
            {
                return (WorkItemTypesViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void workItemTypesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedWorkItemTypes = this.workItemTypesDataGrid.SelectedItems.Cast<WorkItemTypeInfo>().ToList();
        }
    }
}