using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemTypes
{
    [Export]
    public class WorkItemTypesViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetWorkItemTypesCommand { get; private set; }
        public RelayCommand ExportSelectedWorkItemTypesCommand { get; private set; }
        public RelayCommand DeleteSelectedWorkItemTypesCommand { get; private set; }
        public RelayCommand BrowseWorkItemTypesFilePathCommand { get; private set; }
        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand ValidateCommand { get; private set; }
        public RelayCommand ValidateAndImportCommand { get; private set; }
        public RelayCommand ImportCommand { get; private set; }

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

        public string WorkItemTypesFilePath
        {
            get { return this.GetValue(WorkItemTypesFilePathProperty); }
            set { this.SetValue(WorkItemTypesFilePathProperty, value); }
        }

        public static ObservableProperty<string> WorkItemTypesFilePathProperty = new ObservableProperty<string, WorkItemTypesViewModel>(o => o.WorkItemTypesFilePath, OnWorkItemTypesFilePathChanged);

        public ICollection<WorkItemTypeFile> WorkItemTypeFiles
        {
            get { return this.GetValue(WorkItemTypeFilesProperty); }
            set { this.SetValue(WorkItemTypeFilesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeFile>> WorkItemTypeFilesProperty = new ObservableProperty<ICollection<WorkItemTypeFile>, WorkItemTypesViewModel>(o => o.WorkItemTypeFiles);

        public ICollection<WorkItemTypeFile> SelectedWorkItemTypeFiles
        {
            get { return this.GetValue(SelectedWorkItemTypeFilesProperty); }
            set { this.SetValue(SelectedWorkItemTypeFilesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemTypeFile>> SelectedWorkItemTypeFilesProperty = new ObservableProperty<ICollection<WorkItemTypeFile>, WorkItemTypesViewModel>(o => o.SelectedWorkItemTypeFiles);

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
            : base(eventAggregator, logger, "Work Item Types", "Allows you to manage work item type definitions and work item categories.")
        {
            this.GetWorkItemTypesCommand = new RelayCommand(GetWorkItemTypes, CanGetWorkItemTypes);
            this.ExportSelectedWorkItemTypesCommand = new RelayCommand(ExportSelectedWorkItemTypes, CanExportSelectedWorkItemTypes);
            this.DeleteSelectedWorkItemTypesCommand = new RelayCommand(DeleteSelectedWorkItemTypes, CanDeleteSelectedWorkItemTypes);
            this.BrowseWorkItemTypesFilePathCommand = new RelayCommand(BrowseWorkItemTypesFilePath, CanBrowseWorkItemTypesFilePath);
            this.SearchCommand = new RelayCommand(Search, CanSearch);
            this.ValidateCommand = new RelayCommand(Validate, CanValidate);
            this.ValidateAndImportCommand = new RelayCommand(ValidateAndImport, CanValidateAndImport);
            this.ImportCommand = new RelayCommand(Import, CanImport);
        }

        #endregion

        #region Events

        private static void OnWorkItemTypesFilePathChanged(ObservableObject sender, ObservablePropertyChangedEventArgs<string> args)
        {
            var viewModel = (WorkItemTypesViewModel)sender;
            var path = viewModel.WorkItemTypesFilePath;
            if (Directory.Exists(path))
            {
                viewModel.WorkItemTypeFiles = Directory.GetFiles(path, "*.xml").Select(f => new WorkItemTypeFile(f)).ToList();
            }
            else
            {
                viewModel.WorkItemTypeFiles = null;
            }
        }

        #endregion

        #region Commands

        private bool CanGetWorkItemTypes(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetWorkItemTypes(object argument)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var task = new ApplicationTask("Retrieving work item types", teamProjectNames.Count);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
                {
                    var store = tfs.GetService<WorkItemStore>();

                    var results = new List<WorkItemTypeInfo>();
                    foreach (var teamProjectName in teamProjectNames)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                        var project = store.Projects[teamProjectName];
                        foreach (WorkItemType workItemType in project.WorkItemTypes)
                        {
                            var workItemCount = store.QueryCount("SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = '" + workItemType.Name.Replace("'", "''") + "' AND [System.TeamProject] = '" + workItemType.Project.Name.Replace("'", "''") + "'");
                            var categoriesXml = project.Categories.Export();
                            var categoryList = WorkItemCategoryList.Load(categoriesXml);
                            var referencingCategories = categoryList.Categories.Where(c => c.WorkItemTypes.Concat(new WorkItemTypeReference[] { c.DefaultWorkItemType }).Any(w => string.Equals(w.Name, workItemType.Name, StringComparison.OrdinalIgnoreCase))).Select(c => c.Name);
                            results.Add(new WorkItemTypeInfo(teamProjectName, workItemType.Name, workItemType.Description, workItemCount, referencingCategories.ToList()));
                        }
                    }
                    e.Result = results;
                }
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
            return (this.SelectedWorkItemTypes != null && this.SelectedWorkItemTypes.Count > 0);
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
            if (workItemTypesToDelete.Any(w => w.WorkItemCount > 0))
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

            var task = new ApplicationTask("Deleting " + workItemTypesToDelete.Count.ToCountString("work item type"), workItemTypesToDelete.Count);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
                {
                    var store = tfs.GetService<WorkItemStore>();

                    foreach (var workItemTypeToDelete in workItemTypesToDelete)
                    {
                        try
                        {
                            task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting work item type \"{0}\" from Team Project \"{1}\"", workItemTypeToDelete.Name, workItemTypeToDelete.TeamProject));
                            var project = store.Projects[workItemTypeToDelete.TeamProject];
                            var tfsWorkItemType = project.WorkItemTypes[workItemTypeToDelete.Name];
                            destroyWorkItemTypeMethod.Invoke(null, new object[] { tfsWorkItemType });
                        }
                        catch (Exception exc)
                        {
                            task.SetError(string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting the work item type \"{0}\" for Team Project \"{1}\"", workItemTypeToDelete.Name, workItemTypeToDelete.TeamProject), exc);
                        }
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
            return (this.SelectedWorkItemTypes != null && this.SelectedWorkItemTypes.Count > 0);
        }

        private void ExportSelectedWorkItemTypes(object argument)
        {
            var workItemTypesToExport = new List<Tuple<WorkItemTypeInfo, string>>();
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
                    workItemTypesToExport.Add(new Tuple<WorkItemTypeInfo, string>(workItemType, dialog.FileName));
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
                        var fileName = Path.Combine(rootFolder, workItemType.TeamProject, workItemType.Name + ".xml");
                        workItemTypesToExport.Add(new Tuple<WorkItemTypeInfo, string>(workItemType, fileName));
                    }
                }
            }

            if (workItemTypesToExport.Count > 0)
            {
                var task = new ApplicationTask("Exporting " + workItemTypesToExport.Count.ToCountString("work item type"), workItemTypesToExport.Count);
                PublishStatus(new StatusEventArgs(task));
                var step = 0;
                var worker = new BackgroundWorker();
                worker.DoWork += (sender, e) =>
                {
                    using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
                    {
                        var store = tfs.GetService<WorkItemStore>();

                        var results = new List<WorkItemTypeInfo>();
                        foreach (var workItemTypeToExport in workItemTypesToExport)
                        {
                            task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Exporting work item type \"{0}\" from Team Project \"{1}\"", workItemTypeToExport.Item1.Name, workItemTypeToExport.Item1.TeamProject));
                            Directory.CreateDirectory(Path.GetDirectoryName(workItemTypeToExport.Item2));
                            var project = store.Projects[workItemTypeToExport.Item1.TeamProject];
                            var workItemType = project.WorkItemTypes[workItemTypeToExport.Item1.Name];
                            var workItemTypeXml = workItemType.Export(false);
                            workItemTypeXml.Save(workItemTypeToExport.Item2);
                        }
                    }
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

        private bool CanBrowseWorkItemTypesFilePath(object arguments)
        {
            return true;
        }

        private void BrowseWorkItemTypesFilePath(object argument)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Please select the path where the Work Item Type Definition files (*.xml) are stored.";
            dialog.SelectedPath = this.WorkItemTypesFilePath;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.WorkItemTypesFilePath = dialog.SelectedPath;
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
            var task = new ApplicationTask(string.Format(CultureInfo.CurrentCulture, "Searching for \"{0}\"", searchText), teamProjectNames.Count);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                using (var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.SelectedTeamProjectCollection.Uri))
                {
                    var store = tfs.GetService<WorkItemStore>();

                    var results = new List<SearchResult>();
                    foreach (var teamProjectName in teamProjectNames)
                    {
                        task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
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
                        }
                    }
                    e.Result = results;
                }
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

        private bool CanValidate(object argument)
        {
            return IsAnyTeamProjectSelected() && this.SelectedWorkItemTypeFiles != null && this.SelectedWorkItemTypeFiles.Count > 0;
        }

        private void Validate(object argument)
        {
            PerformImport("Validating work item types", ImportOptions.Validate);
        }

        private bool CanValidateAndImport(object argument)
        {
            return CanValidate(argument);
        }

        private void ValidateAndImport(object argument)
        {
            PerformImport("Validating and importing work item types", ImportOptions.Validate | ImportOptions.Import);
        }

        private bool CanImport(object argument)
        {
            return CanValidate(argument);
        }

        private void Import(object argument)
        {
            PerformImport("Importing work item types", ImportOptions.Import);
        }

        #endregion

        #region Helper Methods

        private void PerformImport(string description, ImportOptions options)
        {
            var teamProjectNames = this.SelectedTeamProjects.Select(p => p.Name).ToList();
            var numberOfSteps = GetTotalNumberOfSteps(options, teamProjectNames, this.SelectedWorkItemTypeFiles);
            var task = new ApplicationTask(description, numberOfSteps);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                new WorkItemTypeImporter().ImportWorkItemTypes(task, options, this.SelectedTeamProjectCollection.Uri, teamProjectNames, this.SelectedWorkItemTypeFiles);
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while " + description.ToLower(CultureInfo.CurrentCulture), e.Error);
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

        private static int GetTotalNumberOfSteps(ImportOptions options, ICollection<string> teamProjectNames, ICollection<WorkItemTypeFile> workItemTypeFiles)
        {
            var numberOfSteps = 0;
            var numberOfImports = teamProjectNames.Count * workItemTypeFiles.Count;
            if (options.HasFlag(ImportOptions.Validate))
            {
                numberOfSteps += numberOfImports;
            }
            if (options.HasFlag(ImportOptions.Import))
            {
                numberOfSteps += numberOfImports;
            }
            return numberOfSteps;
        }

        private static bool Matches(string searchText, bool exactMatch, string value)
        {
            return (value != null && (exactMatch ? value.Equals(searchText, StringComparison.CurrentCultureIgnoreCase) : value.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        #endregion
    }
}