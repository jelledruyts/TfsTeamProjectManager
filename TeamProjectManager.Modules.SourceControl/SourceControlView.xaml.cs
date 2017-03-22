using Microsoft.Practices.Prism.Regions;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.SourceControl
{
    [Export]
    [ViewSortHint("950")]
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