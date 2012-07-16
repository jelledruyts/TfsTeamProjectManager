using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [Export]
    public partial class WorkItemConfigurationView : UserControl
    {
        public WorkItemConfigurationView()
        {
            InitializeComponent();
        }

        [Import]
        public WorkItemConfigurationViewModel ViewModel
        {
            get
            {
                return (WorkItemConfigurationViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void comparisonResultsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ViewModel.ViewSelectedComparisonDetailsCommand.CanExecute(null))
            {
                this.ViewModel.ViewSelectedComparisonDetailsCommand.Execute(null);
            }
        }
    }
}
