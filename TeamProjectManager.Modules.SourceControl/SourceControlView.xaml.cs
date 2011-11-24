using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeamProjectManager.Modules.SourceControl
{
    [Export]
    public partial class SourceControlView : UserControl
    {
        public SourceControlView()
        {
            InitializeComponent();
        }

        [Import]
        public SourceControlViewModel ViewModel
        {
            get
            {
                return (SourceControlViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void changesetsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedChangesets = this.changesetsDataGrid.SelectedItems.Cast<ChangesetInfo>().ToList();
        }

        private void changesetsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ViewModel.ViewChangesetDetailsCommand.CanExecute(null))
            {
                this.ViewModel.ViewChangesetDetailsCommand.Execute(null);
            }
        }
    }
}