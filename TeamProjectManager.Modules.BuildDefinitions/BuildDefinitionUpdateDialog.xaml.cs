using System.Collections.Generic;
using System.Windows;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.BuildDefinitions
{
    public partial class BuildDefinitionUpdateDialog : Window
    {
        private ICollection<BuildDefinitionInfo> buildDefinitionsToUpdate;
        public BuildDefinitionInfoUpdate BuildDefinitionInfoUpdate { get; private set; }

        public BuildDefinitionUpdateDialog(ICollection<BuildDefinitionInfo> buildDefinitionsToUpdate, ICollection<string> buildControllerNames)
        {
            InitializeComponent();
            this.buildDefinitionsToUpdate = buildDefinitionsToUpdate ?? new BuildDefinitionInfo[0];
            this.Title = "Updating " + this.buildDefinitionsToUpdate.Count.ToCountString("build definition");
            this.BuildDefinitionInfoUpdate = new BuildDefinitionInfoUpdate(buildDefinitionsToUpdate);
            this.DataContext = this.BuildDefinitionInfoUpdate;
            this.buildControllerNameComboBox.ItemsSource = buildControllerNames;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will udpate the selected build definitions. Are you sure you want to continue?", "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}