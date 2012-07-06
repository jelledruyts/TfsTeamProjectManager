using System.Collections.Generic;
using System.Windows;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    public partial class WorkItemTypeEditorDialog : Window
    {
        public IList<WorkItemTypeExport> WorkItemTypesToExport { get; private set; }

        public WorkItemTypeEditorDialog(IList<WorkItemTypeExport> workItemTypesToExport)
        {
            InitializeComponent();
            this.WorkItemTypesToExport = workItemTypesToExport ?? new WorkItemTypeExport[0];
            this.Title = "Editing " + this.WorkItemTypesToExport.Count.ToCountString("Work Item Type");
            this.DataContext = this.WorkItemTypesToExport;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}