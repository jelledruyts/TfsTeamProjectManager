using System.Windows;

namespace TeamProjectManager.Modules.XamlBuild.BuildDefinitions
{
    public partial class BuildDefinitionDeleteDialog : Window
    {
        public bool DeleteBuilds { get; private set; }

        public BuildDefinitionDeleteDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}