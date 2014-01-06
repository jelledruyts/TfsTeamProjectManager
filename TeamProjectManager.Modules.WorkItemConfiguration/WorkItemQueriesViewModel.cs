using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public class WorkItemQueriesViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand GetWorkItemQueriesCommand { get; private set; }
        public RelayCommand ExportSelectedWorkItemQueriesCommand { get; private set; }
        public RelayCommand DeleteSelectedWorkItemQueriesCommand { get; private set; }
        public RelayCommand EditSelectedWorkItemQueriesCommand { get; private set; }
        public RelayCommand ScanForWorkItemQueriesToImportCommand { get; private set; }
        public RelayCommand ImportWorkItemQueriesCommand { get; private set; }

        #endregion

        #region Observable Properties

        public ICollection<WorkItemQueryInfo> WorkItemQueries
        {
            get { return this.GetValue(WorkItemQueriesProperty); }
            set { this.SetValue(WorkItemQueriesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemQueryInfo>> WorkItemQueriesProperty = new ObservableProperty<ICollection<WorkItemQueryInfo>, WorkItemQueriesViewModel>(o => o.WorkItemQueries);

        public ICollection<WorkItemQueryInfo> SelectedWorkItemQueries
        {
            get { return this.GetValue(SelectedWorkItemQueriesProperty); }
            set { this.SetValue(SelectedWorkItemQueriesProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemQueryInfo>> SelectedWorkItemQueriesProperty = new ObservableProperty<ICollection<WorkItemQueryInfo>, WorkItemQueriesViewModel>(o => o.SelectedWorkItemQueries);

        public ICollection<WorkItemQueryInfo> WorkItemQueriesToImport
        {
            get { return this.GetValue(WorkItemQueriesToImportProperty); }
            set { this.SetValue(WorkItemQueriesToImportProperty, value); }
        }

        public static ObservableProperty<ICollection<WorkItemQueryInfo>> WorkItemQueriesToImportProperty = new ObservableProperty<ICollection<WorkItemQueryInfo>, WorkItemQueriesViewModel>(o => o.WorkItemQueriesToImport);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemQueriesViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Queries", "Allows you to manage work item queries.")
        {
            this.GetWorkItemQueriesCommand = new RelayCommand(GetWorkItemQueries, CanGetWorkItemQueries);
            this.ExportSelectedWorkItemQueriesCommand = new RelayCommand(ExportSelectedWorkItemQueries, CanExportSelectedWorkItemQueries);
            this.DeleteSelectedWorkItemQueriesCommand = new RelayCommand(DeleteSelectedWorkItemQueries, CanDeleteSelectedWorkItemQueries);
            this.EditSelectedWorkItemQueriesCommand = new RelayCommand(EditSelectedWorkItemQueries, CanEditSelectedWorkItemQueries);
            this.ScanForWorkItemQueriesToImportCommand = new RelayCommand(ScanForWorkItemQueriesToImport, CanScanForWorkItemQueriesToImport);
            this.ImportWorkItemQueriesCommand = new RelayCommand(ImportWorkItemQueries, CanImportWorkItemQueries);
        }

        #endregion

        #region GetWorkItemQueries Command

        private bool CanGetWorkItemQueries(object argument)
        {
            return IsAnyTeamProjectSelected();
        }

        private void GetWorkItemQueries(object argument)
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

                var results = new List<WorkItemQueryInfo>();
                foreach (var teamProjectName in teamProjectNames)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProjectName));
                    try
                    {
                        var project = store.Projects[teamProjectName];
                        GetTeamQueries(teamProjectName, project.QueryHierarchy, results);
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
                    this.WorkItemQueries = (ICollection<WorkItemQueryInfo>)e.Result;
                    task.SetComplete("Retrieved " + this.WorkItemQueries.Count.ToCountString("work item query"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion

        #region ExportSelectedWorkItemQueries Command

        private bool CanExportSelectedWorkItemQueries(object argument)
        {
            return this.SelectedWorkItemQueries != null && this.SelectedWorkItemQueries.Any();
        }

        private void ExportSelectedWorkItemQueries(object argument)
        {
            var workItemQueriesToExport = new List<WorkItemQueryExport>();
            var workItemQueries = this.SelectedWorkItemQueries;
            if (workItemQueries.Count == 1)
            {
                // Export to single file.
                var workItemQuery = workItemQueries.Single();
                var dialog = new SaveFileDialog();
                dialog.FileName = workItemQuery.Name + ".wiq";
                dialog.Filter = "Work Item Query Files (*.wiq)|*.wiq";
                var result = dialog.ShowDialog(Application.Current.MainWindow);
                if (result == true)
                {
                    workItemQueriesToExport.Add(new WorkItemQueryExport(workItemQuery, dialog.FileName));
                }
            }
            else
            {
                // Export to a directory structure.
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please select the path where to export the Work Item Query files (*.wiq). They will be stored in a folder per Team Project.";
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var rootFolder = dialog.SelectedPath;
                    foreach (var workItemQuery in workItemQueries)
                    {
                        var fileName = Path.Combine(rootFolder, workItemQuery.TeamProject, workItemQuery.Path, workItemQuery.Name + ".wiq");
                        workItemQueriesToExport.Add(new WorkItemQueryExport(workItemQuery, fileName));
                    }
                }
            }

            var task = new ApplicationTask("Exporting " + workItemQueriesToExport.Count.ToCountString("work item query"), workItemQueriesToExport.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var step = 0;
                foreach (var workItemQueryToExport in workItemQueriesToExport)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Exporting \"{0}\" from Team Project \"{1}\"", workItemQueryToExport.Query.Name, workItemQueryToExport.Query.TeamProject));
                    try
                    {
                        if (!string.IsNullOrEmpty(workItemQueryToExport.SaveAsFileName))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(workItemQueryToExport.SaveAsFileName));
                            workItemQueryToExport.WrapInXmlDocument().Save(workItemQueryToExport.SaveAsFileName);
                        }
                    }
                    catch (Exception exc)
                    {
                        var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while exporting \"{0}\"", workItemQueryToExport.Query.Name);
                        this.Logger.Log(message, exc);
                        task.SetError(message, exc);
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
                    Logger.Log("An unexpected exception occurred while exporting work item queries", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete("Exported " + workItemQueriesToExport.Count.ToCountString("work item query"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion

        #region DeleteSelectedWorkItemQueries Command

        private bool CanDeleteSelectedWorkItemQueries(object argument)
        {
            return this.SelectedWorkItemQueries != null && this.SelectedWorkItemQueries.Any();
        }

        private void DeleteSelectedWorkItemQueries(object argument)
        {
            var result = MessageBox.Show("This will delete the selected work item queries. Are you sure you want to continue?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var workItemQueries = this.SelectedWorkItemQueries.ToList();
            var task = new ApplicationTask("Deleting " + workItemQueries.Count.ToCountString("work item query"), workItemQueries.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                var step = 0;
                foreach (var workItemQuery in workItemQueries)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Deleting \"{0}\" from Team Project \"{1}\"", workItemQuery.Name, workItemQuery.TeamProject));
                    try
                    {
                        var project = store.Projects[workItemQuery.TeamProject];
                        var rootFolder = project.QueryHierarchy;
                        var query = rootFolder.Find(workItemQuery.Id);
                        if (query == null)
                        {
                            var message = string.Format(CultureInfo.CurrentCulture, "The work item query \"{0}\" could not be found", workItemQuery.Name);
                            this.Logger.Log(message, TraceEventType.Warning);
                            task.SetWarning(message);
                        }
                        else
                        {
                            query.Delete();
                        }
                    }
                    catch (Exception exc)
                    {
                        var message = string.Format(CultureInfo.CurrentCulture, "An error occurred while deleting \"{0}\"", workItemQuery.Name);
                        this.Logger.Log(message, exc);
                        task.SetError(message, exc);
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
                    Logger.Log("An unexpected exception occurred while deleting work item queries", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    task.SetComplete("Deleted " + workItemQueries.Count.ToCountString("work item query"));
                }

                // Refresh the list.
                GetWorkItemQueries(null);
            };
            worker.RunWorkerAsync();
        }

        #endregion

        #region EditSelectedWorkItemQueries Command

        private bool CanEditSelectedWorkItemQueries(object argument)
        {
            return this.SelectedWorkItemQueries != null && this.SelectedWorkItemQueries.Any();
        }

        private void EditSelectedWorkItemQueries(object argument)
        {
            var queriesToEdit = this.SelectedWorkItemQueries.ToList();
            var dialog = new WorkItemQueryEditorDialog(queriesToEdit);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var result = MessageBox.Show("This will import the edited work item queries. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var teamProjectsWithQueries = queriesToEdit.GroupBy(w => w.TeamProject).ToDictionary(g => g.Key, g => g.Select(w => w).ToList());
                    PerformImport(teamProjectsWithQueries);
                }
            }
        }

        #endregion

        #region ScanForWorkItemQueriesToImport Command

        private bool CanScanForWorkItemQueriesToImport(object arguments)
        {
            return true;
        }

        private void ScanForWorkItemQueriesToImport(object arguments)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Please select the path where to recursively look for Work Item Query files (*.wiq). They will be imported into the \"Team Queries\" folder in the same folder structure.";
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var queries = new List<WorkItemQueryInfo>();
                GetTeamQueryFiles(dialog.SelectedPath, dialog.SelectedPath, queries);
                this.WorkItemQueriesToImport = queries;
            }
        }

        #endregion

        #region ImportWorkItemQueries Command

        private bool CanImportWorkItemQueries(object arguments)
        {
            return IsAnyTeamProjectSelected() && this.WorkItemQueriesToImport != null && this.WorkItemQueriesToImport.Any();
        }

        private void ImportWorkItemQueries(object arguments)
        {
            var queriesToImport = this.WorkItemQueriesToImport.ToList();
            var result = MessageBox.Show("This will import the specified work item queries. Are you sure you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var teamProjectsWithQueries = this.SelectedTeamProjects.ToDictionary(p => p.Name, p => queriesToImport);
                PerformImport(teamProjectsWithQueries);
            }
        }

        #endregion

        #region Helper Methods

        private static void GetTeamQueryFiles(string rootFolder, string currentFolder, ICollection<WorkItemQueryInfo> queries)
        {
            foreach (var wiqFile in Directory.GetFiles(currentFolder, "*.wiq"))
            {
                var path = currentFolder.Substring(rootFolder.Length).Replace(@"\", "/");
                if (path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }
                var wiql = new XmlDocument();
                wiql.Load(wiqFile);
                var queryText = WorkItemQueryExport.GetQueryTextFromXml(wiql);
                queries.Add(new WorkItemQueryInfo(path, Path.GetFileNameWithoutExtension(wiqFile), queryText));
            }
            foreach (var directory in Directory.GetDirectories(currentFolder))
            {
                GetTeamQueryFiles(rootFolder, directory, queries);
            }
        }

        private static void GetTeamQueries(string teamProjectName, QueryFolder folder, ICollection<WorkItemQueryInfo> queries)
        {
            foreach (var item in folder)
            {
                var definition = item as QueryDefinition;
                if (definition == null)
                {
                    // We're in a folder; only process shared (non-personal) folders.
                    if (!item.IsPersonal)
                    {
                        GetTeamQueries(teamProjectName, (QueryFolder)item, queries);
                    }
                }
                else
                {
                    // Strip the root nodes ("<TeamProjectName>/Shared Queries/") from the path.
                    var path = folder.Path;
                    var rootParent = folder;
                    var rootParentPath = string.Empty;
                    while (rootParent != null)
                    {
                        if (rootParent.IsRootNode)
                        {
                            rootParentPath = rootParent.Name + "/" + rootParentPath;
                        }
                        rootParent = (QueryFolder)rootParent.Parent;
                    }
                    path = path.Substring(Math.Min(path.Length, rootParentPath.Length));
                    queries.Add(new WorkItemQueryInfo(definition.Project.Name, definition.Id, path, definition.Name, definition.QueryText, definition.QueryType));
                }
            }
        }

        private void PerformImport(Dictionary<string, List<WorkItemQueryInfo>> teamProjectsWithQueries)
        {
            var numberOfSteps = teamProjectsWithQueries.Aggregate(0, (a, p) => a += p.Value.Count);
            var task = new ApplicationTask("Importing work item queries", numberOfSteps, true);
            PublishStatus(new StatusEventArgs(task));
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();

                var step = 0;
                foreach (var teamProjectWithQueries in teamProjectsWithQueries)
                {
                    var teamProject = teamProjectWithQueries.Key;
                    var queries = teamProjectWithQueries.Value;
                    var project = store.Projects[teamProjectWithQueries.Key];

                    foreach (var query in queries)
                    {
                        task.SetProgress(step++, "Importing work item query \"{0}\" for Team Project \"{1}\"".FormatCurrent(query.Name, teamProject));

                        ImportQuery(query, project.QueryHierarchy);

                        if (task.IsCanceled)
                        {
                            task.Status = "Canceled";
                            break;
                        }
                    }
                }
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while importing work item queries", e.Error);
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

        private static void ImportQuery(WorkItemQueryInfo query, QueryHierarchy queryHierarchy)
        {
            var currentFolder = queryHierarchy.OfType<QueryFolder>().FirstOrDefault(i => !i.IsPersonal);
            if (currentFolder == null)
            {
                throw new InvalidOperationException("The query hierarchy does not contain a shared (non-personal) root folder.");
            }
            var currentFolderNameIndex = 0;
            var pathComponents = (query.Path ?? string.Empty).Split('/');
            while (currentFolderNameIndex < pathComponents.Length)
            {
                var childFolderName = pathComponents[currentFolderNameIndex];
                if (currentFolder.Contains(childFolderName))
                {
                    var itemWithName = currentFolder[childFolderName];
                    if (itemWithName is QueryDefinition)
                    {
                        throw new ArgumentException("A query folder was requested but a query file with the same name exists at the same location: " + childFolderName);
                    }
                    else
                    {
                        currentFolder = (QueryFolder)itemWithName;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(childFolderName))
                    {
                        // If childFolderName is empty then we import into the root, otherwise create the folder.
                        var newFolder = new QueryFolder(childFolderName);
                        currentFolder.Add(newFolder);
                        currentFolder = newFolder;
                    }
                }
                currentFolderNameIndex++;
            }

            if (currentFolder.Contains(query.Name))
            {
                var itemWithName = currentFolder[query.Name];
                if (itemWithName is QueryFolder)
                {
                    throw new ArgumentException("A query file was requested but a query folder with the same name exists at the same location: " + query.Name);
                }
                else
                {
                    var existingQuery = (QueryDefinition)itemWithName;
                    existingQuery.QueryText = query.Text;
                }
            }
            else
            {
                var newQuery = new QueryDefinition(query.Name, query.Text);
                currentFolder.Add(newQuery);
            }
        }

        #endregion
    }
}