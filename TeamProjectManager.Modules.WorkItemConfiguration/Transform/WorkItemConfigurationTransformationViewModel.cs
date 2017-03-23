using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using TeamProjectManager.Common;
using TeamProjectManager.Common.Events;
using TeamProjectManager.Common.Infrastructure;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration.Transform
{
    [Export]
    public class WorkItemConfigurationTransformationViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand AddTransformationCommand { get; private set; }
        public RelayCommand EditSelectedTransformationCommand { get; private set; }
        public RelayCommand RemoveSelectedTransformationCommand { get; private set; }
        public RelayCommand RemoveAllTransformationsCommand { get; private set; }
        public RelayCommand MoveSelectedTransformationUpCommand { get; private set; }
        public RelayCommand MoveSelectedTransformationDownCommand { get; private set; }
        public RelayCommand LoadTransformationsCommand { get; private set; }
        public RelayCommand SaveTransformationsCommand { get; private set; }
        public RelayCommand ApplyTransformationsCommand { get; private set; }
        public ObservableCollection<WorkItemConfigurationTransformationItem> Transformations { get; private set; }

        #endregion

        #region Observable Properties

        public WorkItemConfigurationTransformationItem SelectedTransformation
        {
            get { return this.GetValue(SelectedTransformationProperty); }
            set { this.SetValue(SelectedTransformationProperty, value); }
        }

        public static readonly ObservableProperty<WorkItemConfigurationTransformationItem> SelectedTransformationProperty = new ObservableProperty<WorkItemConfigurationTransformationItem, WorkItemConfigurationTransformationViewModel>(o => o.SelectedTransformation);

        public bool Simulate
        {
            get { return this.GetValue(SimulateProperty); }
            set { this.SetValue(SimulateProperty, value); }
        }

        public static readonly ObservableProperty<bool> SimulateProperty = new ObservableProperty<bool, WorkItemConfigurationTransformationViewModel>(o => o.Simulate);

        public bool SaveCopy
        {
            get { return this.GetValue(SaveCopyProperty); }
            set { this.SetValue(SaveCopyProperty, value); }
        }

        public static readonly ObservableProperty<bool> SaveCopyProperty = new ObservableProperty<bool, WorkItemConfigurationTransformationViewModel>(o => o.SaveCopy);

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemConfigurationTransformationViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Configuration Transformation", "Allows you to transform the XML files that define the work item configuration (i.e. work item type definitions, work item categories and common and agile process configuration). This can be useful if you want have upgraded Team Foundation Server and want to take advantage of new features.")
        {
            this.AddTransformationCommand = new RelayCommand(AddTransformation, CanAddTransformation);
            this.EditSelectedTransformationCommand = new RelayCommand(EditSelectedTransformation, CanEditSelectedTransformation);
            this.RemoveSelectedTransformationCommand = new RelayCommand(RemoveSelectedTransformation, CanRemoveSelectedTransformation);
            this.RemoveAllTransformationsCommand = new RelayCommand(RemoveAllTransformations, CanRemoveAllTransformations);
            this.MoveSelectedTransformationUpCommand = new RelayCommand(MoveSelectedTransformationUp, CanMoveSelectedTransformationUp);
            this.MoveSelectedTransformationDownCommand = new RelayCommand(MoveSelectedTransformationDown, CanMoveSelectedTransformationDown);
            this.LoadTransformationsCommand = new RelayCommand(LoadTransformations, CanLoadTransformations);
            this.SaveTransformationsCommand = new RelayCommand(SaveTransformations, CanSaveTransformations);
            this.ApplyTransformationsCommand = new RelayCommand(ApplyTransformations, CanApplyTransformations);
            this.Transformations = new ObservableCollection<WorkItemConfigurationTransformationItem>();
        }

        #endregion

        #region AddTransformation Command

        private bool CanAddTransformation(object argument)
        {
            return true;
        }

        private void AddTransformation(object argument)
        {
            var dialog = new WorkItemConfigurationTransformationItemEditorDialog();
            dialog.Owner = Application.Current.MainWindow;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                this.Transformations.Add(dialog.Transformation);
            }
        }

        #endregion

        #region EditSelectedTransformation Command

        private bool CanEditSelectedTransformation(object argument)
        {
            return this.SelectedTransformation != null;
        }

        private void EditSelectedTransformation(object argument)
        {
            var dialog = new WorkItemConfigurationTransformationItemEditorDialog(this.SelectedTransformation);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        #endregion

        #region RemoveSelectedTransformation Command

        private bool CanRemoveSelectedTransformation(object argument)
        {
            return this.SelectedTransformation != null;
        }

        private void RemoveSelectedTransformation(object argument)
        {
            this.Transformations.Remove(this.SelectedTransformation);
        }

        #endregion

        #region RemoveAllTransformations Command

        private bool CanRemoveAllTransformations(object argument)
        {
            return this.Transformations.Any();
        }

        private void RemoveAllTransformations(object argument)
        {
            this.Transformations.Clear();
        }

        #endregion

        #region MoveSelectedTransformationUp Command

        private bool CanMoveSelectedTransformationUp(object argument)
        {
            return this.SelectedTransformation != null && this.Transformations.IndexOf(this.SelectedTransformation) > 0;
        }

        private void MoveSelectedTransformationUp(object argument)
        {
            var currentIndex = this.Transformations.IndexOf(this.SelectedTransformation);
            this.Transformations.Move(currentIndex, currentIndex - 1);
        }

        #endregion

        #region MoveSelectedTransformationDown Command

        private bool CanMoveSelectedTransformationDown(object argument)
        {
            return this.SelectedTransformation != null && this.Transformations.IndexOf(this.SelectedTransformation) < this.Transformations.Count - 1;
        }

        private void MoveSelectedTransformationDown(object argument)
        {
            var currentIndex = this.Transformations.IndexOf(this.SelectedTransformation);
            this.Transformations.Move(currentIndex, currentIndex + 1);
        }

        #endregion

        #region LoadTransformations Command

        private bool CanLoadTransformations(object argument)
        {
            return true;
        }

        private void LoadTransformations(object argument)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Please select the transformation list (*.xml) to load.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                try
                {
                    var transformations = SerializationProvider.Read<WorkItemConfigurationTransformationItem[]>(dialog.FileName);
                    this.Transformations.Clear();
                    foreach (var transformation in transformations)
                    {
                        this.Transformations.Add(transformation);
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.Log(string.Format(CultureInfo.CurrentCulture, "An error occurred while loading the transformation list from \"{0}\"", dialog.FileName), exc);
                    MessageBox.Show("An error occurred while loading the transformation list. See the log file for details", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #region SaveTransformations Command

        private bool CanSaveTransformations(object argument)
        {
            return this.Transformations.Any();
        }

        private void SaveTransformations(object argument)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Please select the transformation list (*.xml) to save.";
            dialog.Filter = "XML Files (*.xml)|*.xml";
            var result = dialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                try
                {
                    SerializationProvider.Write<WorkItemConfigurationTransformationItem[]>(this.Transformations.ToArray(), dialog.FileName);
                }
                catch (Exception exc)
                {
                    this.Logger.Log(string.Format(CultureInfo.CurrentCulture, "An error occurred while saving the transformation list to \"{0}\"", dialog.FileName), exc);
                    MessageBox.Show("An error occurred while saving the transformation list. See the log file for details", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #region ApplyTransformations Command

        private bool CanApplyTransformations(object argument)
        {
            return this.IsAnyTeamProjectSelected() && this.Transformations.Any();
        }

        private void ApplyTransformations(object argument)
        {
            var simulate = this.Simulate;
            var saveCopy = this.SaveCopy;
            if (!simulate)
            {
                var result = MessageBox.Show("This will apply the specified transformations to all selected Team Projects. Are you sure you want to continue?", "Confirm Transformation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            var teamProjects = this.SelectedTeamProjects.ToList();
            var transformations = this.Transformations.ToList();
            var task = new ApplicationTask("Transforming work item configuration", teamProjects.Count, true);
            PublishStatus(new StatusEventArgs(task));
            var step = 0;
            var worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                var tfs = GetSelectedTfsTeamProjectCollection();
                var store = tfs.GetService<WorkItemStore>();
                var numTransformations = 0;

                foreach (var teamProject in teamProjects)
                {
                    task.SetProgress(step++, string.Format(CultureInfo.CurrentCulture, "Processing Team Project \"{0}\"", teamProject.Name));
                    try
                    {
                        var project = store.Projects[teamProject.Name];

                        // Apply the transformations only if everything succeeded for the Team Project; cache them in the mean time.
                        var transformedItems = new List<WorkItemConfigurationItem>();
                        foreach (var transformation in transformations)
                        {
                            if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.WorkItemType)
                            {
                                // Apply work item type definition transformation, which can apply to multiple work item type definitions (semicolon separated).
                                // If no work item type names are specified, apply to all work item types in the project.
                                var workItemTypeNames = (string.IsNullOrEmpty(transformation.WorkItemTypeNames) ? project.WorkItemTypes.Cast<WorkItemType>().Select(w => w.Name) : transformation.WorkItemTypeNames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                                foreach (var workItemTypeName in workItemTypeNames)
                                {
                                    // If the work item type has already been processed before, continue with that version.
                                    var itemToTransform = transformedItems.FirstOrDefault(w => string.Equals(w.Name, workItemTypeName, StringComparison.OrdinalIgnoreCase));
                                    if (itemToTransform == null)
                                    {
                                        // If the work item type wasn't processed yet, find the one with the specified name (if it exists).
                                        var wit = project.WorkItemTypes.Cast<WorkItemType>().FirstOrDefault(w => string.Equals(w.Name, workItemTypeName, StringComparison.OrdinalIgnoreCase));
                                        if (wit != null)
                                        {
                                            try
                                            {
                                                itemToTransform = WorkItemTypeDefinition.FromXml(wit.Export(false));
                                            }
                                            catch (VssServiceResponseException)
                                            {
                                                // A VssServiceResponseException with message "VS403207: The object does not exist or access is denied"
                                                // happens when trying to export a work item type in the inherited model.
                                                task.Status = string.Format(CultureInfo.CurrentCulture, "Skipping work item type \"{0}\" as it cannot be modified.", wit.Name);
                                            }
                                        }
                                    }
                                    if (itemToTransform != null)
                                    {
                                        task.Status = "Transforming " + itemToTransform.DisplayName;
                                        var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                        if (string.Equals(itemToTransform.XmlDefinition.DocumentElement.OuterXml, transformed.DocumentElement.OuterXml))
                                        {
                                            task.Status = "The transformation was applied but did not result in any changes, skipping.";
                                        }
                                        else
                                        {
                                            itemToTransform.XmlDefinition = transformed;
                                            transformedItems.Add(itemToTransform);
                                        }
                                    }
                                    else
                                    {
                                        task.Status = "Skipping \"{0}\": a work item type with this name was not found in the Team Project".FormatCurrent(workItemTypeName);
                                    }
                                }
                            }
                            else if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.Categories)
                            {
                                var itemToTransform = transformedItems.FirstOrDefault(w => w.Type == WorkItemConfigurationItemType.Categories);
                                if (itemToTransform == null)
                                {
                                    itemToTransform = WorkItemConfigurationItemImportExport.GetCategories(project);
                                }
                                task.Status = "Transforming " + itemToTransform.DisplayName;
                                var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                if (string.Equals(itemToTransform.XmlDefinition.DocumentElement.OuterXml, transformed.DocumentElement.OuterXml))
                                {
                                    task.Status = "The transformation was applied but did not result in any changes, skipping.";
                                }
                                else
                                {
                                    itemToTransform.XmlDefinition = transformed;
                                    transformedItems.Add(itemToTransform);
                                }
                            }
                            else if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.AgileConfiguration)
                            {
                                var itemToTransform = transformedItems.FirstOrDefault(w => w.Type == WorkItemConfigurationItemType.AgileConfiguration);
                                if (itemToTransform == null)
                                {
                                    itemToTransform = WorkItemConfigurationItemImportExport.GetAgileConfiguration(project);
                                }
                                if (itemToTransform != null)
                                {
                                    task.Status = "Transforming " + itemToTransform.DisplayName;
                                    var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                    if (string.Equals(itemToTransform.XmlDefinition.DocumentElement.OuterXml, transformed.DocumentElement.OuterXml))
                                    {
                                        task.Status = "The transformation was applied but did not result in any changes, skipping.";
                                    }
                                    else
                                    {
                                        itemToTransform.XmlDefinition = transformed;
                                        transformedItems.Add(itemToTransform);
                                    }
                                }
                            }
                            else if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.CommonConfiguration)
                            {
                                var itemToTransform = transformedItems.FirstOrDefault(w => w.Type == WorkItemConfigurationItemType.CommonConfiguration);
                                if (itemToTransform == null)
                                {
                                    itemToTransform = WorkItemConfigurationItemImportExport.GetCommonConfiguration(project);
                                }
                                if (itemToTransform != null)
                                {
                                    task.Status = "Transforming " + itemToTransform.DisplayName;
                                    var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                    if (string.Equals(itemToTransform.XmlDefinition.DocumentElement.OuterXml, transformed.DocumentElement.OuterXml))
                                    {
                                        task.Status = "The transformation was applied but did not result in any changes, skipping.";
                                    }
                                    else
                                    {
                                        itemToTransform.XmlDefinition = transformed;
                                        transformedItems.Add(itemToTransform);
                                    }
                                }
                            }
                            else if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.ProcessConfiguration)
                            {
                                var itemToTransform = transformedItems.FirstOrDefault(w => w.Type == WorkItemConfigurationItemType.ProcessConfiguration);
                                if (itemToTransform == null)
                                {
                                    itemToTransform = WorkItemConfigurationItemImportExport.GetProcessConfiguration(project);
                                }
                                if (itemToTransform != null)
                                {
                                    task.Status = "Transforming " + itemToTransform.DisplayName;
                                    var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                    if (string.Equals(itemToTransform.XmlDefinition.DocumentElement.OuterXml, transformed.DocumentElement.OuterXml))
                                    {
                                        task.Status = "The transformation was applied but did not result in any changes, skipping.";
                                    }
                                    else
                                    {
                                        itemToTransform.XmlDefinition = transformed;
                                        transformedItems.Add(itemToTransform);
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException("The Work Item Configuration Item Type is unknown: " + transformation.WorkItemConfigurationItemType.ToString());
                            }
                            if (task.IsCanceled)
                            {
                                break;
                            }
                        }

                        // Only apply the transformations if they all succeeded (i.e. there was no exception).
                        if (task.IsCanceled)
                        {
                            break;
                        }
                        else
                        {
                            var teamProjectsWithConfigurationItems = new Dictionary<TeamProjectInfo, List<WorkItemConfigurationItem>>()
                            {
                                { teamProject, transformedItems }
                            };
                            var options = ImportOptions.None;
                            if (simulate)
                            {
                                options |= ImportOptions.Simulate;
                            }
                            if (saveCopy)
                            {
                                options |= ImportOptions.SaveCopy;
                            }
                            WorkItemConfigurationItemImportExport.Import(this.Logger, task, false, store, teamProjectsWithConfigurationItems, options);
                        }
                        numTransformations += transformedItems.Count;
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
                e.Result = numTransformations;
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log("An unexpected exception occurred while transforming work item configuration", e.Error);
                    task.SetError(e.Error);
                    task.SetComplete("An unexpected exception occurred");
                }
                else
                {
                    var numTransformations = (int)e.Result;
                    task.SetComplete("Applied transformation to " + numTransformations.ToCountString("work item configuration item"));
                }
            };
            worker.RunWorkerAsync();
        }

        #endregion
    }
}