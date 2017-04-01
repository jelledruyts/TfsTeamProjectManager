using Microsoft.TeamFoundation.Build.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    }
}
