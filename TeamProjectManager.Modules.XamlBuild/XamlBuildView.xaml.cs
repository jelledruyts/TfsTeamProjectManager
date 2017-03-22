using Microsoft.Practices.Prism.Regions;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.XamlBuild
{
    [Export]
    [ViewSortHint("900")]
    public partial class XamlBuildView : UserControl
    {
        public XamlBuildView()
        {
            InitializeComponent();
        }

        [Import]
        public XamlBuildViewModel ViewModel
        {
            get
            {
                return (XamlBuildViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }
    }
}