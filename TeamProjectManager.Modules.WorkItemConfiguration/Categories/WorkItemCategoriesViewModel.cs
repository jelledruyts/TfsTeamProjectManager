using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Categories
{
    [Export]
    public class WorkItemCategoriesViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetWorkItemCategoriesCommand { get; private set; }
        public RelayCommand DeleteSelectedWorkItemCategoriesCommand { get; private set; }
        public RelayCommand GetWorkItemCategoriesXmlCommand { get; private set; }
        public RelayCommand ExportSelectedWorkItemCategoriesXmlCommand { get; private set; }
        public RelayCommand EditSelectedWorkItemCategoriesXmlCommand { get; private set; }
        public RelayCommand TransformSelectedWorkItemCategoriesXmlCommand { get; private set; }
        public RelayCommand LoadWorkItemCategoryListCommand { get; private set; }
        public RelayCommand AddWorkItemCategoryCommand { get; private set; }
        public RelayCommand RemoveSelectedWorkItemCategoryCommand { get; private set; }
        public RelayCommand ImportWorkItemCategoryListCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<WorkItemCategoryInfo> WorkItemCategories
        {
            get { return this.GetValue(WorkItemCategoriesProperty); }
            set { this.SetValue(WorkItemCategoriesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemCategoryInfo>> WorkItemCategoriesProperty = new ObservableProperty<ICollection<WorkItemCategoryInfo>, WorkItemCategoriesViewModel>(o => o.WorkItemCategories);

        public ICollection<WorkItemCategoryInfo> SelectedWorkItemCategories
        {
            get { return this.GetValue(SelectedWorkItemCategoriesProperty); }
            set { this.SetValue(SelectedWorkItemCategoriesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemCategoryInfo>> SelectedWorkItemCategoriesProperty = new ObservableProperty<ICollection<WorkItemCategoryInfo>, WorkItemCategoriesViewModel>(o => o.SelectedWorkItemCategories);

        public ICollection<WorkItemConfigurationItemExport> WorkItemCategoriesXml
        {
            get { return this.GetValue(WorkItemCategoriesXmlProperty); }
            set { this.SetValue(WorkItemCategoriesXmlProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<WorkItemConfigurationItemExport>> WorkItemCategoriesXmlProperty = new ObservableProperty<ICollection<WorkItemConfigurationItemExport>, WorkItemCategoriesViewModel>(o => o.WorkItemCategoriesXml);

        public ICollection<WorkItemConfigurationItemExport> SelectedWorkItemCategoriesXml
        {
            get { return this.GetValue(SelectedWorkItemCategoriesXmlProperty); }
            set { this.SetValue(SelectedWorkItemCategoriesXmlProperty, value); }
        }

        public static readonly ObservableProperty<ICollection<WorkItemConfigurationItemExport>> SelectedWorkItemCategoriesXmlProperty = new ObservableProperty<ICollection<WorkItemConfigurationItemExport>, WorkItemCategoriesViewModel>(o => o.SelectedWorkItemCategoriesXml);

        public WorkItemCategoryList SelectedWorkItemCategoryList
        {
            get { return this.GetValue(SelectedWorkItemCategoryListProperty); }
            set { this.SetValue(SelectedWorkItemCategoryListProperty, value); }
        }

        public static ObservableProperty<WorkItemCategoryList> SelectedWorkItemCategoryListProperty = new ObservableProperty<WorkItemCategoryList, WorkItemCategoriesViewModel>(o => o.SelectedWorkItemCategoryList);

        public WorkItemCategory SelectedWorkItemCategory
        {
            get { return this.GetValue(SelectedWorkItemCategoryProperty); }
            set { this.SetValue(SelectedWorkItemCategoryProperty, value); }
        }

        public static ObservableProperty<WorkItemCategory> SelectedWorkItemCategoryProperty = new ObservableProperty<WorkItemCategory, WorkItemCategoriesViewModel>(o => o.SelectedWorkItemCategory);

        public ICollection<WorkItemTypeReference> AvailableWorkItemTypeReferences
        {
            get { return this.GetValue(AvailableWorkItemTypeReferencesProperty); }
            set { this.SetValue(AvailableWorkItemTypeReferencesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeReference>> AvailableWorkItemTypeReferencesProperty = new ObservableProperty<ICollection<WorkItemTypeReference>, WorkItemCategoriesViewModel>(o => o.AvailableWorkItemTypeReferences);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemCategoriesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Categories", "Allows you to manage work item categories.")
        {
            this.GetWorkItemCategoriesCommand = new RelayCommand(GetWorkItemCategories, CanGetWorkItemCategories);
            this.DeleteSelectedWorkItemCategoriesCommand = new RelayCommand(DeleteSelectedWorkItemCategories, CanDeleteSelectedWorkItemCategories);
            this.GetWorkItemCategoriesXmlCommand = new RelayCommand(GetWorkItemCategoriesXml, CanGetWorkItemCategoriesXml);
            this.ExportSelectedWorkItemCategoriesXmlCommand = new RelayCommand(ExportSelectedWorkItemCategoriesXml, CanExportSelectedWorkItemCategoriesXml);
            this.EditSelectedWorkItemCategoriesXmlCommand = new RelayCommand(EditSelectedWorkItemCategoriesXml, CanEditSelectedWorkItemCategoriesXml);
            this.TransformSelectedWorkItemCategoriesXmlCommand = new RelayCommand(TransformSelectedWorkItemCategoriesXml, CanTransformSelectedWorkItemCategoriesXml);
            this.LoadWorkItemCategoryListCommand = new RelayCommand(LoadWorkItemCategoryList, CanLoadWorkItemCategoryList);
            this.AddWorkItemCategoryCommand = new RelayCommand(AddWorkItemCategory, CanAddWorkItemCategory);
            this.RemoveSelectedWorkItemCategoryCommand = new RelayCommand(RemoveSelectedWorkItemCategory, CanRemoveSelectedWorkItemCategory);
            this.ImportWorkItemCategoryListCommand = new RelayCommand(ImportWorkItemCategoryList, CanImportWorkItemCategoryList);
        }

        #endregion

        #region Commands

        private bool CanGetWorkItemCategories(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetWorkItemCategories(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving work item categories", teamProjectNames.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var results = new List<WorkItemCategoryInfo>();
                foreach (var teamProjectName in teamProjectNames)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                    try
                    {
                        var project = store.Projects[teamProjectName];
                        var categoriesXml = WorkItemConfigurationItemImportExport.GetCategoriesXml(project);
                        var categoryList = WorkItemCategoryList.Load(categoriesXml);
                        foreach (var category in categoryList.Categories)
                        {
                            results.Add(new WorkItemCategoryInfo(teamProjectName, categoryList, category));
                        }
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProjectName), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                e.Result = results;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving work item categories", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.WorkItemCategories = (ICollection<WorkItemCategoryInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.WorkItemCategories.Count.ToCountString("work item category"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanDeleteSelectedWorkItemCategories(object argument)
        {
            return this.SelectedWorkItemCategories != null && this.SelectedWorkItemCategories.Count > 0;
        }

        private void DeleteSelectedWorkItemCategories(object argument)
        {
            var workItemCategoriesToDelete = this.SelectedWorkItemCategories;

            var result = MessageBox.Show("This will delete the selected work item categories. Certain applications like Microsoft Test Manager rely on categories so deleting them can reduce their functionality. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var task = new ApplicationTask("Deleting " + workItemCategoriesToDelete.Count.ToCountString("work item category"), workItemCategoriesToDelete.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                foreach (var categoriesByTeamProject in workItemCategoriesToDelete.GroupBy(c => c.TeamProject).ToList())
                {
                    var teamProjectName = categoriesByTeamProject.Key;
                    try
                    {
                        var project = store.Projects[teamProjectName];
                        var categoriesXml = WorkItemConfigurationItemImportExport.GetCategoriesXml(project);
                        var categoryList = WorkItemCategoryList.Load(categoriesXml);

                        foreach (var workItemCategoryToDelete in categoriesByTeamProject)
                        {
                            try
                            {
                                task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting work item category \"{0}\" from Team Project \"{1}\"", workItemCategoryToDelete.WorkItemCategory.Name, workItemCategoryToDelete.TeamProject));
                                var category = categoryList.Categories.FirstOrDefault(c => string.Equals(c.RefName, workItemCategoryToDelete.WorkItemCategory.RefName, StringComparison.OrdinalIgnoreCase));
                                if (category != null)
                                {
                                    categoryList.Categories.Remove(category);
                                }
                            }
                            catch (Exception exc)
                            {
                                task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting the work item category \"{0}\" for Team Project \"{1}\"", workItemCategoryToDelete.WorkItemCategory.Name, workItemCategoryToDelete.TeamProject), exc);
                            }
                        }

                        categoriesXml = categoryList.Save();
                        WorkItemConfigurationItemImportExport.SetCategories(project, categoriesXml);
                    }
                    catch (Exception exc)
                    {
                        task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProjectName), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while deleting work item categories", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete("Deleted " + workItemCategoriesToDelete.Count.ToCountString("work item category"));
                }

                // Refresh the list.
                GetWorkItemCategories(null);
            };
            worker.RunWorkerAsync();
        }

        private bool CanGetWorkItemCategoriesXml(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetWorkItemCategoriesXml(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving work item categories", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var results = new List<WorkItemConfigurationItemExport>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var project = store.Projects[teamProject.Name];
                        var categories = WorkItemConfigurationItemImportExport.GetCategories(project);
                        var categoriesInfo = new WorkItemConfigurationItemExport(teamProject, categories);
                        results.Add(categoriesInfo);
                    }
                    catch (Exception exc)
                    {
                        task.SetWarning(string.Format(CultureInfo.CurrentCulture, "An error occurred while processing Team Project \"{0}\"", teamProject.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
                e.Result = results;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while retrieving work item categories", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.WorkItemCategoriesXml = (ICollection<WorkItemConfigurationItemExport>)e.Result;
                    task.SetComplete("Retrieved " + this.WorkItemCategoriesXml.Count.ToCountString("work item category file"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanExportSelectedWorkItemCategoriesXml(object argument)
        {
            return CanEditSelectedWorkItemCategoriesXml(argument);
        }

        private void ExportSelectedWorkItemCategoriesXml(object argument)
        {
            var categoriesToExport = new List<WorkItemConfigurationItemExport>();
            var categoriesExports = this.SelectedWorkItemCategoriesXml.ToList();
            if (categoriesExports.Count == 1)
            {
                // Export to single file.
                var categoriesExport = categoriesExports.Single();
                var dialog = new SaveFileDialog();
                dialog.FileName = categoriesExport.Item.DisplayName + ".xml";
                dialog.Filter = "XML Files (*.xml)|*.xml";
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    categoriesToExport.Add(new WorkItemConfigurationItemExport(categoriesExport.TeamProject, categoriesExport.Item, dialog.FileName));
                }
            }
            else
            {
                // Export to a directory structure.
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select the path where to export the Work Item Categories files (*.xml). They will be stored in a folder per Team Project.";
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var rootFolder = dialog.SelectedPath;
                    foreach (var categoriesExport in categoriesExports)
                    {
                        var fileName = Path.Combine(rootFolder, categoriesExport.TeamProject.Name, categoriesExport.Item.DisplayName + ".xml");
                        categoriesToExport.Add(new WorkItemConfigurationItemExport(categoriesExport.TeamProject, categoriesExport.Item, fileName));
                    }
                }
            }

            var task = new ApplicationTask("Exporting work item categories", categoriesExports.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                WorkItemConfigurationItemImportExport.Export(this.Logger, task, categoriesToExport);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while exporting work item categories", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete("Exported " + categoriesToExport.Count.ToCountString("work item category file"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanEditSelectedWorkItemCategoriesXml(object argument)
        {
            return this.SelectedWorkItemCategoriesXml != null && this.SelectedWorkItemCategoriesXml.Any();
        }

        private async void EditSelectedWorkItemCategoriesXml(object argument)
        {
            if (string.IsNullOrEmpty(SelectedExternalEditor))
            {
                var categoriesToEdit = this.SelectedWorkItemCategoriesXml.ToList();
                var dialog = new WorkItemConfigurationItemEditorDialog(categoriesToEdit, "Work Item Category File");
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true)
                {
                    var options = dialog.Options;
                    var result = MessageBoxResult.Yes;
                    if (!options.HasFlag(ImportOptions.Simulate))
                    {
                        result = MessageBox.Show("This will import the edited work item categories. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    }
                    if (result == MessageBoxResult.Yes)
                    {
                        var teamProjectsWithCategories = categoriesToEdit.GroupBy(w => w.TeamProject).ToDictionary(g => g.Key, g => g.Select(w => w.Item).ToList());
                        PerformImport(teamProjectsWithCategories, options);
                    }
                }
            }
            else
            {
                var categoriesToEdit = this.SelectedWorkItemCategoriesXml.ToList();
                var editor = new EditorService(categoriesToEdit);
                var startEditor = await editor.StartEditor(SelectedExternalEditor);

                if (startEditor.Completed && startEditor.ExitCode == 0)
                {
                    var result = MessageBox.Show(
                        "This will import the edited work item categories. Are you sure you want to continue?",
                        "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        var teamProjectsWithCategories = categoriesToEdit.GroupBy(w => w.TeamProject)
                            .ToDictionary(g => g.Key, g => g.Select(w => w.Item).ToList());
                        PerformImport(teamProjectsWithCategories, ImportOptions.None);
                    }
                }
            }
        }

        private bool CanTransformSelectedWorkItemCategoriesXml(object argument)
        {
            return CanEditSelectedWorkItemCategoriesXml(argument);
        }

        private void TransformSelectedWorkItemCategoriesXml(object argument)
        {
            var categoriesToTransform = this.SelectedWorkItemCategoriesXml.ToList();
            var dialog = new WorkItemConfigurationItemTransformationEditorDialog(categoriesToTransform, "Work Item Category File");
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var options = dialog.Options;
                var result = MessageBoxResult.Yes;
                if (!options.HasFlag(ImportOptions.Simulate))
                {
                    result = MessageBox.Show("This will import the transformed work item categories. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }
                if (result == MessageBoxResult.Yes)
                {
                    var teamProjectsWithCategories = categoriesToTransform.GroupBy(w => w.TeamProject).ToDictionary(g => g.Key, g => g.Select(w => w.Item).ToList());
                    PerformImport(teamProjectsWithCategories, options);
                }
            }
        }

        private bool CanLoadWorkItemCategoryList(object argument)
        {
            return true;
        }

        private void LoadWorkItemCategoryList(object argument)
        {
            using (var dialog = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false))
            {
                var result = dialog.ShowDialog(Application.Current.MainWindow.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedProjects != null && dialog.SelectedProjects.Length > 0)
                {
                    var teamProjectCollection = dialog.SelectedTeamProjectCollection;
                    var teamProject = dialog.SelectedProjects.First();

                    var task = new ApplicationTask("Loading work item category list");
                    PublishStatus(new StatusEventArgs(task));
                    var worker = new BackgroundWorker();
                    worker.DoWork += (sender, e) =>
                    {
                        var tfs = GetTfsTeamProjectCollection(teamProjectCollection.Uri);
                        var store = tfs.GetService<WorkItemStore>();
                        var project = store.Projects[teamProject.Name];
                        var categoriesXml = WorkItemConfigurationItemImportExport.GetCategoriesXml(project);
                        var selectedWorkItemCategoryList = WorkItemCategoryList.Load(categoriesXml);
                        var availableWorkItemTypeReferences = project.WorkItemTypes.Cast<WorkItemType>().Select(w => new WorkItemTypeReference { Name = w.Name }).ToList();
                        e.Result = new Tuple<WorkItemCategoryList, ICollection<WorkItemTypeReference>>(selectedWorkItemCategoryList, availableWorkItemTypeReferences);
                    };
                    worker.RunWorkerCompleted += (sender, e) =>
                    {
                        if (e.Error != null)
                        {
                            Logger.Log("An unexpected exception occurred while importing the work item category list", e.Error);
                            task.SetError(e.Error);
                            task.SetComplete("An unexpected exception occurred");
                        }
                        else
                        {
                            var loadResult = (Tuple<WorkItemCategoryList, ICollection<WorkItemTypeReference>>)e.Result;
                            this.SelectedWorkItemCategoryList = loadResult.Item1;
                            this.AvailableWorkItemTypeReferences = loadResult.Item2;
                            this.SelectedWorkItemCategory = this.SelectedWorkItemCategoryList.Categories.FirstOrDefault();
                            task.SetComplete("Done");
                        }
                    };
                    worker.RunWorkerAsync();
                }
            }
        }

        private bool CanImportWorkItemCategoryList(object argument)
        {
            return IsAnyTeamProjectSelected() && this.SelectedWorkItemCategoryList != null;
        }

        private void ImportWorkItemCategoryList(object argument)
        {
            var result = MessageBox.Show("This will import the work item category list to all selected Team Projects. Certain applications like Microsoft Test Manager rely on categories so modifying them can reduce their functionality. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var task = new ApplicationTask("Importing work item category list", this.SelectedTeamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                var categoriesXml = this.SelectedWorkItemCategoryList.Save();
                foreach (var teamProject in this.SelectedTeamProjects)
                {
                    try
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Importing work item category list in Team Project \"{0}\"", teamProject.Name));
                        var project = store.Projects[teamProject.Name];
                        WorkItemConfigurationItemImportExport.SetCategories(project, categoriesXml);
                    }
                    catch (Exception exc)
                    {
                        task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while importing the work item category list for Team Project \"{0}\"", teamProject.Name), exc);
                    }
                    if (task.IsCanceled)
                    {
                        task.Status = "Canceled";
                        break;
                    }
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while importing the work item category list", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete(task.IsError ? "Failed" : (task.IsWarning ? "Succeeded with warnings" : "Succeeded"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanAddWorkItemCategory(object argument)
        {
            return this.SelectedWorkItemCategoryList != null;
        }

        private void AddWorkItemCategory(object argument)
        {
            var category = new WorkItemCategory { Name = "New Category" };
            this.SelectedWorkItemCategoryList.Categories.Add(category);
            this.SelectedWorkItemCategory = category;
        }

        private bool CanRemoveSelectedWorkItemCategory(object argument)
        {
            return this.SelectedWorkItemCategoryList != null && this.SelectedWorkItemCategory != null;
        }

        private void RemoveSelectedWorkItemCategory(object argument)
        {
            this.SelectedWorkItemCategoryList.Categories.Remove(this.SelectedWorkItemCategory);
            this.SelectedWorkItemCategory = this.SelectedWorkItemCategoryList.Categories.FirstOrDefault();
        }

        #endregion

        #region Overrides

        protected override bool IsTfsSupported(TeamFoundationServerInfo server)
        {
            return server.MajorVersion >= TfsMajorVersion.V10;
        }

        protected override void OnSelectedExternalEditorChanged()
        {
        }

        #endregion

        #region Helper Methods

        private void PerformImport(Dictionary<TeamProjectInfo, List<WorkItemConfigurationItem>> teamProjectsWithCategories, ImportOptions options)
        {
            var numberOfSteps = teamProjectsWithCategories.Aggregate(0, (a, p) => a += p.Value.Count);
            var task = new ApplicationTask("Importing work item categories", numberOfSteps, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                WorkItemConfigurationItemImportExport.Import(this.Logger, task, true, store, teamProjectsWithCategories, options);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while importing work item categories", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete(task.IsError ? "Failed" : (task.IsWarning ? "Succeeded with warnings" : "Succeeded"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion
    }
}