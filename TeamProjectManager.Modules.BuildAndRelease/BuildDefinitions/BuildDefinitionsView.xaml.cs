using Microsoft.TeamFoundation.Build.WebApi;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.BuildAndRelease.BuildDefinitions
{
    [Export]
    public partial class BuildDefinitionsView : UserControl
    {
        public BuildDefinitionsView()
        {
            InitializeComponent();
        }

        [Import]
        public BuildDefinitionsViewModel ViewModel
        {
            get
            {
                return (BuildDefinitionsViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void buildDefinitionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedBuildDefinitions = this.buildDefinitionsDataGrid.SelectedItems.Cast<BuildDefinition>().ToList();
        }
    }
}