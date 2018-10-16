using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Categories
{
    [Export]
    public partial class WorkItemCategoriesView : UserControl
    {
        public WorkItemCategoriesView()
        {
            InitializeComponent();
        }

        [Import]
        public WorkItemCategoriesViewModel ViewModel
        {
            get
            {
                return (WorkItemCategoriesViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void workItemCategoriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedWorkItemCategories = this.workItemCategoriesDataGrid.SelectedItems.Cast<WorkItemCategoryInfo>().ToList();
        }

        private void workItemCategoriesXmlDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.SelectedWorkItemCategoriesXml = this.workItemCategoriesXmlDataGrid.SelectedItems.Cast<WorkItemConfigurationItemExport>().ToList();
        }

        private void workItemCategoriesXmlDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.EditSelectedWorkItemCategoriesXmlCommand.CanExecute(ViewModel.SelectedWorkItemCategoriesXml))
            {
                ViewModel.EditSelectedWorkItemCategoriesXmlCommand.Execute(ViewModel.SelectedWorkItemCategoriesXml);
            }
        }

        private void workItemCategoriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var category = this.ViewModel.SelectedWorkItemCategory;
            try
            {
                this.workItemTypesListBox.SelectionChanged -= workItemTypesListBox_SelectionChanged;
                this.workItemTypesListBox.SelectedItems.Clear();
                if (category != null)
                {
                    foreach (WorkItemTypeReference availableWorkItemType in this.workItemTypesListBox.Items)
                    {
                        if (category.WorkItemTypes.Contains(availableWorkItemType))
                        {
                            this.workItemTypesListBox.SelectedItems.Add(availableWorkItemType);
                        }
                    }
                }
            }
            finally
            {
                this.workItemTypesListBox.SelectionChanged += workItemTypesListBox_SelectionChanged;
            }
        }

        private void workItemTypesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var category = this.ViewModel.SelectedWorkItemCategory;
            category.WorkItemTypes.Clear();
            foreach (WorkItemTypeReference selectedWorkItemType in this.workItemTypesListBox.SelectedItems)
            {
                category.WorkItemTypes.Add(selectedWorkItemType);
            }
        }
    }
}