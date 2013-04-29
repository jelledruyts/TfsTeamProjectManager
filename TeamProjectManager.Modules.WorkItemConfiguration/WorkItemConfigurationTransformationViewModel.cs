using Microsoft.Practices.Prism.Events;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
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

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    [Export]
    public class WorkItemConfigurationTransformationViewModel : ViewModelBase
    {
        #region Properties

        public RelayCommand ApplyTransformationsCommand { get; private set; }
        public ObservableCollection<WorkItemConfigurationTransformationItem> Transformations { get; private set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public WorkItemConfigurationTransformationViewModel(IEventAggregator eventAggregator, ILogger logger)
            : base(eventAggregator, logger, "Work Item Configuration Transformation", "Allows you to transform the XML files that define the work item configuration (i.e. work item type definitions, work item categories and common and agile process configuration). This can be useful if you want have upgraded Team Foundation Server and want to take advantage of new features.")
        {
            this.ApplyTransformationsCommand = new RelayCommand(ApplyTransformations, CanApplyTransformations);
            this.Transformations = new ObservableCollection<WorkItemConfigurationTransformationItem>();
        }

        #endregion

        #region ApplyTransformations Command

        private bool CanApplyTransformations(object argument)
        {
            return this.IsAnyTeamProjectSelected() && this.Transformations.Any();
        }

        private void ApplyTransformations(object argument)
        {
            var result = MessageBox.Show("This will apply the specified transformations to all selected Team Projects. Are you sure you want to continue?", "Confirm Transformation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
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
                            if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.WorkItemType && !string.IsNullOrEmpty(transformation.WorkItemTypeNames))
                            {
                                // Apply work item type definition transformation, which can apply to multiple work item type definitions (semicolon separated).
                                var workItemTypeNames = (string.IsNullOrEmpty(transformation.WorkItemTypeNames) ? new string[0] : transformation.WorkItemTypeNames.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
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
                                            itemToTransform = WorkItemTypeDefinition.FromXml(wit.Export(false));
                                            transformedItems.Add(itemToTransform);
                                        }
                                    }
                                    if (itemToTransform != null)
                                    {
                                        task.Status = "Transforming \"{0}\"".FormatCurrent(itemToTransform.Name);
                                        var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                        itemToTransform.XmlDefinition = transformed;
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
                                    itemToTransform = WorkItemConfigurationItem.FromXml(project.Categories.Export());
                                    transformedItems.Add(itemToTransform);
                                }
                                task.Status = "Transforming " + itemToTransform.Name;
                                var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                itemToTransform.XmlDefinition = transformed;
                            }
                            else if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.AgileConfiguration)
                            {
                                var itemToTransform = transformedItems.FirstOrDefault(w => w.Type == WorkItemConfigurationItemType.AgileConfiguration);
                                if (itemToTransform == null)
                                {
                                    itemToTransform = WorkItemConfigurationItemImportExport.GetAgileConfiguration(tfs, project);
                                    if (itemToTransform != null)
                                    {
                                        transformedItems.Add(itemToTransform);
                                    }
                                }
                                if (itemToTransform != null)
                                {
                                    task.Status = "Transforming " + itemToTransform.Name;
                                    var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                    itemToTransform.XmlDefinition = transformed;
                                }
                            }
                            else if (transformation.WorkItemConfigurationItemType == WorkItemConfigurationItemType.CommonConfiguration)
                            {
                                var itemToTransform = transformedItems.FirstOrDefault(w => w.Type == WorkItemConfigurationItemType.CommonConfiguration);
                                if (itemToTransform == null)
                                {
                                    itemToTransform = WorkItemConfigurationItemImportExport.GetCommonConfiguration(tfs, project);
                                    if (itemToTransform != null)
                                    {
                                        transformedItems.Add(itemToTransform);
                                    }
                                }
                                if (itemToTransform != null)
                                {
                                    task.Status = "Transforming " + itemToTransform.Name;
                                    var transformed = WorkItemConfigurationTransformer.Transform(transformation.TransformationType, itemToTransform.XmlDefinition, transformation.TransformationXml);
                                    itemToTransform.XmlDefinition = transformed;
                                }
                            }
                            else
                            {
                                throw new ArgumentException("The Work Item Configuration Item Type is unknown: " + transformation.WorkItemConfigurationItemType.ToString());
                            }
                        }

                        // Only apply the transformations if they all succeeded (i.e. there was no exception).
                        // First apply all work item types in batch.
                        var teamProjectsWithWorkItemTypes = new Dictionary<TeamProjectInfo, List<WorkItemTypeDefinition>>()
                        {
                            { teamProject, transformedItems.Where(t => t.Type == WorkItemConfigurationItemType.WorkItemType).Cast<WorkItemTypeDefinition>().ToList() }
                        };
                        WorkItemConfigurationItemImportExport.ImportWorkItemTypes(this.Logger, task, ImportOptions.Import, store, teamProjectsWithWorkItemTypes);

                        // Then apply the other transformed items.
                        foreach (var transformedItem in transformedItems.Where(w => w.Type != WorkItemConfigurationItemType.WorkItemType))
                        {
                            task.Status = "Importing {0} in project \"{1}\"".FormatCurrent(transformedItem.Name, teamProject.Name);
                            switch (transformedItem.Type)
                            {
                                case WorkItemConfigurationItemType.Categories:
                                    project.Categories.Import(transformedItem.XmlDefinition.DocumentElement);
                                    break;
                                case WorkItemConfigurationItemType.CommonConfiguration:
                                    WorkItemConfigurationItemImportExport.SetCommonConfiguration(tfs, project, transformedItem);
                                    break;
                                case WorkItemConfigurationItemType.AgileConfiguration:
                                    WorkItemConfigurationItemImportExport.SetAgileConfiguration(tfs, project, transformedItem);
                                    break;
                                default:
                                    throw new ArgumentException("The Work Item Configuration Item Type is unknown: " + transformedItem.ToString());
                            }
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