using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public partial class ComparisonView : UserControl
    {
        public ComparisonView()
        {
            InitializeComponent();
        }

        [Import]
        public ComparisonViewModel ViewModel
        {
            get
            {
                return (ComparisonViewModel)this.DataContext;
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
