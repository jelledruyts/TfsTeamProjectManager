using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [Export]
    public partial class WorkItemTypesView : UserControl
    {
        public RelayCommand SelectAllCommand { get; private set; }
        public RelayCommand SelectNoneCommand { get; private set; }

        public WorkItemTypesView()
        {
            InitializeComponent();
            this.SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
            this.SelectNoneCommand = new RelayCommand(SelectNone, CanSelectNone);
        }

        [Import]
        public WorkItemTypesViewModel ViewModel
        {
            get
            {
                return (WorkItemTypesViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void workItemTypeFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedWorkItemTypeFiles = this.workItemTypeFilesListBox.SelectedItems.Cast<WorkItemTypeFile>().ToList();
        }

        private bool CanSelectAll(object argument)
        {
            return (this.workItemTypeFilesListBox.SelectedItems.Count != this.workItemTypeFilesListBox.Items.Count);
        }

        private void SelectAll(object argument)
        {
            this.workItemTypeFilesListBox.SelectAll();
        }

        private bool CanSelectNone(object argument)
        {
            return (this.workItemTypeFilesListBox.SelectedItems.Count > 0);
        }

        private void SelectNone(object argument)
        {
            this.workItemTypeFilesListBox.SelectedItem = null;
        }
    }
}