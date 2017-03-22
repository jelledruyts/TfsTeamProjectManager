using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.XamlBuild.BuildProcessTemplates
{
    [Export]
    public partial class BuildProcessTemplatesView : UserControl
    {
        public BuildProcessTemplatesView()
        {
            InitializeComponent();
        }

        [Import]
        public BuildProcessTemplatesViewModel ViewModel
        {
            get
            {
                return (BuildProcessTemplatesViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void buildProcessTemplatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedBuildProcessTemplates = this.buildProcessTemplatesDataGrid.SelectedItems.Cast<BuildProcessTemplateInfo>().ToList();
        }
    }
}