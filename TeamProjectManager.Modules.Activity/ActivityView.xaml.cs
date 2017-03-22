using Microsoft.Practices.Prism.Regions;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeamProjectManager.Modules.Activity
{
    [Export]
    [ViewSortHint("600")]
    public partial class ActivityView : UserControl
    {
        public ActivityView()
        {
            InitializeComponent();
        }

        [Import]
        public ActivityViewModel ViewModel
        {
            get
            {
                return (ActivityViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void activitiesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ViewModel.ViewActivityDetailsCommand.CanExecute(null))
            {
                this.ViewModel.ViewActivityDetailsCommand.Execute(null);
            }
        }
    }
}