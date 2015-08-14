using System.Collections.Generic;
using System.Windows;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class WorkItemConfigurationItemEditorDialog : Window
    {
        public IDictionary<WorkItemConfigurationItemExport, WorkItemConfigurationItemExport> OriginalItemsWithClones { get; private set; }
        public IList<WorkItemConfigurationItemExport> Items { get; private set; }
        public ImportOptions Options { get; private set; }

        public WorkItemConfigurationItemEditorDialog(IList<WorkItemConfigurationItemExport> items, string itemType)
        {
            InitializeComponent();
            var originalItems = items ?? new WorkItemConfigurationItemExport[0];
            this.OriginalItemsWithClones = new Dictionary<WorkItemConfigurationItemExport, WorkItemConfigurationItemExport>();
            this.Items = new List<WorkItemConfigurationItemExport>();
            foreach (var originalItem in originalItems)
            {
                var clone = originalItem.Clone();
                this.Items.Add(clone);
                this.OriginalItemsWithClones.Add(originalItem, clone);
            }
            this.Title = "Editing " + this.Items.Count.ToCountString(itemType);
            this.DataContext = this.Items;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var originalWithClone in this.OriginalItemsWithClones)
            {
                var originalItem = originalWithClone.Key;
                var clone = originalWithClone.Value;
                originalItem.Item.XmlDefinition = clone.Item.XmlDefinition;
            }
            this.Options = ImportOptions.None;
            if (this.simulateCheckBox.IsChecked == true)
            {
                this.Options |= ImportOptions.Simulate;
            }
            if (this.saveCopyCheckBox.IsChecked == true)
            {
                this.Options |= ImportOptions.SaveCopy;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}