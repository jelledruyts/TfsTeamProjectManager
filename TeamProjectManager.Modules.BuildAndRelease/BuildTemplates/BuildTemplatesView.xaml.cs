using Microsoft.TeamFoundation.Build.WebApi;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.BuildAndRelease.BuildTemplates
{
    [Export]
    public partial class BuildTemplatesView : UserControl
    {
        public BuildTemplatesView()
        {
            InitializeComponent();
        }

        [Import]
        public BuildTemplatesViewModel ViewModel
        {
            get
            {
                return (BuildTemplatesViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void buildTemplatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedBuildTemplates = this.buildTemplatesDataGrid.SelectedItems.Cast<BuildDefinitionTemplate>().ToList();
        }

        private void buildTemplatesToImportListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedBuildTemplatesToImport = this.buildTemplatesToImportListBox.SelectedItems.Cast<BuildDefinitionTemplate>().ToList();
        }
    }
}