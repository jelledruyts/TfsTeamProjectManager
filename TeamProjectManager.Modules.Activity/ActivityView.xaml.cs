using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.Activity
{
    [Export]
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
    }
}
