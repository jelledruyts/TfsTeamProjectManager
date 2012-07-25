using System.Collections.Generic;
using System.Windows;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public partial class WorkItemConfigurationItemEditorDialog : Window
    {
        public IList<WorkItemConfigurationItemExport> Items { get; private set; }

        public WorkItemConfigurationItemEditorDialog(IList<WorkItemConfigurationItemExport> items, string itemType)
        {
            InitializeComponent();
            this.Items = items ?? new WorkItemConfigurationItemExport[0];
            this.Title = "Editing " + this.Items.Count.ToCountString(itemType);
            this.DataContext = this.Items;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}