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
    }
}