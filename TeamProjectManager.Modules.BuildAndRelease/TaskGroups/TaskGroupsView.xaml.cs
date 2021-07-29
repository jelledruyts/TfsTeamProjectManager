using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.BuildAndRelease.TaskGroups
{
    [Export]
    public partial class TaskGroupsView : UserControl
    {
        public TaskGroupsView()
        {
            InitializeComponent();
        }

        [Import]
        public TaskGroupsViewModel ViewModel
        {
            get
            {
                return (TaskGroupsViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void taskGroupsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedTaskGroups = this.taskGroupsDataGrid.SelectedItems.Cast<TaskGroupInfo>().ToList();
        }

        private void taskGroupsToImportListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedTaskGroupsToImport = this.taskGroupsToImportListBox.SelectedItems.Cast<TaskGroupCreateParameter>().ToList();
        }
    }
}