using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration.WorkItemTypes
{
    [Export]
    public class WorkItemTypesViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetWorkItemTypesCommand { get; private set; }
        public RelayCommand ExportSelectedWorkItemTypesCommand { get; private set; }
        public RelayCommand DeleteSelectedWorkItemTypesCommand { get; private set; }
        public RelayCommand EditSelectedWorkItemTypesCommand { get; private set; }
        public RelayCommand TransformSelectedWorkItemTypesCommand { get; private set; }

        public RelayCommand AddWorkItemTypeFileCommand { get; private set; }
        public RelayCommand RemoveSelectedWorkItemTypeFileCommand { get; private set; }
        public RelayCommand RemoveAllWorkItemTypeFilesCommand { get; private set; }
        public RelayCommand ImportCommand { get; private set; }

        public RelayCommand SearchCommand { get; private set; }

        public bool Simulate { get; set; }
        public bool SaveCopy { get; set; }

        #endregion

        #region Observable Properties

        public ICollection<WorkItemTypeInfo> WorkItemTypes
        {
            get { return this.GetValue(WorkItemTypesProperty); }
            set { this.SetValue(WorkItemTypesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeInfo>> WorkItemTypesProperty = new ObservableProperty<ICollection<WorkItemTypeInfo>, WorkItemTypesViewModel>(o => o.WorkItemTypes);

        public ICollection<WorkItemTypeInfo> SelectedWorkItemTypes
        {
            get { return this.GetValue(SelectedWorkItemTypesProperty); }
            set { this.SetValue(SelectedWorkItemTypesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeInfo>> SelectedWorkItemTypesProperty = new ObservableProperty<ICollection<WorkItemTypeInfo>, WorkItemTypesViewModel>(o => o.SelectedWorkItemTypes);

        public ObservableCollection<string> WorkItemTypeFiles
        {
            get { return this.GetValue(WorkItemTypeFilesProperty); }
            private set { this.SetValue(WorkItemTypeFilesProperty, value); }
        }

        public static ObservableProperty<ObservableCollection<string>> WorkItemTypeFilesProperty = new ObservableProperty<ObservableCollection<string>, WorkItemTypesViewModel>(o => o.WorkItemTypeFiles);

        public string SelectedWorkItemTypeFile
        {
            get { return this.GetValue(SelectedWorkItemTypeFileProperty); }
            set { this.SetValue(SelectedWorkItemTypeFileProperty, value); }
        }

        public static readonly ObservableProperty<string> SelectedWorkItemTypeFileProperty = new ObservableProperty<string, WorkItemTypesViewModel>(o => o.SelectedWorkItemTypeFile);

        public string SearchText
        {
            get { return this.GetValue(SearchTextProperty); }
            set { this.SetValue(SearchTextProperty, value); }
        }

        public static ObservableProperty<string> SearchTextProperty = new ObservableProperty<string, WorkItemTypesViewModel>(o => o.SearchText);

        public ICollection<SearchResult> SearchResults
        {
            get { return this.GetValue(SearchResultsProperty); }
            set { this.SetValue(SearchResultsProperty, value); }
        }

        public static ObservableProperty<ICollection<SearchResult>> SearchResultsProperty = new ObservableProperty<ICollection<SearchResult>, WorkItemTypesViewModel>(o => o.SearchResults);

        public bool SearchIncludesWorkItemFields
        {
            get { return this.GetValue(SearchIncludesWorkItemFieldsProperty); }
            set { this.SetValue(SearchIncludesWorkItemFieldsProperty, value); }
        }

        public static ObservableProperty<bool> SearchIncludesWorkItemFieldsProperty = new ObservableProperty<bool, WorkItemTypesViewModel>(o => o.SearchIncludesWorkItemFields, true);

        public bool SearchUsesExactMatch
        {
            get { return this.GetValue(SearchUsesExactMatchProperty); }
            set { this.SetValue(SearchUsesExactMatchProperty, value); }
        }

        public static ObservableProperty<bool> SearchUsesExactMatchProperty = new ObservableProperty<bool, WorkItemTypesViewModel>(o => o.SearchUsesExactMatch);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemTypesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Types", "Allows you to manage work item type definitions.")
        {
            this.WorkItemTypeFiles = new ObservableCollection<string>();

            this.GetWorkItemTypesCommand = new RelayCommand(GetWorkItemTypes, CanGetWorkItemTypes);
            this.ExportSelectedWorkItemTypesCommand = new RelayCommand(ExportSelectedWorkItemTypes, CanExportSelectedWorkItemTypes);
            this.DeleteSelectedWorkItemTypesCommand = new RelayCommand(DeleteSelectedWorkItemTypes, CanDeleteSelectedWorkItemTypes);
            this.EditSelectedWorkItemTypesCommand = new RelayCommand(EditSelectedWorkItemTypes, CanEditSelectedWorkItemTypes);
            this.TransformSelectedWorkItemTypesCommand = new RelayCommand(TransformSelectedWorkItemTypes, CanTransformSelectedWorkItemTypes);

            this.AddWorkItemTypeFileCommand = new RelayCommand(AddWorkItemTypeFile, CanAddWorkItemTypeFile);
            this.RemoveSelectedWorkItemTypeFileCommand = new RelayCommand(RemoveSelectedWorkItemTypeFile, CanRemoveSelectedWorkItemTypeFile);
            this.RemoveAllWorkItemTypeFilesCommand = new RelayCommand(RemoveAllWorkItemTypeFiles, CanRemoveAllWorkItemTypeFiles);
            this.ImportCommand = new RelayCommand(Import, CanImport);

            this.SearchCommand = new RelayCommand(Search, CanSearch);
        }

        #endregion

        #region Commands

        private bool CanGetWorkItemTypes(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetWorkItemTypes(object argument)
        {
            var teamProjects = this.SelectedTeamProjects.ToList();
            var task = new ApplicationTask("Retrieving work item types", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var results = new List<WorkItemTypeInfo>();
                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var project = store.Projects[teamProject.Name];
                        var categoriesXml = WorkItemConfigurationItemImportExport.GetCategoriesXml(project);
                        var categoryList = WorkItemCategoryList.Load(categoriesXml);

                        foreach (WorkItemType workItemType in project.WorkItemTypes)
                        {
                            var parameters = new Dictionary<string, object>() {
                                { "WorkItemType", workItemType.Name },
                                { "TeamProject", workItemType.Project.Name}
                            };
                            var workItemCount = default(int?);
                            try
                            {
                                workItemCount = store.QueryCount("SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = @WorkItemType AND [System.TeamProject] = @TeamProject", parameters);
                            }
                            catch (ClientException)
                            {
                                // Exceptions while running the query are not important and can be ignored.
                                // An example is "VS402337: The number of work items returned exceeds the size limit of 20000".
                            }
                            var referencingCategories = categoryList.Categories.Where(c => c.WorkItemTypes.Concat(new WorkItemTypeReference[] { c.DefaultWorkItemType }).Any(w => string.Equals(w.Name, workItemType.Name, StringComparison.OrdinalIgnoreCase))).Select(c => c.Name);
                            var workItemTypeDefinition = default(WorkItemTypeDefinition);
                            try
                            {
                                workItemTypeDefinition = WorkItemTypeDefinition.FromXml(workItemType.Export(false));
                            }
                            catch (VssServiceResponseException)
                            {
                                // A VssServiceResponseException with message "VS403207: The object does not exist or access is denied"
                                // happens when trying to export a work item type in the inherited model.
                                workItemTypeDefinition = new WorkItemTypeDefinition(workItemType.Name, null);
                            }
                            results.Add(new WorkItemTypeInfo(teamProject, workItemType.Name, workItemType.Description, workItemCount, referencingCategories.ToList(), workItemTypeDefinition));
                        }
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
                    Logger.Log("An unexpected exception occurred while retrieving work item types", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.WorkItemTypes = (ICollection<WorkItemTypeInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.WorkItemTypes.Count.ToCountString("work item type"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanDeleteSelectedWorkItemTypes(object argument)
        {
            return CanEditSelectedWorkItemTypes(argument);
        }

        private void DeleteSelectedWorkItemTypes(object argument)
        {
            var workItemTypesToDelete = this.SelectedWorkItemTypes;

            if (workItemTypesToDelete.Any(w => w.WorkItemCategories.Any()))
            {
                MessageBox.Show("You have selected work item types that are used in one or more categories, which means they cannot be deleted. You must remove them from the work item categories first.", "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("This will delete the selected work item types. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Warn even harder when there are work items that use the work item types.
            if (workItemTypesToDelete.Any(w => w.WorkItemCount == null || w.WorkItemCount.Value > 0))
            {
                result = MessageBox.Show("You have selected work item types have actual work item instances. These individual work items will be deleted when deleting the work item type. Are you REALLY sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // First try to get to the internal type that provides the API.
            MethodInfo destroyWorkItemTypeMethod = null;
            var internalAdminTypeName = "Microsoft.TeamFoundation.WorkItemTracking.Client.InternalAdmin";
            var destroyWorkItemTypeMethodName = "DestroyWorkItemType";
            var internalAdminType = typeof(WorkItemStore).Assembly.GetType(internalAdminTypeName, false, true);
            string errorDetail = null;
            if (internalAdminType == null)
            {
                errorDetail = string.Format(CultureInfo.CurrentCulture, "Could not load type \"{0}\".", internalAdminTypeName);
            }
            else
            {
                destroyWorkItemTypeMethod = internalAdminType.GetMethod(destroyWorkItemTypeMethodName, BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(WorkItemType) }, null);
                if (destroyWorkItemTypeMethod == null)
                {
                    errorDetail = string.Format(CultureInfo.CurrentCulture, "Could not find public static method \"{0}\" on type \"{1}\".", destroyWorkItemTypeMethodName, internalAdminTypeName);
                }
            }

            if (destroyWorkItemTypeMethod == null)
            {
                var message = "There was a problem finding the internal TFS implementation to delete work item types.";
                Logger.Log(string.Concat(message, " ", errorDetail), TraceEventType.Warning);
                MessageBox.Show(message + " See the log file for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var task = new ApplicationTask("Deleting " + workItemTypesToDelete.Count.ToCountString("work item type"), workItemTypesToDelete.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                foreach (var workItemTypeToDelete in workItemTypesToDelete)
                {
                    try
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting work item type \"{0}\" from Team Project \"{1}\"", workItemTypeToDelete.Name, workItemTypeToDelete.TeamProject.Name));
                        var project = store.Projects[workItemTypeToDelete.TeamProject.Name];
                        var tfsWorkItemType = project.WorkItemTypes[workItemTypeToDelete.Name];
                        destroyWorkItemTypeMethod.Invoke(null, new object[] { tfsWorkItemType });
                    }
                    catch (Exception exc)
                    {
                        task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting the work item type \"{0}\" for Team Project \"{1}\"", workItemTypeToDelete.Name, workItemTypeToDelete.TeamProject.Name), exc);
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
                    Logger.Log("An unexpected exception occurred while deleting work item types", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete("Deleted " + workItemTypesToDelete.Count.ToCountString("work item type"));
                }

                // Refresh the list.
                GetWorkItemTypes(null);
            };
            worker.RunWorkerAsync();
        }

        private bool CanExportSelectedWorkItemTypes(object argument)
        {
            return CanEditSelectedWorkItemTypes(argument);
        }

        private void ExportSelectedWorkItemTypes(object argument)
        {
            var workItemTypesToExport = new List<WorkItemConfigurationItemExport>();
            var workItemTypes = this.SelectedWorkItemTypes;
            if (workItemTypes.Count == 1)
            {
                // Export to single file.
                var workItemType = workItemTypes.Single();
                var dialog = new SaveFileDialog();
                dialog.FileName = workItemType.Name + ".xml";
                dialog.Filter = "XML Files (*.xml)|*.xml";
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    workItemTypesToExport.Add(new WorkItemConfigurationItemExport(workItemType.TeamProject, workItemType.WorkItemTypeDefinition, dialog.FileName));
                }
            }
            else
            {
                // Export to a directory structure.
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select the path where to export the Work Item Type Definition files (*.xml). They will be stored in a folder per Team Project.";
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var rootFolder = dialog.SelectedPath;
                    foreach (var workItemType in workItemTypes)
                    {
                        var fileName = Path.Combine(rootFolder, workItemType.TeamProject.Name, workItemType.Name + ".xml");
                        workItemTypesToExport.Add(new WorkItemConfigurationItemExport(workItemType.TeamProject, workItemType.WorkItemTypeDefinition, fileName));
                    }
                }
            }

            if (workItemTypesToExport.Any())
            {
                var task = new ApplicationTask("Exporting " + workItemTypesToExport.Count.ToCountString("work item type"), workItemTypesToExport.Count, true);
                PublishStatus(new StatusEventArgs(task));
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    WorkItemConfigurationItemImportExport.Export(this.Logger, task, workItemTypesToExport);
                };
                worker.RunWorkerCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Logger.Log("An unexpected exception occurred while exporting work item types", e.Error);
                        task.SetError(e.Error);
                        task.SetComplete("An unexpected exception occurred");
                    }
                    else
                    {
                        task.SetComplete("Exported " + workItemTypesToExport.Count.ToCountString("work item type"));
                    }
                };
                worker.RunWorkerAsync();
            }
        }

        private bool CanEditSelectedWorkItemTypes(object argument)
        {
            return (this.SelectedWorkItemTypes != null && this.SelectedWorkItemTypes.Count > 0 && this.SelectedWorkItemTypes.All(w => w.WorkItemTypeDefinition.CanEdit));
        }

        private void EditSelectedWorkItemTypes(object argument)
        {
            var workItemTypesToEdit = this.SelectedWorkItemTypes.ToList();
            var dialog = new WorkItemConfigurationItemEditorDialog(workItemTypesToEdit.Select(w => new WorkItemConfigurationItemExport(w.TeamProject, w.WorkItemTypeDefinition)).ToList(), "Work Item Type");
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var options = dialog.Options;
                var result = MessageBoxResult.Yes;
                if (!options.HasFlag(ImportOptions.Simulate))
                {
                    result = MessageBox.Show("This will import the edited work item types. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }
                if (result == MessageBoxResult.Yes)
                {
                    var teamProjectsWithWorkItemTypes = workItemTypesToEdit.GroupBy(w => w.TeamProject).ToDictionary(g => g.Key, g => g.Select(w => (WorkItemConfigurationItem)w.WorkItemTypeDefinition).ToList());
                    PerformImport(options, teamProjectsWithWorkItemTypes);
                }
            }
        }

        private bool CanTransformSelectedWorkItemTypes(object argument)
        {
            return CanEditSelectedWorkItemTypes(argument);
        }

        private void TransformSelectedWorkItemTypes(object argument)
        {
            var workItemTypesToTransform = this.SelectedWorkItemTypes.ToList();
            var dialog = new WorkItemConfigurationItemTransformationEditorDialog(workItemTypesToTransform.Select(w => new WorkItemConfigurationItemExport(w.TeamProject, w.WorkItemTypeDefinition)).ToList(), "Work Item Type");
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var options = dialog.Options;
                var result = MessageBoxResult.Yes;
                if (!options.HasFlag(ImportOptions.Simulate))
                {
                    result = MessageBox.Show("This will import the transformed work item types. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }
                if (result == MessageBoxResult.Yes)
                {
                    var teamProjectsWithWorkItemTypes = workItemTypesToTransform.GroupBy(w => w.TeamProject).ToDictionary(g => g.Key, g => g.Select(w => (WorkItemConfigurationItem)w.WorkItemTypeDefinition).ToList());
                    PerformImport(options, teamProjectsWithWorkItemTypes);
                }
            }
        }

        private bool CanSearch(object argument)
        {
            return IsAnyTeamProjectSelected() && !string.IsNullOrEmpty(this.SearchText);
        }

        private void Search(object argument)
        {
            var searchText = this.SearchText;
            var searchUsesExactMatch = this.SearchUsesExactMatch;
            var searchIncludesWorkItemFields = this.SearchIncludesWorkItemFields;
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask(string.Format(CultureInfo.CurrentCulture, "Searching for \"{0}\"", searchText), teamProjectNames.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var results = new List<SearchResult>();
                foreach (var teamProjectName in teamProjectNames)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                    try
                    {
                        var project = store.Projects[teamProjectName];
                        foreach (WorkItemType workItemType in project.WorkItemTypes)
                        {
                            if (Matches(searchText, searchUsesExactMatch, workItemType.Name))
                            {
                                results.Add(new SearchResult(teamProjectName, "Work Item", workItemType.Name, string.Format(CultureInfo.CurrentCulture, "Matching work item name: \"{0}\"", workItemType.Name)));
                            }
                            else if (Matches(searchText, searchUsesExactMatch, workItemType.Description))
                            {
                                results.Add(new SearchResult(teamProjectName, "Work Item", workItemType.Name, string.Format(CultureInfo.CurrentCulture, "Matching work item description: \"{0}\"", workItemType.Description)));
                            }
                            if (searchIncludesWorkItemFields)
                            {
                                foreach (FieldDefinition field in workItemType.FieldDefinitions)
                                {
                                    if (Matches(searchText, searchUsesExactMatch, field.Name))
                                    {
                                        results.Add(new SearchResult(teamProjectName, "Work Item Field", string.Concat(workItemType.Name, ".", field.Name), string.Format(CultureInfo.CurrentCulture, "Matching field name: \"{0}\"", field.Name)));
                                    }
                                    else if (Matches(searchText, searchUsesExactMatch, field.ReferenceName))
                                    {
                                        results.Add(new SearchResult(teamProjectName, "Work Item Field", string.Concat(workItemType.Name, ".", field.Name), string.Format(CultureInfo.CurrentCulture, "Matching field reference name: \"{0}\"", field.ReferenceName)));
                                    }
                                    else if (Matches(searchText, searchUsesExactMatch, field.HelpText))
                                    {
                                        results.Add(new SearchResult(teamProjectName, "Work Item Field", string.Concat(workItemType.Name, ".", field.Name), string.Format(CultureInfo.CurrentCulture, "Matching field help text: \"{0}\"", field.HelpText)));
                                    }
                                }
                            }
                            if (task.IsCanceled)
                            {
                                break;
                            }
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
                    Logger.Log("An unexpected exception occurred while searching", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    this.SearchResults = (ICollection<SearchResult>)e.Result;
                    task.SetComplete("Found " + this.SearchResults.Count.ToCountString("result"));
                }
            };
            worker.RunWorkerAsync();
        }

        private bool CanAddWorkItemTypeFile(object argument)
        {
            return true;
        }

        private void AddWorkItemTypeFile(object argument)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the Work Item Type Definition files (*.xml) to add.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            dialog.Multiselect = true;
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                foreach (var workItemTypeFileName in dialog.FileNames)
                {
                    this.WorkItemTypeFiles.Add(workItemTypeFileName);
                }
            }
        }

        private bool CanRemoveSelectedWorkItemTypeFile(object argument)
        {
            return this.SelectedWorkItemTypeFile != null;
        }

        private void RemoveSelectedWorkItemTypeFile(object argument)
        {
            this.WorkItemTypeFiles.Remove(this.SelectedWorkItemTypeFile);
        }

        private bool CanRemoveAllWorkItemTypeFiles(object argument)
        {
            return this.WorkItemTypeFiles.Any();
        }

        private void RemoveAllWorkItemTypeFiles(object argument)
        {
            this.WorkItemTypeFiles.Clear();
        }

        private bool CanImport(object argument)
        {
            return IsAnyTeamProjectSelected() && this.WorkItemTypeFiles.Any();
        }

        private void Import(object argument)
        {
            var options = ImportOptions.None;
            if (this.Simulate)
            {
                options |= ImportOptions.Simulate;
            }
            if (this.SaveCopy)
            {
                options |= ImportOptions.SaveCopy;
            }
            var workItemTypes = new List<WorkItemConfigurationItem>();
            foreach (var workItemTypeFile in this.WorkItemTypeFiles)
            {
                try
                {
                    workItemTypes.Add(WorkItemTypeDefinition.FromFile(workItemTypeFile));
                }
                catch (Exception exc)
                {
                    this.Logger.Log(string.Format(CultureInfo.CurrentCulture, "An error occurred while loading the work item type from \"{0}\"", workItemTypeFile), exc);
                    MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "An error occurred while loading the work item type from \"{0}\". See the log file for details", workItemTypeFile), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            var teamProjectsWithWorkItemTypes = this.SelectedTeamProjects.ToDictionary(p => p, p => workItemTypes);
            if (!options.HasFlag(ImportOptions.Simulate))
            {
                var result = MessageBox.Show("This will import the selected work item types. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            PerformImport(options, teamProjectsWithWorkItemTypes);
        }

        #endregion

        #region Helper Methods

        private void PerformImport(ImportOptions options, Dictionary<TeamProjectInfo, List<WorkItemConfigurationItem>> teamProjectsWithWorkItemTypes)
        {
            var numberOfSteps = teamProjectsWithWorkItemTypes.Aggregate(0, (a, p) => a += p.Value.Count);
            var task = new ApplicationTask("Importing work item types", numberOfSteps, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                WorkItemConfigurationItemImportExport.Import(this.Logger, task, true, store, teamProjectsWithWorkItemTypes, options);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while importing work item types", e.Error);
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

        private static bool Matches(string searchText, bool exactMatch, string value)
        {
            return (value != null && (exactMatch ? value.Equals(searchText, StringComparison.CurrentCultureIgnoreCase) : value.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        #endregion
    }
}