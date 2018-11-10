using Prism.Regions;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.BuildAndRelease
{
    [Export]
    [ViewSortHint("200")]
    public partial class BuildAndReleaseView : UserControl
    {
        public BuildAndReleaseView()
        {
            InitializeComponent();
        }

        [Import]
        public BuildAndReleaseViewModel ViewModel
        {
            get
            {
                return (BuildAndReleaseViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }
    }
}