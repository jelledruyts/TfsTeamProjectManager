using Prism.Regions;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    [ViewSortHint("100")]
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
    }
}