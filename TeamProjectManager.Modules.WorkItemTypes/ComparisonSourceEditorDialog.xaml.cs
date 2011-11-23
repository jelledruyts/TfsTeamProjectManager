using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
            this.ComparisonSource = new ComparisonSource(this.nameTextBox.Text, this.workItemTypesListBox.Items.Cast<string>().Select(w => new WorkItemTypeDefinition(w)).ToList());
            this.DialogResult = true;
            this.Close();
        }

        private void nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUI();
        }

        private void addWorkItemTypesHyperlink_Click(object sender, RoutedEventArgs e)
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
                    this.workItemTypesListBox.Items.Add(file);
                }
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            this.okButton.IsEnabled = this.nameTextBox.Text.Length > 0 && this.workItemTypesListBox.Items.Count > 0;
        }
    }
}