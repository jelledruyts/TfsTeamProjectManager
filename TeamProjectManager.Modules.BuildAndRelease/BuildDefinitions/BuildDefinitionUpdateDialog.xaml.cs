using System.Windows;

namespace TeamProjectManager.Modules.BuildAndRelease.BuildDefinitions
{
    public partial class BuildDefinitionUpdateDialog : Window
    {
        public BuildDefinitionUpdate BuildDefinitionUpdate { get; private set; }

        public BuildDefinitionUpdateDialog(string title)
        {
            InitializeComponent();
            this.Title = title;
            this.BuildDefinitionUpdate = new BuildDefinitionUpdate();
            this.DataContext = this.BuildDefinitionUpdate;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will update the selected build definitions. Are you sure you want to continue?", "Confirm Update", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}