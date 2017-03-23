using System.Collections.Generic;
using System.Windows;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Queries
{
    public partial class WorkItemQueryEditorDialog : Window
    {
        public IDictionary<WorkItemQueryInfo, WorkItemQueryInfo> OriginalItemsWithClones { get; private set; }
        public IList<WorkItemQueryInfo> Items { get; private set; }

        public WorkItemQueryEditorDialog(IList<WorkItemQueryInfo> items)
        {
            InitializeComponent();
            var originalItems = items ?? new WorkItemQueryInfo[0];
            this.OriginalItemsWithClones = new Dictionary<WorkItemQueryInfo, WorkItemQueryInfo>();
            this.Items = new List<WorkItemQueryInfo>();
            foreach (var originalItem in originalItems)
            {
                var clone = originalItem.Clone();
                this.Items.Add(clone);
                this.OriginalItemsWithClones.Add(originalItem, clone);
            }
            this.Title = "Editing " + this.Items.Count.ToCountString("Work Item Query");
            this.DataContext = this.Items;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Apply();
            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Apply()
        {
            foreach (var originalWithClone in this.OriginalItemsWithClones)
            {
                var originalItem = originalWithClone.Key;
                var clone = originalWithClone.Value;
                originalItem.Text = clone.Text;
            }
        }
    }
}