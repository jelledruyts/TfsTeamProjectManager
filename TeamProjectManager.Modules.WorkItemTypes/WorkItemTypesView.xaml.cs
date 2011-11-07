using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemTypes
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
    }
}
