using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using TeamProjectManager.Common.ObjectModel;

namespace TeamProjectManager.Modules.WorkItemConfiguration
{
    public class WorkItemConfiguration : ObservableObject
    {
        #region Observable Properties

        public string Name
        {
            get { return this.GetValue(NameProperty); }
            set { this.SetValue(NameProperty, value); }
        }

        public static readonly ObservableProperty<string> NameProperty = new ObservableProperty<string, WorkItemConfiguration>(o => o.Name);

        public string Description
        {
            get { return this.GetValue(DescriptionProperty); }
            set { this.SetValue(DescriptionProperty, value); }
        }

        public static readonly ObservableProperty<string> DescriptionProperty = new ObservableProperty<string, WorkItemConfiguration>(o => o.Description);

        public bool IsValid
        {
            get { return this.GetValue(IsValidProperty); }
            private set { this.SetValue(IsValidProperty, value); }
        }

        public static readonly ObservableProperty<bool> IsValidProperty = new ObservableProperty<bool, WorkItemConfigurationTransformationItem>(o => o.IsValid);

        #endregion

        #region Properties

        public ObservableCollection<WorkItemConfigurationItem> Items { get; private set; }

        #endregion

        #region Constructors

        public WorkItemConfiguration()
            : this(null, null)
        {
        }

        public WorkItemConfiguration(string name, IEnumerable<WorkItemConfigurationItem> items)
        {
            this.Name = name;
            this.Items = new ObservableCollection<WorkItemConfigurationItem>(items ?? new WorkItemConfigurationItem[0]);
            this.Items.CollectionChanged += (sender, e) => { RefreshCalculatedProperties(); };
            RefreshCalculatedProperties();
        }

        #endregion

        #region Event Handlers

        protected override void OnObservablePropertyChanged(ObservablePropertyChangedEventArgs e)
        {
            base.OnObservablePropertyChanged(e);
            RefreshCalculatedProperties();
        }

        private void RefreshCalculatedProperties()
        {
            this.Description = this.Name + (this.Items != null && this.Items.Any() ? string.Format(CultureInfo.CurrentCulture, " ({0})", string.Join(", ", this.Items.Select(w => w.Name))) : string.Empty);
            this.IsValid = !string.IsNullOrEmpty(this.Name) && this.Items != null && this.Items.Any();
        }

        #endregion

        #region Static Factory Methods

        public static WorkItemConfiguration FromTeamProject(TfsTeamProjectCollection tfs, Project project)
        {
            // Export work item type definitions.
            var projectWorkItemTypes = new List<WorkItemConfigurationItem>();
            foreach (WorkItemType workItemType in project.WorkItemTypes)
            {
                projectWorkItemTypes.Add(WorkItemConfigurationItem.FromXml(workItemType.Export(false)));
            }

            // Export categories.
            projectWorkItemTypes.Add(WorkItemConfigurationItemImportExport.GetCategories(project));

            // Export process configuration.
            var commonConfig = WorkItemConfigurationItemImportExport.GetCommonConfiguration(project);
            if (commonConfig != null)
            {
                projectWorkItemTypes.Add(commonConfig);
            }
            var agileConfig = WorkItemConfigurationItemImportExport.GetAgileConfiguration(project);
            if (agileConfig != null)
            {
                projectWorkItemTypes.Add(agileConfig);
            }
            var processConfig = WorkItemConfigurationItemImportExport.GetProcessConfiguration(project);
            if (processConfig != null)
            {
                projectWorkItemTypes.Add(processConfig);
            }

            return new WorkItemConfiguration(project.Name, projectWorkItemTypes);
        }

