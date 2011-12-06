using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public partial class ComparisonSourceEditorDialog : Window
    {
        public ComparisonSourceEditorDialog()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.nameTextBox.Focus();
        }

        public ComparisonSource ComparisonSource { get; private set; }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.ComparisonSource = new ComparisonSource(this.nameTextBox.Text, this.workItemTypesListBox.Items.Cast<WorkItemTypeDefinition>().ToList());
            this.DialogResult = true;
            this.Close();
        }

        private void nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUI();
        }

        private void addWorkItemTypesFromFilesHyperlink_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the Work Item Type Definition files (*.xml) to compare with.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            dialog.Multiselect = true;
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    this.workItemTypesListBox.Items.Add(new WorkItemTypeDefinition(file));
                }
                UpdateUI();
            }
        }

        private void addWorkItemTypesFromTeamProjectHyperlink_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                {
                    var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                    var teamProject = dialog.SelectedProjects.First();
                    var tfs = TfsTeamProjectCollectionCache.GetTfsTeamProjectCollection(teamProjectCollection.Uri);
                    var store = tfs.GetService<WorkItemStore>();
                    var project = store.Projects[teamProject.Name];

                    foreach (var workItemType in project.WorkItemTypes.Cast<WorkItemType>())
                    {
                        this.workItemTypesListBox.Items.Add(new WorkItemTypeDefinition(workItemType.Export(false)));
                    }

                    if (string.IsNullOrEmpty(this.nameTextBox.Text))
                    {
                        this.nameTextBox.Text = teamProject.Name;
                    }

                    UpdateUI();
                }
            }
        }

        private void removeHyperlink_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.workItemTypesListBox.SelectedItems.Cast<WorkItemTypeDefinition>().ToList())
            {
                this.workItemTypesListBox.Items.Remove(item);
            }
            UpdateUI();
        }

        private void workItemTypesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            this.removeHyperlink.IsEnabled = this.workItemTypesListBox.SelectedItems.Count > 0;
            this.okButton.IsEnabled = this.nameTextBox.Text.Length > 0;
        }
    }
}