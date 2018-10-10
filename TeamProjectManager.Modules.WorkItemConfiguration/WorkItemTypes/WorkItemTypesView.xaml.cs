using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration.WorkItemTypes
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

        private void workItemTypesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.EditSelectedWorkItemTypesCommand.CanExecute(ViewModel.SelectedWorkItemTypes))
            {
                ViewModel.EditSelectedWorkItemTypesCommand.Execute(ViewModel.SelectedWorkItemTypes);
            }
        }
    }
}