        public static WorkItemConfiguration FromProcessTemplate(string processTemplateFileName)
        {
            // Load the process template XML.
            if (!File.Exists(processTemplateFileName))
            {
                throw new FileNotFoundException("The process template file does not exist: " + processTemplateFileName);
            }
            var processTemplate = new XmlDocument();
            processTemplate.Load(processTemplateFileName);
            var baseDir = Path.GetDirectoryName(processTemplateFileName);
            var items = new List<WorkItemConfigurationItem>();
            string processTemplateName = null;
            var processTemplateNameNode = processTemplate.SelectSingleNode("/ProcessTemplate/metadata/name");
            if (processTemplateNameNode != null)
            {
                processTemplateName = processTemplateNameNode.InnerText;
            }

            // Find the work item tracking XML file.
            var workItemFileNameAttribute = processTemplate.SelectSingleNode("/ProcessTemplate/groups/group[@id='WorkItemTracking']/taskList/@filename");
            if (workItemFileNameAttribute != null)
            {
                // Load the work item tracking XML.
                var workItemConfigurationTemplateFileName = Path.Combine(baseDir, workItemFileNameAttribute.InnerText);
                if (!File.Exists(workItemConfigurationTemplateFileName))
                {
                    throw new FileNotFoundException("The work item configuration file defined in the process template file does not exist: " + workItemConfigurationTemplateFileName);
                }
                var workItemConfigurationTemplate = new XmlDocument();
                workItemConfigurationTemplate.Load(workItemConfigurationTemplateFileName);

                // Find all work item type definition XML files.
                foreach (XmlAttribute witFileNameAttribute in workItemConfigurationTemplate.SelectNodes("/tasks/task[@id='WITs']/taskXml/WORKITEMTYPES/WORKITEMTYPE/@fileName"))
                {
                    var witFileName = Path.Combine(baseDir, witFileNameAttribute.InnerText);
                    items.Add(WorkItemTypeDefinition.FromFile(witFileName));
                }

                // Find the categories XML file.
                var categoriesFileNameAttribute = workItemConfigurationTemplate.SelectSingleNode("/tasks/task[@id='Categories']/taskXml/CATEGORIES/@fileName");
                if (categoriesFileNameAttribute != null)
                {
                    var categoriesFileName = Path.Combine(baseDir, categoriesFileNameAttribute.InnerText);
                    items.Add(WorkItemConfigurationItem.FromFile(categoriesFileName));
                }
                else
                {
                    // If the process template doesn't specify any categories (typically because it's an old
                    // process template from before Work Item Categories existed), load an empty list anyway.
                    // This will improve comparisons because a Team Project will always have a Work Item
                    // Categories configuration item (even if it's empty).
                    items.Add(WorkItemConfigurationItem.FromXml("<cat:CATEGORIES xmlns:cat=\"http://schemas.microsoft.com/VisualStudio/2008/workitemtracking/categories\"/>"));
                }

                // Find the common configuration XML file.
                var commonConfigurationFileNameAttribute = workItemConfigurationTemplate.SelectSingleNode("/tasks/task[@id='ProcessConfiguration']/taskXml/PROCESSCONFIGURATION/CommonConfiguration/@fileName");
                if (commonConfigurationFileNameAttribute != null)
                {
                    var commonConfigurationFileName = Path.Combine(baseDir, commonConfigurationFileNameAttribute.InnerText);
                    items.Add(WorkItemConfigurationItem.FromFile(commonConfigurationFileName));
                }

                // Find the agile configuration XML file.
                var agileConfigurationFileNameAttribute = workItemConfigurationTemplate.SelectSingleNode("/tasks/task[@id='ProcessConfiguration']/taskXml/PROCESSCONFIGURATION/AgileConfiguration/@fileName");
                if (agileConfigurationFileNameAttribute != null)
                {
                    var agileConfigurationFileName = Path.Combine(baseDir, agileConfigurationFileNameAttribute.InnerText);
                    items.Add(WorkItemConfigurationItem.FromFile(agileConfigurationFileName));
                }

                // Find the process configuration XML file.
                var processConfigurationFileNameAttribute = workItemConfigurationTemplate.SelectSingleNode("/tasks/task[@id='ProcessConfiguration']/taskXml/PROCESSCONFIGURATION/ProjectConfiguration/@fileName");
                if (processConfigurationFileNameAttribute != null)
                {
                    var processConfigurationFileName = Path.Combine(baseDir, processConfigurationFileNameAttribute.InnerText);
                    items.Add(WorkItemConfigurationItem.FromFile(processConfigurationFileName));
                }
            }

            return new WorkItemConfiguration(processTemplateName, items);
        }

        #endregion
    }
}