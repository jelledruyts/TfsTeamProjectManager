using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.Security
{
    [Export]
    public partial class SecurityView : UserControl
    {
        public SecurityView()
        {
            InitializeComponent();
        }

        [Import]
        public SecurityViewModel ViewModel
        {
            get
            {
                return (SecurityViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void securityGroupsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedSecurityGroups = this.securityGroupsDataGrid.SelectedItems.Cast<SecurityGroupInfo>().ToList();
        }
    }
}