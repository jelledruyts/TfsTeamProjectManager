using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class WorkItemConfigurationEditorDialog : Window
    {
        public WorkItemConfiguration Configuration
        {
            get
            {
                return (WorkItemConfiguration)this.DataContext;
            }
            private set
            {
                this.DataContext = value;
                UpdateUI();
            }
        }

        public WorkItemConfigurationEditorDialog()
            : this(new WorkItemConfiguration(), true)
        {
        }

        public WorkItemConfigurationEditorDialog(WorkItemConfiguration configuration)
            : this(configuration, false)
        {
        }

        private WorkItemConfigurationEditorDialog(WorkItemConfiguration configuration, bool canCancel)
        {
            InitializeComponent();
            this.Configuration = configuration;
            this.cancelButton.IsEnabled = canCancel;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.nameTextBox.Focus();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUI();
        }

        private void addItemsFromFilesHyperlink_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the work item configuration files (*.xml) to compare with.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            dialog.Multiselect = true;
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    try
                    {
                        this.Configuration.Items.Add(WorkItemConfigurationItem.FromFile(file));
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("An error occurred while loading the work item configuration file: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                UpdateUI();
            }
        }

        private void importFromTeamProjectButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                {
                    try
                    {
                        this.IsEnabled = false;

                        var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                        var teamProject = dialog.SelectedProjects.First();
                        var tfs = TfsTeamProjectCollectionCache.GetTfsTeamProjectCollection(teamProjectCollection.Uri);
                        var store = tfs.GetService<WorkItemStore>();
                        var project = store.Projects[teamProject.Name];

                        this.Configuration = WorkItemConfiguration.FromTeamProject(tfs, project, true);
                    }
                    finally
                    {
                        this.IsEnabled = true;
                    }
                }
            }
        }

        private void importFromProcessTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the Process Template file that defines the configuration files to compare with.";
            dialog.Filter = "Process Template XML Files (ProcessTemplate.xml)|ProcessTemplate.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                try
                {
                    this.Configuration = WorkItemConfiguration.FromProcessTemplate(dialog.FileName);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("An error occurred while loading the process template: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void removeHyperlink_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.itemsDataGrid.SelectedItems.Cast<WorkItemConfigurationItem>().ToList())
            {
                this.Configuration.Items.Remove(item);
            }
            UpdateUI();
        }

        private void itemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            this.removeHyperlink.IsEnabled = this.itemsDataGrid.SelectedItems.Count > 0;
        }
    }
}