using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeamProjectManager.Modules.LastChangesets
{
    [Export]
    public partial class LastChangesetsView : UserControl
    {
        public LastChangesetsView()
        {
            InitializeComponent();
        }

        [Import]
        public LastChangesetsViewModel ViewModel
        {
            get
            {
                return (LastChangesetsViewModel)this.DataContext;
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