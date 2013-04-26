using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public partial class WorkItemConfigurationTransformationView : UserControl
    {
        public WorkItemConfigurationTransformationView()
        {
            InitializeComponent();
        }

        [Import]
        public WorkItemConfigurationTransformationViewModel ViewModel
        {
            get
            {
                return (WorkItemConfigurationTransformationViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

    }
}